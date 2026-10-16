using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

class FootpathData
{
    public JArray coords;
}


public class FootpathGenerator : MonoBehaviour
{
    [Tooltip("File name placed in StreamingAssets")]
    public string fileName = "";
    
    public Material footpathMaterial;
    
    [Tooltip("Road width in meters (used to offset footpaths from centerline)")]
    public float roadWidthMeters = 6f;

    [Tooltip("Footpath width in meters")]
    public float footpathWidthMeters = 2f;

    [Tooltip("Extra offset from road edge in meters")]
    public float footpathEdgeOffsetMeters = 0.5f;

    [Tooltip("Flat elevation in meters for footpaths")]
    public float footpathHeightMeters = 0.0f;
    [Tooltip("Shared detail settings applied to all generators")]
    public DetailSettings detailSettings = new DetailSettings();
    public SmoothingSettings smoothingSettings = new SmoothingSettings();
    public ChunkingSettings chunkSettings = new ChunkingSettings();
    public CustomOrigin customOrigin = new CustomOrigin();
    [Tooltip("Combined mesh generated from all footpaths")]
    public Mesh combinedFootpathMesh;

    [Tooltip("Combined meshes per chunk")]
    public List<Mesh> chunkMeshes = new List<Mesh>();

    [Tooltip("Chunk coordinates for combined meshes")]
    public List<Vector2Int> chunkCoords = new List<Vector2Int>();

    [Tooltip("Triangle counts per chunk mesh")]
    public List<int> chunkTriangleCounts = new List<int>();

    [Tooltip("Total triangle count across combined chunk meshes")]
    public int totalTriangleCount = 0;

    List<FootpathData> footpathDataList = new List<FootpathData>();
    public event Action GenerationCompleted;

    public float loadTimeSeconds { get; private set; }
    public float generationTimeSeconds { get; private set; }

    private float generationStartTimestamp;
    private float loadStartTimestamp;

    public void StartGeneration()
    {
        generationStartTimestamp = Time.realtimeSinceStartup;
        loadTimeSeconds = 0f;
        generationTimeSeconds = 0f;
        ClearGeneratedChildren();
        StartCoroutine(LoadAndCreate());
    }

    void ClearGeneratedChildren()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    // void Start()
    // {
    //     StartGeneration();
    // }

    IEnumerator LoadAndCreate()
    {
        string json = null;
        string error = null;

        loadStartTimestamp = Time.realtimeSinceStartup;

        yield return DatasetLoader.LoadGeoJsonFromStreamingAssets(
            fileName,
            s => json = s,
            e => error = e
        );

        loadTimeSeconds = Time.realtimeSinceStartup - loadStartTimestamp;

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError(error);
            CompleteGeneration();
            yield break;
        }

        if (string.IsNullOrEmpty(json))
        {
            CompleteGeneration();
            yield break;
        }

        JObject root = JObject.Parse(json);
        JArray features = (JArray)root["features"];
        if (features == null)
        {
            Debug.LogError("No features array in GeoJSON");
            CompleteGeneration();
            yield break;
        }

        if (!customOrigin.useCustomOrigin)
        {
            // Calculate center of all features for proper origin
            if (!GeometryConverter.TryGetBoundingBoxCenter(features, out customOrigin.originLat, out customOrigin.originLon))
            {
                Debug.LogWarning("Could not determine origin from GeoJSON; using (0,0)");
            }
        }

        GeometryConverter.ApplySharedOrigin(ref customOrigin.originLat, ref customOrigin.originLon, customOrigin.useCustomOrigin);

        footpathDataList.Clear();

        foreach (var f in features)
        {
            var geom = f["geometry"];
            if (geom == null) continue;
            string type = (string)geom["type"];

            if (type == "LineString")
            {
                var coords = (JArray)geom["coordinates"];
                if (coords != null && coords.Count >= 2)
                {
                    footpathDataList.Add(new FootpathData { coords = coords });
                }
            }
            else if (type == "MultiLineString")
            {
                var lines = (JArray)geom["coordinates"];
                foreach (var line in lines)
                {
                    JArray coords = (JArray)line;
                    if (coords != null && coords.Count >= 2)
                    {
                        footpathDataList.Add(new FootpathData { coords = coords });
                    }
                }
            }
        }

        if (chunkSettings.combinePerChunk)
        {
            BuildChunkMeshesWithLOD();
            CompleteGeneration();
            yield break;
        }

        CompleteGeneration();
    }

    void CompleteGeneration()
    {
        generationTimeSeconds = Time.realtimeSinceStartup - generationStartTimestamp;
        GenerationCompleted?.Invoke();
    }

    void CreateFootpathsFromCoords(
        JArray coords,
        List<Vector3> combinedVertices,
        List<int> combinedTris,
        List<Vector2> combinedUVs,
        Dictionary<Vector2Int, ChunkMeshData> chunkMap)
    {
        if (coords == null || coords.Count < 2) return;
        List<Vector3> pts = new List<Vector3>(coords.Count);
        foreach (var c in coords)
        {
            double lon = (double)c[0];
            double lat = (double)c[1];
            Vector3 p = GeoUtils.latLonToUnityPosition(lat, lon, customOrigin.originLat, customOrigin.originLon, footpathHeightMeters);
            p *= detailSettings.unitsPerMeter;
            p.y = footpathHeightMeters * detailSettings.unitsPerMeter;
            pts.Add(p);
        }

        List<Vector3> finalPts = pts;
        if (smoothingSettings.smooth && pts.Count >= 4)
        {
            int subdivision = smoothingSettings.smoothSubdivision;
            finalPts = CatmullRomSpline(pts, subdivision);
        }

        float roadHalf = roadWidthMeters * 0.5f * detailSettings.unitsPerMeter;
        float edgeOffset = footpathEdgeOffsetMeters * detailSettings.unitsPerMeter;
        float footpathWidth = footpathWidthMeters * detailSettings.unitsPerMeter;

        float leftOffset = -(roadHalf + edgeOffset + footpathWidth * 0.5f);
        float rightOffset = (roadHalf + edgeOffset + footpathWidth * 0.5f);

        if (chunkSettings.combinePerChunk)
        {
            Vector2Int key = GetChunkKeyForPoints(finalPts, chunkSettings.chunkSizeMeters * detailSettings.unitsPerMeter);
            if (!chunkMap.TryGetValue(key, out var data))
            {
                data = new ChunkMeshData();
                chunkMap[key] = data;
            }
            AppendOffsetStrip(finalPts, leftOffset, footpathWidth, data.vertices, data.tris, data.uvs);
            AppendOffsetStrip(finalPts, rightOffset, footpathWidth, data.vertices, data.tris, data.uvs);
        }
        else
        {
            AppendOffsetStrip(finalPts, leftOffset, footpathWidth, combinedVertices, combinedTris, combinedUVs);
            AppendOffsetStrip(finalPts, rightOffset, footpathWidth, combinedVertices, combinedTris, combinedUVs);
        }
    }

    void BuildChunkMeshesWithLOD()
    {
        chunkMeshes.Clear();
        chunkCoords.Clear();
        chunkTriangleCounts.Clear();
        totalTriangleCount = 0;

        if (footpathDataList.Count == 0)
        {
            return;
        }

        float chunkSizeUnits = Mathf.Max(0.01f, chunkSettings.chunkSizeMeters * detailSettings.unitsPerMeter);

        // Generate 3 mesh sets with different LOD settings
        var highSet   = GenerateAndBuildChunkMap(ChunkLOD.High, chunkSizeUnits);
        var mediumSet = GenerateAndBuildChunkMap(ChunkLOD.Medium, chunkSizeUnits);
        var lowSet    = GenerateAndBuildChunkMap(ChunkLOD.Low, chunkSizeUnits);

        HashSet<Vector2Int> allCoords = new HashSet<Vector2Int>(highSet.Keys);
        allCoords.UnionWith(mediumSet.Keys);
        allCoords.UnionWith(lowSet.Keys);

        foreach (var coord in allCoords)
        {
            Mesh highMesh   = highSet.ContainsKey(coord) ? highSet[coord] : null;
            Mesh mediumMesh = mediumSet.ContainsKey(coord) ? mediumSet[coord] : null;
            Mesh lowMesh    = lowSet.ContainsKey(coord) ? lowSet[coord] : null;

            // Fallback logic: use best available mesh
            if (highMesh == null) highMesh = mediumMesh ?? lowMesh ?? new Mesh();
            if (mediumMesh == null) mediumMesh = lowMesh ?? highMesh;
            if (lowMesh == null) lowMesh = mediumMesh;

            // int highTris = highMesh.triangles.Length / 3;
            // int mediumTris = mediumMesh.triangles.Length / 3;
            // int lowTris = lowMesh.triangles.Length / 3;

            // Debug.Log($"Footpath Chunk ({coord.x}, {coord.y}) → High: {highTris}, Medium: {mediumTris}, Low: {lowTris}");

            GameObject go = new GameObject($"Footpaths_Chunk_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (footpathMaterial != null)
                mr.material = footpathMaterial;

            CityChunk chunk = go.AddComponent<CityChunk>();
            chunk.coord = coord;
            chunk.SetLODMeshes(highMesh, mediumMesh, lowMesh);

            int triCount = highMesh.triangles.Length / 3;
            totalTriangleCount += triCount;

            chunkMeshes.Add(highMesh);
            chunkCoords.Add(coord);
            chunkTriangleCounts.Add(triCount);
        }
    }

    Dictionary<Vector2Int, Mesh> GenerateAndBuildChunkMap(ChunkLOD lod, float chunkSizeUnits)
    {
        ApplyLODSettings(lod);

        Dictionary<Vector2Int, ChunkMeshData> chunkMap = new Dictionary<Vector2Int, ChunkMeshData>();

        foreach (var data in footpathDataList)
        {
            List<Vector3> dummyVerts = new List<Vector3>();
            List<int> dummyTris = new List<int>();
            List<Vector2> dummyUVs = new List<Vector2>();

            CreateFootpathsFromCoords(data.coords, dummyVerts, dummyTris, dummyUVs, chunkMap);
        }

        // Build meshes from chunk data
        Dictionary<Vector2Int, Mesh> finalMap = new Dictionary<Vector2Int, Mesh>();

        foreach (var kvp in chunkMap)
        {
            ChunkMeshData data = kvp.Value;
            if (data.vertices.Count == 0) continue;

            Mesh mesh = new Mesh();
            mesh.name = $"Footpaths_Chunk_{kvp.Key.x}_{kvp.Key.y}_{lod}";
            mesh.SetVertices(data.vertices);
            mesh.SetTriangles(data.tris, 0);
            if (data.uvs.Count == data.vertices.Count)
                mesh.SetUVs(0, data.uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            finalMap[kvp.Key] = mesh;
        }

        return finalMap;
    }

    void ApplyLODSettings(ChunkLOD lod)
    {
        smoothingSettings.smooth = true;

        switch (lod)
        {
            case ChunkLOD.High:
                smoothingSettings.smoothSubdivision = 3;
                break;

            case ChunkLOD.Medium:
                smoothingSettings.smoothSubdivision = 2;
                break;

            case ChunkLOD.Low:
                smoothingSettings.smooth = false;
                break;
        }
    }

    void BuildChunkMeshes(Dictionary<Vector2Int, ChunkMeshData> chunkMap)
    {
        chunkMeshes.Clear();
        chunkCoords.Clear();
        chunkTriangleCounts.Clear();
        totalTriangleCount = 0;

        foreach (var kvp in chunkMap)
        {
            ChunkMeshData data = kvp.Value;
            if (data.vertices.Count == 0) continue;

            Mesh mesh = new Mesh();
            mesh.name = $"Footpaths_Chunk_{kvp.Key.x}_{kvp.Key.y}";
            mesh.SetVertices(data.vertices);
            mesh.SetTriangles(data.tris, 0);
            if (data.uvs.Count == data.vertices.Count)
                mesh.SetUVs(0, data.uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            int triCount = mesh.triangles.Length / 3;
            totalTriangleCount += triCount;

            chunkMeshes.Add(mesh);
            chunkCoords.Add(kvp.Key);
            chunkTriangleCounts.Add(triCount);

            GameObject go = new GameObject($"Footpaths_Chunk_{kvp.Key.x}_{kvp.Key.y}");
            go.transform.SetParent(transform, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (footpathMaterial != null) mr.material = footpathMaterial;
        }
    }

    static Vector2Int GetChunkKeyForPoints(List<Vector3> points, float chunkSizeUnits)
    {
        if (points == null || points.Count == 0) return Vector2Int.zero;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < points.Count; i++) sum += points[i];
        Vector3 center = sum / points.Count;
        int x = Mathf.FloorToInt(center.x / Mathf.Max(0.01f, chunkSizeUnits));
        int z = Mathf.FloorToInt(center.z / Mathf.Max(0.01f, chunkSizeUnits));
        return new Vector2Int(x, z);
    }

    class ChunkMeshData
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<int> tris = new List<int>();
        public List<Vector2> uvs = new List<Vector2>();
    }

    static void AppendOffsetStrip(
        List<Vector3> centerLine,
        float lateralOffset,
        float width,
        List<Vector3> combinedVertices,
        List<int> combinedTris,
        List<Vector2> combinedUVs)
    {
        if (centerLine == null || centerLine.Count < 2) return;
        if (Mathf.Approximately(width, 0f)) return;

        int baseIndex = combinedVertices.Count;
        float halfWidth = width * 0.5f;

        float totalLength = 0f;
        for (int i = 1; i < centerLine.Count; i++)
            totalLength += Vector3.Distance(centerLine[i - 1], centerLine[i]);

        float accumulated = 0f;
        for (int i = 0; i < centerLine.Count; i++)
        {
            Vector3 forward;
            if (i == 0)
                forward = (centerLine[i + 1] - centerLine[i]).normalized;
            else if (i == centerLine.Count - 1)
                forward = (centerLine[i] - centerLine[i - 1]).normalized;
            else
                forward = (centerLine[i + 1] - centerLine[i - 1]).normalized;

            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 centerOffset = centerLine[i] + right * lateralOffset;

            combinedVertices.Add(centerOffset - right * halfWidth);
            combinedVertices.Add(centerOffset + right * halfWidth);

            if (totalLength > 0f)
            {
                if (i > 0)
                    accumulated += Vector3.Distance(centerLine[i - 1], centerLine[i]);
                float v = accumulated / totalLength;
                combinedUVs.Add(new Vector2(0f, v));
                combinedUVs.Add(new Vector2(1f, v));
            }
            else
            {
                combinedUVs.Add(Vector2.zero);
                combinedUVs.Add(Vector2.right);
            }
        }

        for (int i = 0; i < centerLine.Count - 1; i++)
        {
            int i0 = baseIndex + i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            combinedTris.Add(i0);
            combinedTris.Add(i2);
            combinedTris.Add(i1);

            combinedTris.Add(i1);
            combinedTris.Add(i2);
            combinedTris.Add(i3);
        }
    }

    static List<Vector3> CatmullRomSpline(List<Vector3> pts, int subdivision)
    {
        var outPts = new List<Vector3>();
        int n = pts.Count;
        for (int i = 0; i < n - 1; i++)
        {
            Vector3 p0 = i == 0 ? pts[i] : pts[i - 1];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = (i + 2 < n) ? pts[i + 2] : pts[i + 1];

            for (int j = 0; j <= subdivision; j++)
            {
                float t = j / (float)(subdivision + 1);
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                point.y = pts[0].y;
                outPts.Add(point);
            }
        }
        Vector3 last = pts[n - 1];
        last.y = pts[0].y;
        outPts.Add(last);
        return outPts;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    public void RegenerateWithLOD(ChunkLOD lod)
    {
        smoothingSettings.smoothSubdivision = 2;

        switch (lod)
        {
            case ChunkLOD.High:
                smoothingSettings.smoothSubdivision = 3;
                break;
            case ChunkLOD.Medium:
                smoothingSettings.smoothSubdivision = 2;
                break;
            case ChunkLOD.Low:
                smoothingSettings.smooth = false;
                break;
        }

        StartGeneration();
    }
}
