using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{

    [Tooltip("File name placed in StreamingAssets")]
    public string fileName = "";

    [Tooltip("Facade style preset for building appearance")]
    public FacadePreset facadePreset;

    public Material buildingMaterial;

    public DetailSettings detailSettings = new DetailSettings();
    public ChunkingSettings chunkingSettings = new ChunkingSettings();
    public CustomOrigin customOrigin = new CustomOrigin();

    [Tooltip("Generated meshes from the last load")]
    public List<Mesh> generatedMeshes = new List<Mesh>();

    [Tooltip("Combined meshes per chunk")]
    public List<Mesh> chunkMeshes = new List<Mesh>();

    [Tooltip("Chunk coordinates for combined meshes")]
    public List<Vector2Int> chunkCoords = new List<Vector2Int>();

    [Tooltip("Triangle counts per chunk mesh")]
    public List<int> chunkTriangleCounts = new List<int>();

    [Tooltip("Total triangle count across combined chunk meshes")]
    public int totalTriangleCount = 0;

    List<BuildingInstance> buildingInstances = new List<BuildingInstance>();
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

        buildingInstances.Clear();
        int buildingIndex = 0;

        foreach (var f in features)
        {
            var geom = f["geometry"];
            if (geom == null) continue;

            string type = (string)geom["type"];
            JArray rings = GetRingsForFootprint(geom);
            double featureHeight = GetFeatureHeight(f, rings);

            if (rings == null) continue;

            if (type == "Polygon")
            {
                var instance = new BuildingInstance(rings, featureHeight, buildingIndex++);
                buildingInstances.Add(instance);
            }
            else if (type == "MultiPolygon")
            {
                var polys = (JArray)geom["coordinates"];
                foreach (var poly in polys)
                {
                    var instance = new BuildingInstance((JArray)poly, featureHeight, buildingIndex++);
                    buildingInstances.Add(instance);
                }
            }
        }

        BuildChunkMeshesWithLOD();

    }

    void CompleteGeneration()
    {
        generationTimeSeconds = Time.realtimeSinceStartup - generationStartTimestamp;
        GenerationCompleted?.Invoke();
    }

    double GetFeatureHeight(JToken f, JArray rings = null)
    {
        double h = 0.0;
        var props = f["properties"];
        if (props != null)
        {
            var pheight = props["height"];
            if (pheight != null && pheight.Type != JTokenType.Null)
            {
                if (double.TryParse(pheight.Value<string>(), out h) && h > 0)
                    return h;
            }

            var levels = props["building:levels"];
            if (levels != null && levels.Type != JTokenType.Null)
            {
                if (double.TryParse(levels.Value<string>(), out double numLevels) && numLevels > 0)
                    return numLevels * 3.0;
            }

            levels = props["levels"];
            if (levels != null && levels.Type != JTokenType.Null)
            {
                if (double.TryParse(levels.Value<string>(), out double numLevels2) && numLevels2 > 0)
                    return numLevels2 * 3.0;
            }

            var buildingType = props["building"];
            if (buildingType != null && buildingType.Type != JTokenType.Null)
            {
                string type = buildingType.Value<string>()?.ToLower() ?? "";
                h = GetHeightByBuildingType(type);
                if (h > 0)
                    return h;
            }

            if (rings != null && rings.Count > 0)
            {
                h = EstimateHeightByFootprint(rings);
                if (h > 0)
                    return h;
            }
        }

        return 6.0;
    }

    double GetHeightByBuildingType(string buildingType)
    {
        return buildingType switch
        {
            "apartments" or "residential" or "flat" => 12.0,
            "office" or "commercial" or "retail" => 15.0,
            "industrial" or "warehouse" => 10.0,
            "church" or "cathedral" or "temple" => 20.0,
            "school" or "university" or "college" => 12.0,
            "hospital" or "clinic" => 15.0,
            "parking" or "garage" => 5.0,
            "shed" or "garage" or "storage" => 3.0,
            "greenhouse" => 4.0,
            "train_station" or "station" => 15.0,
            _ => 6.0
        };
    }

    double EstimateHeightByFootprint(JArray rings)
    {
        if (rings == null || rings.Count == 0) return 0;

        var outer = (JArray)rings[0];
        if (outer == null || outer.Count < 3) return 0;

        double area = 0;
        int n = outer.Count;
        for (int i = 0; i < n; i++)
        {
            double lon1 = (double)outer[i][0];
            double lat1 = (double)outer[i][1];
            double lon2 = (double)outer[(i + 1) % n][0];
            double lat2 = (double)outer[(i + 1) % n][1];
            area += lon1 * lat2 - lon2 * lat1;
        }
        area = Math.Abs(area) / 2.0;

        double areaMetersSquared = area * 111000 * 111000;

        if (areaMetersSquared < 100)
            return 4.0;
        if (areaMetersSquared < 500)
            return 6.0 + (Math.Sqrt(areaMetersSquared - 100) / 100);
        return 8.0 + (Math.Sqrt(areaMetersSquared - 500) / 200);
    }

    static JArray GetRingsForFootprint(JToken geom)
    {
        if (geom == null) return null;

        string type = (string)geom["type"];
        if (type == "Polygon")
            return (JArray)geom["coordinates"];

        if (type == "MultiPolygon")
        {
            var polys = (JArray)geom["coordinates"];
            if (polys != null && polys.Count > 0)
                return (JArray)polys[0];
        }

        return null;
    }

    void BuildChunkMeshesWithLOD()
    {
        chunkMeshes.Clear();
        chunkCoords.Clear();
        chunkTriangleCounts.Clear();
        totalTriangleCount = 0;

        if (!chunkingSettings.combinePerChunk || buildingInstances.Count == 0)
        {
            CompleteGeneration();
            return;
        }

        float chunkSizeUnits = Mathf.Max(0.01f, chunkingSettings.chunkSizeMeters * detailSettings.unitsPerMeter);

        // Build 3 mesh sets: High (LOD0), Medium (LOD1), Low (LOD2)
        var highSet   = BuildChunkMapForLOD(ChunkLOD.High, chunkSizeUnits);   // LOD0
        var mediumSet = BuildChunkMapForLOD(ChunkLOD.Medium, chunkSizeUnits);   // LOD1
        var lowSet    = BuildChunkMapForLOD(ChunkLOD.Low, chunkSizeUnits);   // LOD2

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

            GameObject go = new GameObject($"Buildings_Chunk_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (buildingMaterial != null)
                mr.material = buildingMaterial;

            CityChunk chunk = go.AddComponent<CityChunk>();
            chunk.coord = coord;
            chunk.SetLODMeshes(highMesh, mediumMesh, lowMesh);

            int triCount = highMesh.triangles.Length / 3;
            totalTriangleCount += triCount;

            chunkMeshes.Add(highMesh);
            chunkCoords.Add(coord);
            chunkTriangleCounts.Add(triCount);
        }

        CompleteGeneration();
    }

    Dictionary<Vector2Int, Mesh> BuildChunkMapForLOD(ChunkLOD lod, float chunkSizeUnits)
    {
        // Initialize all BuildingInstance objects with generation parameters
        foreach (var instance in buildingInstances)
        {
            instance.SetGenerationParams(detailSettings, customOrigin.originLat, customOrigin.originLon, facadePreset);
        }

        ApplyLODSettings(lod);

        // Generate meshes for all buildings at this LOD
        List<Mesh> lodMeshes = new List<Mesh>();
        foreach (var instance in buildingInstances)
        {
            instance.GenerateMeshForLOD((int)lod);
            Mesh mesh = instance.GetLODMesh((int)lod);
            if (mesh != null)
                lodMeshes.Add(mesh);
        }

        // Group meshes by chunk
        Dictionary<Vector2Int, List<Mesh>> tempMap = new Dictionary<Vector2Int, List<Mesh>>();

        foreach (var mesh in lodMeshes)
        {
            Vector3 center = mesh.bounds.center;
            Vector2Int key = GetChunkKey(center, chunkSizeUnits);

            if (!tempMap.TryGetValue(key, out var list))
            {
                list = new List<Mesh>();
                tempMap[key] = list;
            }

            list.Add(mesh);
        }

        // Combine meshes per chunk
        Dictionary<Vector2Int, Mesh> finalMap = new Dictionary<Vector2Int, Mesh>();

        foreach (var kvp in tempMap)
        {
            var combine = new CombineInstance[kvp.Value.Count];

            for (int i = 0; i < kvp.Value.Count; i++)
            {
                combine[i] = new CombineInstance
                {
                    mesh = kvp.Value[i],
                    transform = Matrix4x4.identity
                };
            }

            Mesh combined = new Mesh();
            combined.CombineMeshes(combine, true, true, false);
            combined.RecalculateNormals();
            combined.RecalculateBounds();

            finalMap[kvp.Key] = combined;
        }

        return finalMap;
    }

    void ApplyLODSettings(ChunkLOD lod)
    {
        // Apply detail settings based on LOD level
        switch (lod)
        {
            case ChunkLOD.High: // LOD0 - highest detail
                detailSettings.verticalDivisions = 4;
                detailSettings.metersPerHorizontalDivision = 3f;
                break;

            case ChunkLOD.Medium: // LOD1 - medium detail
                detailSettings.verticalDivisions = 2;
                detailSettings.metersPerHorizontalDivision = 5f;
                break;

            case ChunkLOD.Low: // LOD2 - lowest detail
                detailSettings.verticalDivisions = 1;
                detailSettings.metersPerHorizontalDivision = 10f;
                break;
        }
    }

    static Vector2Int GetChunkKey(Vector3 position, float chunkSizeUnits)
    {
        int x = Mathf.FloorToInt(position.x / chunkSizeUnits);
        int z = Mathf.FloorToInt(position.z / chunkSizeUnits);
        return new Vector2Int(x, z);
    }
}