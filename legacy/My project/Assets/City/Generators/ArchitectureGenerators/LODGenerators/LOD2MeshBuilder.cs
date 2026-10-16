using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


// Generates LOD2 meshes: simplified skyline mesh.
// Lowest detail level for distant viewing.

public class LOD2MeshBuilder
{
    private const float MinSegmentLength = 0.001f;

    public Mesh GenerateLOD2Mesh(JArray rings, double heightMeters, int buildingIndex,
                                  DetailSettings detailSettings, double originLat, double originLon,
                                  FacadePreset facadePreset)
    {
        if (rings == null || rings.Count == 0) return null;

        var outer = (JArray)rings[0];
        if (outer == null || outer.Count < 3) return null;
        
        // If no facade preset, return basic mesh without features
        if (facadePreset == null)
        {
            return GenerateLOD2MeshWithoutFacade(rings, heightMeters, buildingIndex, detailSettings, originLat, originLon);
        }

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

        Color buildingColor = facadePreset.wallColor;

        // Base vertices (ground level)
        int baseVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i]);
            colours.Add(buildingColor);
        }

        // Top vertices (roof level)
        int topVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i] + Vector3.up * h);
            colours.Add(buildingColor);
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

        // Walls: simple flat planes connecting base to roof
        for (int i = 0; i < n; i++)
        {
            int ni = (i + 1) % n;
            
            if (Vector3.Distance(basePts[i], basePts[ni]) < MinSegmentLength)
                continue;

            int v0 = baseVertexOffset + i;
            int v1 = baseVertexOffset + ni;
            int v2 = topVertexOffset + ni;
            int v3 = topVertexOffset + i;

            // Front face
            tris.Add(v0); tris.Add(v3); tris.Add(v1);
            tris.Add(v1); tris.Add(v3); tris.Add(v2);

            // Back face
            tris.Add(v1); tris.Add(v3); tris.Add(v0);
            tris.Add(v2); tris.Add(v3); tris.Add(v1);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Building_{buildingIndex}_LOD2";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colours);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Generates LOD2 mesh without facades (simplified building)
    private Mesh GenerateLOD2MeshWithoutFacade(JArray rings, double heightMeters, int buildingIndex,
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

        Color buildingColor = Color.gray;

        // Base vertices (ground level)
        int baseVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i]);
            colours.Add(buildingColor);
        }

        // Top vertices (roof level)
        int topVertexOffset = vertices.Count;
        for (int i = 0; i < n; i++)
        {
            vertices.Add(basePts[i] + Vector3.up * h);
            colours.Add(buildingColor);
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

        // Walls: simple flat planes connecting base to roof
        for (int i = 0; i < n; i++)
        {
            int ni = (i + 1) % n;
            
            if (Vector3.Distance(basePts[i], basePts[ni]) < MinSegmentLength)
                continue;

            int v0 = baseVertexOffset + i;
            int v1 = baseVertexOffset + ni;
            int v2 = topVertexOffset + ni;
            int v3 = topVertexOffset + i;

            // Front face
            tris.Add(v0); tris.Add(v3); tris.Add(v1);
            tris.Add(v1); tris.Add(v3); tris.Add(v2);

            // Back face
            tris.Add(v1); tris.Add(v3); tris.Add(v0);
            tris.Add(v2); tris.Add(v3); tris.Add(v1);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Building_{buildingIndex}_LOD2_NoFacade";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colours);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
