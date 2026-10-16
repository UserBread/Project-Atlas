using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


// Generates LOD1 meshes: detailed mesh with per-segment wall grids.
// Medium detail level.

public class LOD1MeshBuilder
{
    private const float MinSegmentLength = 0.001f;

    public Mesh GenerateLOD1Mesh(JArray rings, double heightMeters, int buildingIndex,
                                  DetailSettings detailSettings, double originLat, double originLon,
                                  FacadePreset facadePreset)
    {
        if (rings == null || rings.Count == 0) return null;

        var outer = (JArray)rings[0];
        if (outer == null || outer.Count < 3) return null;
        
        // If no facade preset, skip windows/doors (return early with basic building)
        bool hasFacade = facadePreset != null;
        if (!hasFacade)
        {
            // Create building without windows/doors
            return GenerateLOD1MeshWithoutFacade(rings, heightMeters, buildingIndex, detailSettings, originLat, originLon);
        }

        int pointCount = BuildingMeshHelper.CleanupRingPoints(outer);
        float unitsPerMeter = detailSettings.unitsPerMeter;
        float metersPerFloor = detailSettings.metersPerFloor;
        int verticalDivisions = detailSettings.verticalDivisions;
        float metersPerHorizontalDivision = detailSettings.metersPerHorizontalDivision;

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

        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colours = new List<Color>();

        int n = basePts.Count;

        BuildingMeshHelper.CreateRoofAndFloor(vertices, tris, colours, basePts, h, indices, facadePreset);

        int actualFloors = Mathf.CeilToInt(Mathf.Max(1, (float)heightMeters / metersPerFloor));
        int verticalSteps = actualFloors * Mathf.Max(1, verticalDivisions);

        Vector3 buildingCenter = BuildingMeshHelper.CalculateBuildingCenter(basePts);
        int doorWallIndex = BuildingMeshHelper.FindDoorWallIndex(basePts, buildingCenter, unitsPerMeter);

        // Create walls with grid segments
        for (int i = 0; i < n; i++)
        {
            int ni = (i + 1) % n;
            Vector3 bottomLeft = basePts[i];
            Vector3 bottomRight = basePts[ni];

            if (Vector3.Distance(bottomLeft, bottomRight) < MinSegmentLength)
                continue;

            MeshUtils.CreateWallGrid(
                vertices,
                tris,
                bottomLeft,
                bottomRight,
                h,
                verticalSteps,
                unitsPerMeter,
                metersPerHorizontalDivision,
                actualFloors,
                metersPerFloor * unitsPerMeter,
                facadePreset.windowsPerFloor,
                i == doorWallIndex,
                colours,
                facadePreset
            );
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Building_{buildingIndex}_LOD1";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colours);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Generates LOD1 mesh without facades (no windows/doors)
    private Mesh GenerateLOD1MeshWithoutFacade(JArray rings, double heightMeters, int buildingIndex,
                                               DetailSettings detailSettings, double originLat, double originLon)
    {
        var outer = (JArray)rings[0];
        if (outer == null || outer.Count < 3) return null;

        int pointCount = BuildingMeshHelper.CleanupRingPoints(outer);
        float unitsPerMeter = detailSettings.unitsPerMeter;

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

        Color wallColor = Color.gray;
        BuildingMeshHelper.CreateRoofAndFloorBasic(vertices, tris, colours, basePts, h, indices, wallColor);

        int actualFloors = Mathf.CeilToInt(Mathf.Max(1, (float)heightMeters / detailSettings.metersPerFloor));
        int verticalSteps = actualFloors * Mathf.Max(1, detailSettings.verticalDivisions);

        // Create solid walls without windows/doors
        for (int i = 0; i < n; i++)
        {
            int ni = (i + 1) % n;
            Vector3 bottomLeft = basePts[i];
            Vector3 bottomRight = basePts[ni];

            if (Vector3.Distance(bottomLeft, bottomRight) < MinSegmentLength)
                continue;

            MeshUtils.CreateWallGridNoFenestration(
                vertices,
                tris,
                bottomLeft,
                bottomRight,
                h,
                verticalSteps,
                colours,
                wallColor
            );
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Building_{buildingIndex}_LOD1_NoFacade";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colours);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
