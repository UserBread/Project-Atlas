using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


// Generates LOD0 meshes: modular walls with simple cube geometry and plane windows/doors.
// Highest detail level suitable for close viewing.

public class LOD0MeshBuilder
{
    private const float MinSegmentLength = 0.001f;

    public Mesh GenerateLOD0Mesh(
        JArray rings, 
        double heightMeters, 
        int buildingIndex, 
        DetailSettings detailSettings, 
        double originLat, 
        double originLon,
        FacadePreset facadePreset
    )
    {
        if (rings == null || rings.Count == 0) return null;

        var outer = (JArray)rings[0];
        if (outer == null || outer.Count < 3) return null;
        
        // If no facade preset, skip windows/doors (return early with basic building)
        bool hasFacade = facadePreset != null;
        if (!hasFacade)
        {
            // Create building without windows/doors
            return GenerateLOD0MeshWithoutFacade(rings, rings, heightMeters, buildingIndex, detailSettings, originLat, originLon);
        }

        int pointCount = BuildingMeshHelper.CleanupRingPoints(outer);
        float unitsPerMeter = detailSettings.unitsPerMeter;
        float metersPerFloor = detailSettings.metersPerFloor;

        // Convert lat/lon to local positions
        List<Vector3> basePts = BuildingMeshHelper.ConvertRingToPositions(outer, pointCount, originLat, originLon, unitsPerMeter);
        
        List<Vector2> poly2D = BuildingMeshHelper.ConvertPositionsTo2D(basePts);
        if (MeshUtils.SignedArea(poly2D) < 0f)
        {
            poly2D.Reverse();
            basePts.Reverse();
        }

        List<int> indices = MeshUtils.Triangulate(poly2D);
        if (indices == null || indices.Count == 0)
        {
            Debug.LogWarning($"Could not triangulate building polygon #{buildingIndex}");
            return null;
        }

        float h = (float)heightMeters * unitsPerMeter;
        int n = basePts.Count;

        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colours = new List<Color>();

        // Create roof and floor
        BuildingMeshHelper.CreateRoofAndFloor(vertices, tris, colours, basePts, h, indices, facadePreset);

        // Calculate floors - use ceiling to ensure we cover full building height
        // but this may create a partial top floor
        int actualFloors = Mathf.CeilToInt(Mathf.Max(1, (float)heightMeters / metersPerFloor));

        // Find door wall based on nearest road
        Vector3 buildingCenter = BuildingMeshHelper.CalculateBuildingCenter(basePts);
        int doorWallIndex = BuildingMeshHelper.FindDoorWallIndex(basePts, buildingCenter, unitsPerMeter);

        // Create simple modular walls: cube walls with window/door planes
        float floorHeight = metersPerFloor * unitsPerMeter;
        
        for (int i = 0; i < n; i++)
        {
            int ni = (i + 1) % n;
            Vector3 bottomLeft = basePts[i];
            Vector3 bottomRight = basePts[ni];
            float wallLength = Vector3.Distance(bottomLeft, bottomRight);

            if (wallLength < MinSegmentLength)
                continue;

            bool hasDoorsOnThisWall = (i == doorWallIndex);
            CreateLOD0Wall(vertices, tris, colours, bottomLeft, bottomRight, h, actualFloors, 
                          floorHeight, wallLength, hasDoorsOnThisWall, facadePreset);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Building_{buildingIndex}_LOD0";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colours);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateLOD0Wall(List<Vector3> vertices, List<int> tris, List<Color> colours,
                                Vector3 bottomLeft, Vector3 bottomRight, float buildingHeight,
                                int floors, float floorHeight, float wallLength, bool hasDoor, FacadePreset facade)
    {
        Vector3 wallDir = (bottomRight - bottomLeft).normalized;
        Vector3 wallUp = Vector3.up;
        Vector3 wallRight = bottomRight - bottomLeft;

        float wallThickness = facade.wallThicknessMeters; // From facade preset
        Vector3 wallNormal = Vector3.Cross(wallRight, wallUp).normalized;

        // Wall geometry: simple cube slabs per floor
        for (int floor = 0; floor < floors; floor++)
        {
            float floorBase = floor * floorHeight;
            float floorTop = Mathf.Min((floor + 1) * floorHeight, buildingHeight);

            CreateLOD0WallSlab(vertices, tris, colours, bottomLeft, bottomRight, 
                             floorBase, floorTop, wallThickness, wallNormal, wallUp, hasDoor, floor,
                             wallLength, facade);
        }
    }

    private void CreateLOD0WallSlab(List<Vector3> vertices, List<int> tris, List<Color> colours,
                                     Vector3 bottomLeft, Vector3 bottomRight, float floorBase, float floorTop,
                                     float thickness, Vector3 wallNormal, Vector3 wallUp, bool hasDoor, int floorLevel,
                                     float wallLength, FacadePreset facade)
    {
        Vector3 wallDir = (bottomRight - bottomLeft).normalized;
        Color wallColor = facade.wallColor;
        Color windowColor = facade.windowColor;

        int baseIdx = vertices.Count;

        // Front face corners
        vertices.Add(bottomLeft + wallUp * floorBase);
        vertices.Add(bottomRight + wallUp * floorBase);
        vertices.Add(bottomRight + wallUp * floorTop);
        vertices.Add(bottomLeft + wallUp * floorTop);

        // Back face corners (inset by thickness)
        vertices.Add(bottomLeft + wallNormal * thickness + wallUp * floorBase);
        vertices.Add(bottomRight + wallNormal * thickness + wallUp * floorBase);
        vertices.Add(bottomRight + wallNormal * thickness + wallUp * floorTop);
        vertices.Add(bottomLeft + wallNormal * thickness + wallUp * floorTop);

        for (int i = 0; i < 8; i++)
            colours.Add(wallColor);

        // Front face
        tris.Add(baseIdx + 0); tris.Add(baseIdx + 3); tris.Add(baseIdx + 1);
        tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);

        // Back face
        tris.Add(baseIdx + 5); tris.Add(baseIdx + 6); tris.Add(baseIdx + 4);
        tris.Add(baseIdx + 4); tris.Add(baseIdx + 6); tris.Add(baseIdx + 7);

        // Side faces
        tris.Add(baseIdx + 4); tris.Add(baseIdx + 7); tris.Add(baseIdx + 0);
        tris.Add(baseIdx + 0); tris.Add(baseIdx + 7); tris.Add(baseIdx + 3);

        tris.Add(baseIdx + 1); tris.Add(baseIdx + 2); tris.Add(baseIdx + 5);
        tris.Add(baseIdx + 5); tris.Add(baseIdx + 2); tris.Add(baseIdx + 6);

        // Calculate floor height to check if windows/doors fit
        float actualFloorHeight = floorTop - floorBase;
        // Windows need: offset + height, Doors need: just height
        float windowRequiredHeight = facade.windowVerticalOffset + facade.windowHeightMeters;
        float doorRequiredHeight = facade.doorHeightMeters;
        float minimumHeightForFeatures = Mathf.Max(windowRequiredHeight, doorRequiredHeight);
        
        // Skip windows/doors if floor is too short (partial floor at top) - but be lenient
        if (actualFloorHeight < minimumHeightForFeatures * 0.8f) // Allow 80% threshold
        {
            return; // This floor is too short for windows/doors
        }

        // Add windows
        float windowWidth = facade.windowWidthMeters;
        float windowHeight = facade.windowHeightMeters;
        float windowSillHeight = floorBase + facade.windowVerticalOffset;
        int windowsPerFloor = facade.windowsPerFloor;
        float windowSpacing = wallLength / (windowsPerFloor + 1);

        // Calculate door position to avoid window overlap
        float doorCenterPosition = wallLength * 0.5f; // Door is at wall center
        float doorWidth = facade.doorWidthMeters;
        float doorClearance = doorWidth * 0.6f; // Extra clearance around door

        for (int w = 0; w < windowsPerFloor; w++)
        {
            float windowOffset = windowSpacing * (w + 1);
            
            // Skip window if it overlaps with door on ground floor
            if (hasDoor && floorLevel == 0)
            {
                if (Mathf.Abs(windowOffset - doorCenterPosition) < doorClearance)
                {
                    continue; // Skip this window to avoid door overlap
                }
            }
            
            float windowX = bottomLeft.x + wallDir.x * windowOffset;
            float windowZ = bottomLeft.z + wallDir.z * windowOffset;
            Vector3 windowPos = new Vector3(windowX, windowSillHeight + windowHeight / 2, windowZ);

            AddLOD0WindowPlane(vertices, tris, colours, bottomLeft, bottomRight, windowPos,
                             windowWidth, windowHeight, wallNormal, windowColor, facade);
        }

        // Add door on ground floor if applicable
        if (hasDoor && floorLevel == 0)
        {
            float doorHeight = facade.doorHeightMeters;
            Vector3 doorPos = bottomLeft + wallDir * doorCenterPosition + wallUp * doorHeight / 2;

            AddLOD0DoorPlane(vertices, tris, colours, bottomLeft, bottomRight, doorPos,
                           doorWidth, doorHeight, wallNormal, facade.doorColor, facade);
        }
    }

    private void AddLOD0WindowPlane(List<Vector3> vertices, List<int> tris, List<Color> colours,
                                     Vector3 wallStart, Vector3 wallEnd, Vector3 windowCenter,
                                     float width, float height, Vector3 wallNormal, Color windowColor, FacadePreset facade)
    {
        Vector3 wallDir = (wallEnd - wallStart).normalized;
        Vector3 wallUp = Vector3.up;

        Vector3 halfWidth = wallDir * width * 0.5f;
        Vector3 halfHeight = wallUp * height * 0.5f;

        int baseIdx = vertices.Count;
        float offset = facade.geometryInsetMeters; // Offset outward from wall (from facade)
        Vector3 windowOffset = -wallNormal * offset; // Negative to push outward

        vertices.Add(windowCenter - halfWidth - halfHeight + windowOffset);
        vertices.Add(windowCenter + halfWidth - halfHeight + windowOffset);
        vertices.Add(windowCenter + halfWidth + halfHeight + windowOffset);
        vertices.Add(windowCenter - halfWidth + halfHeight + windowOffset);

        for (int i = 0; i < 4; i++)
            colours.Add(windowColor);

        tris.Add(baseIdx + 0); tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
        tris.Add(baseIdx + 0); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
    }

    private void AddLOD0DoorPlane(List<Vector3> vertices, List<int> tris, List<Color> colours,
                                   Vector3 wallStart, Vector3 wallEnd, Vector3 doorCenter,
                                   float width, float height, Vector3 wallNormal, Color doorColor, FacadePreset facade)
    {
        Vector3 wallDir = (wallEnd - wallStart).normalized;
        Vector3 wallUp = Vector3.up;

        Vector3 halfWidth = wallDir * width * 0.5f;
        Vector3 halfHeight = wallUp * height * 0.5f;

        int baseIdx = vertices.Count;
        float offset = facade.geometryInsetMeters; // Offset outward from wall (from facade)
        Vector3 doorOffset = -wallNormal * offset; // Negative to push outward

        vertices.Add(doorCenter - halfWidth - halfHeight + doorOffset);
        vertices.Add(doorCenter + halfWidth - halfHeight + doorOffset);
        vertices.Add(doorCenter + halfWidth + halfHeight + doorOffset);
        vertices.Add(doorCenter - halfWidth + halfHeight + doorOffset);

        for (int i = 0; i < 4; i++)
            colours.Add(doorColor);

        tris.Add(baseIdx + 0); tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
        tris.Add(baseIdx + 0); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
    }

    // Generates LOD0 mesh without facades (no windows/doors)
    private Mesh GenerateLOD0MeshWithoutFacade(JArray rings, JArray rings2, double heightMeters, int buildingIndex,
                                               DetailSettings detailSettings, double originLat, double originLon)
    {
        var outer = (JArray)rings[0];
        if (outer == null || outer.Count < 3) return null;

        int pointCount = BuildingMeshHelper.CleanupRingPoints(outer);
        float unitsPerMeter = detailSettings.unitsPerMeter;

        // Convert lat/lon to local positions
        List<Vector3> basePts = BuildingMeshHelper.ConvertRingToPositions(outer, pointCount, originLat, originLon, unitsPerMeter);
        
        List<Vector2> poly2D = BuildingMeshHelper.ConvertPositionsTo2D(basePts);
        if (MeshUtils.SignedArea(poly2D) < 0f)
        {
            poly2D.Reverse();
            basePts.Reverse();
        }

        List<int> indices = MeshUtils.Triangulate(poly2D);
        if (indices == null || indices.Count == 0)
        {
            Debug.LogWarning($"Could not triangulate building polygon #{buildingIndex}");
            return null;
        }

        float h = (float)heightMeters * unitsPerMeter;
        int n = basePts.Count;

        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colours = new List<Color>();

        // Create basic gray roof and floor
        Color wallColor = Color.gray;
        BuildingMeshHelper.CreateRoofAndFloorBasic(vertices, tris, colours, basePts, h, indices, wallColor);

        // Create simple solid walls without fenestration
        for (int i = 0; i < n; i++)
        {
            int ni = (i + 1) % n;
            Vector3 bottomLeft = basePts[i];
            Vector3 bottomRight = basePts[ni];

            if (Vector3.Distance(bottomLeft, bottomRight) < MinSegmentLength)
                continue;

            Vector3 wallDir = (bottomRight - bottomLeft).normalized;
            Vector3 wallUp = Vector3.up;
            Vector3 wallRight = bottomRight - bottomLeft;
            Vector3 wallNormal = Vector3.Cross(wallRight, wallUp).normalized;
            float wallThickness = 0.3f; // Default thickness

            // Create solid walls without windows/doors
            float floorHeight = detailSettings.metersPerFloor * unitsPerMeter;
            int floors = Mathf.CeilToInt(Mathf.Max(1, (float)heightMeters / detailSettings.metersPerFloor));

            for (int floor = 0; floor < floors; floor++)
            {
                float floorBase = floor * floorHeight;
                float floorTop = Mathf.Min((floor + 1) * floorHeight, h);

                CreateSolidWallSlab(vertices, tris, colours, bottomLeft, bottomRight,
                                   floorBase, floorTop, wallThickness, wallNormal, wallUp, wallColor);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Building_{buildingIndex}_LOD0_NoFacade";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colours);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateSolidWallSlab(List<Vector3> vertices, List<int> tris, List<Color> colours,
                                      Vector3 bottomLeft, Vector3 bottomRight, float floorBase, float floorTop,
                                      float thickness, Vector3 wallNormal, Vector3 wallUp, Color wallColor)
    {
        int baseIdx = vertices.Count;

        // Front face corners
        vertices.Add(bottomLeft + wallUp * floorBase);
        vertices.Add(bottomRight + wallUp * floorBase);
        vertices.Add(bottomRight + wallUp * floorTop);
        vertices.Add(bottomLeft + wallUp * floorTop);

        // Back face corners (inset by thickness)
        vertices.Add(bottomLeft + wallNormal * thickness + wallUp * floorBase);
        vertices.Add(bottomRight + wallNormal * thickness + wallUp * floorBase);
        vertices.Add(bottomRight + wallNormal * thickness + wallUp * floorTop);
        vertices.Add(bottomLeft + wallNormal * thickness + wallUp * floorTop);

        for (int i = 0; i < 8; i++)
            colours.Add(wallColor);

        // Front face
        tris.Add(baseIdx + 0); tris.Add(baseIdx + 3); tris.Add(baseIdx + 1);
        tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);

        // Back face
        tris.Add(baseIdx + 5); tris.Add(baseIdx + 6); tris.Add(baseIdx + 4);
        tris.Add(baseIdx + 4); tris.Add(baseIdx + 6); tris.Add(baseIdx + 7);

        // Side faces
        tris.Add(baseIdx + 4); tris.Add(baseIdx + 7); tris.Add(baseIdx + 0);
        tris.Add(baseIdx + 0); tris.Add(baseIdx + 7); tris.Add(baseIdx + 3);

        tris.Add(baseIdx + 1); tris.Add(baseIdx + 2); tris.Add(baseIdx + 5);
        tris.Add(baseIdx + 5); tris.Add(baseIdx + 2); tris.Add(baseIdx + 6);
    }
}
