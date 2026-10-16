using UnityEngine;
using System.Collections;
using System;

public class CityManager : MonoBehaviour
{
    [Header("Generators")]
    public BuildingGenerator buildingGenerator;
    public RoadGenerator roadGenerator;
    public FootpathGenerator footpathGenerator;

    public event Action CityGenerationCompleted;

    public float totalGenerationTimeSeconds { get; private set; }

    private void Start()
    {
        StartCoroutine(GenerateCity());
    }

    IEnumerator GenerateCity()
    {
        float generationStartTime = Time.realtimeSinceStartup;

        // Start roads and footpaths in parallel
        if (roadGenerator != null)
            roadGenerator.StartGeneration();

        if (footpathGenerator != null)
            footpathGenerator.StartGeneration();

        // Wait for both to complete
        bool roadsComplete = false;
        bool footpathsComplete = false;

        if (roadGenerator != null)
            roadGenerator.GenerationCompleted += () => { roadsComplete = true; };
        else
            roadsComplete = true;

        if (footpathGenerator != null)
            footpathGenerator.GenerationCompleted += () => { footpathsComplete = true; };
        else
            footpathsComplete = true;

        // Wait until both are done
        while (!roadsComplete || !footpathsComplete)
            yield return new WaitForSeconds(0.1f);

        // Now start buildings (which can access road centerlines)
        if (buildingGenerator != null)
        {
            bool buildingsComplete = false;
            Action onBuildingsCompleted = () => { buildingsComplete = true; };

            buildingGenerator.GenerationCompleted += onBuildingsCompleted;
            buildingGenerator.StartGeneration();

            while (!buildingsComplete)
                yield return null;

            buildingGenerator.GenerationCompleted -= onBuildingsCompleted;
        }

        totalGenerationTimeSeconds = Time.realtimeSinceStartup - generationStartTime;
        CityGenerationCompleted?.Invoke();
    }
}

