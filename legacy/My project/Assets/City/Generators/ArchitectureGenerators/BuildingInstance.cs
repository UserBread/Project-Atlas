using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


// Represents a single building with its geometry and LOD meshes.
// Orchestrates mesh generation through specialized LOD builders.

public class BuildingInstance
{
    // Building geometry data
    public JArray rings { get; private set; }
    public double heightMeters { get; private set; }
    public int buildingIndex { get; private set; }

    // LOD meshes: [LOD0, LOD1, LOD2]
    public Mesh[] lodMeshes = new Mesh[3];

    // References for generation
    private DetailSettings detailSettings;
    private double originLat;
    private double originLon;
    private FacadePreset facadePreset;
    private int currentLOD = -1;

    public BuildingInstance(JArray rings, double heightMeters, int buildingIndex)
    {
        this.rings = rings;
        this.heightMeters = heightMeters;
        this.buildingIndex = buildingIndex;
    }

    
    // Initializes generation parameters before generating meshes.
    // Must be called before GenerateMeshForLOD.
    
    public void SetGenerationParams(DetailSettings detailSettings, double originLat, double originLon, FacadePreset facadePreset)
    {
        this.detailSettings = detailSettings;
        this.originLat = originLat;
        this.originLon = originLon;
        this.facadePreset = facadePreset;
    }

    
    // Generates the mesh for a specific LOD level and stores it.
    
    public void GenerateMeshForLOD(int lodIndex)
    {
        if (lodIndex < 0 || lodIndex > 2)
        {
            Debug.LogError($"Invalid LOD index {lodIndex}. Must be 0, 1, or 2.");
            return;
        }

        currentLOD = lodIndex;
        Mesh mesh = GenerateBuildingMesh();
        if (mesh != null)
        {
            lodMeshes[lodIndex] = mesh;
        }
    }

    
    // Gets the mesh for a specific LOD, with fallback logic.
    
    public Mesh GetLODMesh(int lodIndex)
    {
        if (lodIndex < 0 || lodIndex > 2)
            return null;

        // If the requested LOD doesn't exist, fall back to nearest available
        if (lodMeshes[lodIndex] != null)
            return lodMeshes[lodIndex];

        // Fallback: higher detail LOD
        for (int i = lodIndex - 1; i >= 0; i--)
        {
            if (lodMeshes[i] != null)
                return lodMeshes[i];
        }

        // Fallback: lower detail LOD
        for (int i = lodIndex + 1; i < 3; i++)
        {
            if (lodMeshes[i] != null)
                return lodMeshes[i];
        }

        return null;
    }

    private Mesh GenerateBuildingMesh()
    {
        return currentLOD switch
        {
            0 => new LOD0MeshBuilder().GenerateLOD0Mesh(rings, heightMeters, buildingIndex, detailSettings, 
                                                        originLat, originLon, facadePreset),
            1 => new LOD1MeshBuilder().GenerateLOD1Mesh(rings, heightMeters, buildingIndex, detailSettings, 
                                                        originLat, originLon, facadePreset),
            2 => new LOD2MeshBuilder().GenerateLOD2Mesh(rings, heightMeters, buildingIndex, detailSettings, 
                                                        originLat, originLon, facadePreset),
            _ => null
        };
    }
}

