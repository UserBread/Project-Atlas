using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


// Helper utility for common building mesh generation tasks.
// Provides shared methods for ALL LOD builders.

public static class BuildingMeshHelper
{
    
    // Cleans up ring points by removing duplicate closing point if present.
    // Returns the actual number of unique points.
    
    public static int CleanupRingPoints(JArray outer)
    {
        int pointCount = outer.Count;
        if (pointCount > 1 &&
            (double)outer[0][0] == (double)outer[pointCount - 1][0] &&
            (double)outer[0][1] == (double)outer[pointCount - 1][1])
        {
            pointCount -= 1;
        }
        return pointCount;
    }

    
    // Converts lat/lon ring points to local 3D positions.
    
    public static List<Vector3> ConvertRingToPositions(
        JArray outer, 
        int pointCount,
        double originLat, 
        double originLon,
        float unitsPerMeter
    )
    {
        List<Vector3> basePts = new List<Vector3>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            double lon = (double)outer[i][0];
            double lat = (double)outer[i][1];
            Vector3 p = GeoUtils.latLonToUnityPosition(lat, lon, originLat, originLon, 0.0);
            p *= unitsPerMeter;
            basePts.Add(p);
        }
        return basePts;
    }

    
    // Converts 3D positions to 2D coordinates (for triangulation).
    
    public static List<Vector2> ConvertPositionsTo2D(List<Vector3> positions)
    {
        List<Vector2> poly2D = new List<Vector2>(positions.Count);
        for (int i = 0; i < positions.Count; i++) 
        {
            poly2D.Add(new Vector2(positions[i].x, positions[i].z));
        }
        return poly2D;
    }

    
    // Creates roof and floor geometry for a building footprint.
    
    public static void CreateRoofAndFloor(
        List<Vector3> vertices, 
        List<int> tris, 
        List<Color> colours,
        List<Vector3> basePts, 
        float height, 
        List<int> indices,
        FacadePreset facade = null
    )
    {
        int n = basePts.Count;
        Color roofColor = facade != null ? facade.roofColor : Color.gray;
        Color floorColor = facade != null ? facade.floorColor : Color.gray;

        // Base vertices (floor)
        int baseVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i]);
            colours.Add(floorColor);
        }

        // Top vertices (roof)
        int topVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i] + Vector3.up * height);
            colours.Add(roofColor);
        }

        // Roof (top face)
        for (int i = 0; i < indices.Count; i += 3)
        {
            tris.Add(indices[i + 2] + topVertexOffset);
            tris.Add(indices[i + 1] + topVertexOffset);
            tris.Add(indices[i] + topVertexOffset);
        }

        // Floor (bottom face)
        for (int i = 0; i < indices.Count; i += 3)
        {
            tris.Add(indices[i] + baseVertexOffset);
            tris.Add(indices[i + 1] + baseVertexOffset);
            tris.Add(indices[i + 2] + baseVertexOffset);
        }
    }

    
    // Creates basic roof and floor (no facade colors, all gray).
    
    public static void CreateRoofAndFloorBasic(
        List<Vector3> vertices, 
        List<int> tris, 
        List<Color> colours,
        List<Vector3> basePts, 
        float height, 
        List<int> indices,
        Color wallColor
    )
    {
        int n = basePts.Count;
        Color roofColor = wallColor;
        Color floorColor = wallColor;

        // Base vertices (floor)
        int baseVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i]);
            colours.Add(floorColor);
        }

        // Top vertices (roof)
        int topVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i] + Vector3.up * height);
            colours.Add(roofColor);
        }

        // Roof (top face)
        for (int i = 0; i < indices.Count; i += 3)
        {
            tris.Add(indices[i + 2] + topVertexOffset);
            tris.Add(indices[i + 1] + topVertexOffset);
            tris.Add(indices[i] + topVertexOffset);
        }

        // Floor (bottom face)
        for (int i = 0; i < indices.Count; i += 3)
        {
            tris.Add(indices[i] + baseVertexOffset);
            tris.Add(indices[i + 1] + baseVertexOffset);
            tris.Add(indices[i + 2] + baseVertexOffset);
        }
    }

    
    // Calculates the center point of the building footprint.
    
    public static Vector3 CalculateBuildingCenter(List<Vector3> basePts)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < basePts.Count; i++)
        {
            center += basePts[i];
        }
        return center / basePts.Count;
    }

    
    // Finds which wall (edge index) is nearest to the closest road segment AND faces the road.
    // Returns -1 if no nearby road found.
    
    public static int FindDoorWallIndex(
        List<Vector3> basePts, 
        Vector3 buildingCenter, 
        float unitsPerMeter
    )
    {
        int doorWallIndex = -1;
        float maxRoadDistance = 50f * unitsPerMeter;
        
        Vector3 nearestRoadPoint = FindNearestPointOnRoad(buildingCenter, maxRoadDistance, out float roadDist);

        if (nearestRoadPoint != Vector3.zero)
        {
            float closestWallDist = float.MaxValue;
            int n = basePts.Count;
            
            for (int i = 0; i < n; i++)
            {
                int ni = (i + 1) % n;
                Vector3 wallStart = basePts[i];
                Vector3 wallEnd = basePts[ni];
                Vector3 wallMidpoint = (wallStart + wallEnd) * 0.5f;
                
                // Calculate nearest point on wall segment to road point
                float distToRoad = PointToSegmentDistance(nearestRoadPoint, wallStart, wallEnd);
                
                // Calculate wall normal (outward facing)
                Vector3 wallDir = (wallEnd - wallStart).normalized;
                Vector3 wallNormal = new Vector3(-wallDir.z, 0f, wallDir.x); // Perpendicular in XZ plane
                
                // Check if wall faces the road (dot product > 0 means facing)
                Vector3 toRoad = (nearestRoadPoint - wallMidpoint).normalized;
                float facingDot = Vector3.Dot(wallNormal, toRoad);
                
                // Prefer walls that face the road AND are close to it
                // If wall faces away, increase effective distance heavily
                float effectiveDistance = distToRoad;
                if (facingDot < 0f)
                {
                    effectiveDistance *= 10f; // Penalize walls facing away
                }

                if (effectiveDistance < closestWallDist)
                {
                    closestWallDist = effectiveDistance;
                    doorWallIndex = i;
                }
            }
        }

        return doorWallIndex;
    }

    
    // Finds the nearest point on any road segment from the building center.
    // Uses point-to-line-segment distance for accurate proximity.
    
    private static Vector3 FindNearestPointOnRoad(
        Vector3 position, 
        float maxDistance, 
        out float nearestDistance
    )
    {
        nearestDistance = maxDistance;
        Vector3 nearest = Vector3.zero;

        if (RoadGenerator.roadCenterlines.Count < 2)
            return nearest;

        // Check each road segment (line between consecutive points)
        for (int i = 0; i < RoadGenerator.roadCenterlines.Count - 1; i++)
        {
            Vector3 segmentStart = RoadGenerator.roadCenterlines[i];
            Vector3 segmentEnd = RoadGenerator.roadCenterlines[i + 1];
            
            // Find closest point on this segment
            Vector3 closestOnSegment = ClosestPointOnSegment(position, segmentStart, segmentEnd);
            float dist = Vector3.Distance(position, closestOnSegment);
            
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearest = closestOnSegment;
            }
        }

        return nearest;
    }
    
    
    // Returns the closest point on a line segment to a given point.
    
    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        Vector3 segment = segmentEnd - segmentStart;
        float segmentLengthSq = segment.sqrMagnitude;
        
        if (segmentLengthSq < 0.0001f)
            return segmentStart; // Segment is essentially a point
        
        // Project point onto segment
        float t = Mathf.Clamp01(Vector3.Dot(point - segmentStart, segment) / segmentLengthSq);
        return segmentStart + segment * t;
    }
    
    
    // Calculates the minimum distance from a point to a line segment.
    
    private static float PointToSegmentDistance(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        Vector3 closest = ClosestPointOnSegment(point, segmentStart, segmentEnd);
        return Vector3.Distance(point, closest);
    }}
