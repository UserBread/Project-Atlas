using System.Collections.Generic;
using UnityEngine;

public class CityChunkManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public BuildingGenerator buildingGenerator;
    public FootpathGenerator footpathGenerator;
    public RoadGenerator roadGenerator;
    public DetailSettings detailSettings;

    [Header("Chunk Settings")]
    private float chunkSizeUnits; // Calculated automatically from generator settings
    public LevelOfDetail levelOfDetail = new LevelOfDetail();

    private Dictionary<Vector2Int, List<GameObject>> chunkObjects =
        new Dictionary<Vector2Int, List<GameObject>>();

    private HashSet<MonoBehaviour> completedGenerators = new HashSet<MonoBehaviour>();
    private bool allGeneratorsCompleted = false;

    public int TotalChunkCount => chunkObjects.Count;
    public int ActiveChunkCount
    {
        get
        {
            int activeCount = 0;
            foreach (var kvp in chunkObjects)
            {
                var objects = kvp.Value;
                bool hasActiveObject = false;
                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i] != null && objects[i].activeSelf)
                    {
                        hasActiveObject = true;
                        break;
                    }
                }

                if (hasActiveObject)
                    activeCount++;
            }

            return activeCount;
        }
    }

    void OnEnable()
    {
        completedGenerators.Clear();
        allGeneratorsCompleted = false;
        SubscribeToGenerator(buildingGenerator);
        SubscribeToGenerator(roadGenerator);
        SubscribeToGenerator(footpathGenerator);
    }

    void OnDisable()
    {
        UnsubscribeFromGenerator(buildingGenerator);
        UnsubscribeFromGenerator(roadGenerator);
        UnsubscribeFromGenerator(footpathGenerator);
    }

    void Start()
    {
        // Calculate chunkSizeUnits from generator and DetailSettings
        if (buildingGenerator != null && detailSettings != null)
        {
            chunkSizeUnits = buildingGenerator.chunkingSettings.chunkSizeMeters * detailSettings.unitsPerMeter;
            Debug.Log($"ChunkSizeUnits calculated: {buildingGenerator.chunkingSettings.chunkSizeMeters}m * {detailSettings.unitsPerMeter} = {chunkSizeUnits}");
        }
        else
        {
            Debug.LogWarning("BuildingGenerator or DetailSettings not assigned in CityChunkManager!");
            chunkSizeUnits = 1f; // Fallback
        }
        
        if (HasAnyGeneratorChildren())
        {
            IndexChunks();
            UpdateChunkVisibility();
        }
    }

    void Update()
    {
        if (player == null)
            return;

        UpdateChunkVisibility();
        UpdateChunkLOD();
    }

    void IndexChunks()
    {
        chunkObjects.Clear();

        IndexFromGenerator(buildingGenerator);
        IndexFromGenerator(roadGenerator);
        IndexFromGenerator(footpathGenerator);
    }

    void IndexFromGenerator(MonoBehaviour generator)
    {
        if (generator == null)
            return;

        foreach (Transform child in generator.transform)
        {
            if (TryParseChunkCoords(child.name, out Vector2Int coord))
            {
                CityChunk chunk = child.GetComponent<CityChunk>();
                if (chunk == null)
                {
                    chunk = child.gameObject.AddComponent<CityChunk>();
                }
                chunk.coord = coord;
                chunk.SetDistances(levelOfDetail.highDistance, levelOfDetail.mediumDistance);
                chunk.SetChunkCenter(chunkSizeUnits);

                AddChunkObject(coord, child.gameObject);
            }

        }
    }

    void UpdateChunkLOD()
    {
        foreach (var kvp in chunkObjects)
        {
            List<GameObject> objects = kvp.Value;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null && objects[i].activeSelf)
                {
                    CityChunk chunk = objects[i].GetComponent<CityChunk>();
                    if (chunk != null)
                        chunk.UpdateLOD(player.position);
                }
            }
        }
    }

    void UpdateChunkVisibility()
    {
        float loadRadiusSq = levelOfDetail.loadRadius * levelOfDetail.loadRadius;

        foreach (var kvp in chunkObjects)
        {
            Vector2Int coord = kvp.Key;

            // Compare in world space so loadRadius is in Unity units (metres if unitsPerMeter = 1)
            float chunkCenterX = coord.x * chunkSizeUnits + chunkSizeUnits * 0.5f;
            float chunkCenterZ = coord.y * chunkSizeUnits + chunkSizeUnits * 0.5f;
            float dx = player.position.x - chunkCenterX;
            float dz = player.position.z - chunkCenterZ;
            bool shouldBeActive = (dx * dx + dz * dz) <= loadRadiusSq;

            List<GameObject> objects = kvp.Value;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                    objects[i].SetActive(shouldBeActive);
            }
        }
    }

    void AddChunkObject(Vector2Int coord, GameObject obj)
    {
        if (!chunkObjects.TryGetValue(coord, out List<GameObject> list))
        {
            list = new List<GameObject>();
            chunkObjects[coord] = list;
        }

        if (!list.Contains(obj))
            list.Add(obj);
    }

    Vector2Int GetChunkCoord(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkSizeUnits);
        int z = Mathf.FloorToInt(pos.z / chunkSizeUnits);
        return new Vector2Int(x, z);
    }

    static bool TryParseChunkCoords(string name, out Vector2Int coord)
    {
        coord = Vector2Int.zero;
        string[] parts = name.Split('_');
        if (parts.Length < 3) return false;

        if (!int.TryParse(parts[^2], out int x)) return false;
        if (!int.TryParse(parts[^1], out int z)) return false;

        coord = new Vector2Int(x, z);
        return true;
    }

    void SubscribeToGenerator(MonoBehaviour generator)
    {
        if (generator is BuildingGenerator building)
            building.GenerationCompleted += () => HandleGenerationCompleted(building);
        else if (generator is RoadGenerator road)
            road.GenerationCompleted += () => HandleGenerationCompleted(road);
        else if (generator is FootpathGenerator footpath)
            footpath.GenerationCompleted += () => HandleGenerationCompleted(footpath);
    }

    void UnsubscribeFromGenerator(MonoBehaviour generator) {}

    void HandleGenerationCompleted(MonoBehaviour generator)
    {
        completedGenerators.Add(generator);
        
        int totalGenerators = 0;
        if (buildingGenerator != null) totalGenerators++;
        if (roadGenerator != null) totalGenerators++;
        if (footpathGenerator != null) totalGenerators++;

        Debug.Log($"Generator completed: {generator.GetType().Name} ({completedGenerators.Count}/{totalGenerators})");

        if (completedGenerators.Count >= totalGenerators && !allGeneratorsCompleted)
        {
            allGeneratorsCompleted = true;
            Debug.Log("All generators completed! Indexing chunks...");
            IndexChunks();
            UpdateChunkVisibility();
        }
    }

    bool HasAnyGeneratorChildren()
    {
        return (buildingGenerator != null && buildingGenerator.transform.childCount > 0)
            || (roadGenerator != null && roadGenerator.transform.childCount > 0)
            || (footpathGenerator != null && footpathGenerator.transform.childCount > 0);
    }
}