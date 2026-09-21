using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    // Static list to store road centerline points for door placement
    public static List<Vector3> roadCenterlines = new List<Vector3>();

    [Tooltip("File name placed in StreamingAssets")]
    public string fileName = "";

    public Material roadMaterial;

    [Tooltip("Road width in meters")]
    
    public float roadWidthMeters = 6f;

    [Tooltip("Shared detail settings applied to all generators")]
    public DetailSettings detailSettings = new DetailSettings();
    public SmoothingSettings smoothingSettings = new SmoothingSettings();
    public ChunkingSettings chunkingSettings = new ChunkingSettings();
    public CustomOrigin customOrigin = new CustomOrigin();

    [Tooltip("Combined mesh generated from all road polylines")]
    public Mesh combinedRoadMesh;

    [Tooltip("Combined meshes per chunk")]
    public List<Mesh> chunkMeshes = new List<Mesh>();

    [Tooltip("Chunk coordinates for combined meshes")]
    public List<Vector2Int> chunkCoords = new List<Vector2Int>();

    [Tooltip("Triangle counts per chunk mesh")]
    public List<int> chunkTriangleCounts = new List<int>();

    [Tooltip("Total triangle count across combined chunk meshes")]
    public int totalTriangleCount = 0;

    // Store road data for LOD regeneration
    private class RoadData
    {
        public JArray coords;
        public double height;
    }
    private List<RoadData> roadDataList = new List<RoadData>();

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

        roadDataList.Clear();

        foreach (var f in features)
        {
            var geom = f["geometry"];
            double featureHeight = f["height"]?.Value<double?>() ?? 0.0;
            if (geom == null) continue;
            string type = (string)geom["type"];

            if (type == "LineString")
            {
                var coords = (JArray)geom["coordinates"];
                roadDataList.Add(new RoadData { coords = coords, height = featureHeight });
            }
            else if (type == "MultiLineString")
            {
                var lines = (JArray)geom["coordinates"];
                foreach (var line in lines)
                {
                    roadDataList.Add(new RoadData { coords = (JArray)line, height = featureHeight });
                }
            }
        }

        // Store road centerlines for door placement
        PopulateRoadCenterlines(customOrigin.originLat, customOrigin.originLon);

        if (chunkingSettings.combinePerChunk)
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

    void PopulateRoadCenterlines(double originLat, double originLon)
    {
        roadCenterlines.Clear();
        
        foreach (var road in roadDataList)
        {
            if (road.coords == null || road.coords.Count < 2) continue;
            
            foreach (var coord in road.coords)
            {
                double lon = (double)coord[0];
                double lat = (double)coord[1];
                Vector3 p = GeoUtils.latLonToUnityPosition(lat, lon, originLat, originLon, 0.0);
                p *= detailSettings.unitsPerMeter;
                roadCenterlines.Add(p);
            }
        }
    }

    void CreateRoadFromCoords(
        JArray coords,
        double height,
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
            Vector3 p = GeoUtils.latLonToUnityPosition(lat, lon, customOrigin.originLat, customOrigin.originLon, height);
            p *= detailSettings.unitsPerMeter;
            pts.Add(p);
        }

        List<Vector3> finalPts = pts;
        if (smoothingSettings.smooth && pts.Count >= 4)
        {
            int subdivision = smoothingSettings.smoothSubdivision;
            finalPts = CatmullRomSpline(pts, subdivision);
        }

        if (chunkingSettings.combinePerChunk)
        {
            Vector2Int key = GetChunkKeyForPoints(finalPts, chunkingSettings.chunkSizeMeters * detailSettings.unitsPerMeter);
            if (!chunkMap.TryGetValue(key, out var data))
            {
                data = new ChunkMeshData();
                chunkMap[key] = data;
            }
            AppendRoadStrip(finalPts, roadWidthMeters * detailSettings.unitsPerMeter, data.vertices, data.tris, data.uvs);
        }
        else
        {
            AppendRoadStrip(finalPts, roadWidthMeters * detailSettings.unitsPerMeter, combinedVertices, combinedTris, combinedUVs);
        }
    }

    void BuildChunkMeshesWithLOD()
    {
        chunkMeshes.Clear();
        chunkCoords.Clear();
        chunkTriangleCounts.Clear();
        totalTriangleCount = 0;

        // Generate 3 LOD sets
        ApplyLODSettings(ChunkLOD.High);
        var highMap = GenerateAndBuildChunkMap(chunkingSettings.chunkSizeMeters * detailSettings.unitsPerMeter);

        ApplyLODSettings(ChunkLOD.Medium);
        var mediumMap = GenerateAndBuildChunkMap(chunkingSettings.chunkSizeMeters * detailSettings.unitsPerMeter);

        ApplyLODSettings(ChunkLOD.Low);
        var lowMap = GenerateAndBuildChunkMap(chunkingSettings.chunkSizeMeters * detailSettings.unitsPerMeter);

        // Collect all unique chunk coordinates from all LOD levels
        HashSet<Vector2Int> allCoords = new HashSet<Vector2Int>();
        foreach (var key in highMap.Keys) allCoords.Add(key);
        foreach (var key in mediumMap.Keys) allCoords.Add(key);
        foreach (var key in lowMap.Keys) allCoords.Add(key);

        // Combine per chunk
        foreach (Vector2Int coord in allCoords)
        {
            Mesh highMesh = highMap.ContainsKey(coord) ? BuildMeshFromChunkData(highMap[coord]) : null;
            Mesh mediumMesh = mediumMap.ContainsKey(coord) ? BuildMeshFromChunkData(mediumMap[coord]) : null;
            Mesh lowMesh = lowMap.ContainsKey(coord) ? BuildMeshFromChunkData(lowMap[coord]) : null;

            // Fallback logic: use best available mesh
            if (highMesh == null) highMesh = mediumMesh ?? lowMesh ?? new Mesh();
            if (mediumMesh == null) mediumMesh = lowMesh ?? highMesh;
            if (lowMesh == null) lowMesh = mediumMesh;

            GameObject go = new GameObject($"Roads_Chunk_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (roadMaterial != null)
                mr.material = roadMaterial;

            CityChunk chunk = go.AddComponent<CityChunk>();
            chunk.coord = coord;
            chunk.SetLODMeshes(highMesh, mediumMesh, lowMesh);

            int highTris = highMesh.triangles.Length / 3;
            int mediumTris = mediumMesh.triangles.Length / 3;
            int lowTris = lowMesh.triangles.Length / 3;
            // Debug.Log($"Road Chunk ({coord.x}, {coord.y}) → High: {highTris}, Medium: {mediumTris}, Low: {lowTris}");

            totalTriangleCount += highTris;
            chunkMeshes.Add(highMesh);
            chunkCoords.Add(coord);
            chunkTriangleCounts.Add(highTris);
        }
    }

    Dictionary<Vector2Int, ChunkMeshData> GenerateAndBuildChunkMap(float chunkSizeUnits)
    {
        Dictionary<Vector2Int, ChunkMeshData> chunkMap = new Dictionary<Vector2Int, ChunkMeshData>();

        foreach (var roadData in roadDataList)
        {
            List<Vector3> combinedVertices = new List<Vector3>();
            List<int> combinedTris = new List<int>();
            List<Vector2> combinedUVs = new List<Vector2>();
            CreateRoadFromCoords(roadData.coords, roadData.height, combinedVertices, combinedTris, combinedUVs, chunkMap);
        }

        return chunkMap;
    }

    void ApplyLODSettings(ChunkLOD lod)
    {
        if (lod == ChunkLOD.High)
        {
            smoothingSettings.smooth = true;
            smoothingSettings.smoothSubdivision = 3;
        }
        else if (lod == ChunkLOD.Medium)
        {
            smoothingSettings.smooth = true;
            smoothingSettings.smoothSubdivision = 2;
        }
        else // Low
        {
            smoothingSettings.smooth = false;
            smoothingSettings.smoothSubdivision = 0;
        }
    }

    Mesh BuildMeshFromChunkData(ChunkMeshData data)
    {
        Mesh mesh = new Mesh();
        mesh.SetVertices(data.vertices);
        mesh.SetTriangles(data.tris, 0);
        if (data.uvs.Count == data.vertices.Count)
            mesh.SetUVs(0, data.uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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

    static void AppendRoadStrip(
        List<Vector3> centerLine,
        float width,
        List<Vector3> combinedVertices,
        List<int> combinedTris,
        List<Vector2> combinedUVs)
    {
        if (centerLine == null || centerLine.Count < 2) return;
        if (width <= 0f) return;

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
            Vector3 left = -right;

            combinedVertices.Add(centerLine[i] + left * halfWidth);
            combinedVertices.Add(centerLine[i] + right * halfWidth);

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
                outPts.Add(point);
            }
        }
        outPts.Add(pts[n - 1]);
        return outPts;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
