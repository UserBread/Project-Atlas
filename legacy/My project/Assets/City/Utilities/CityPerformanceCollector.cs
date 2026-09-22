using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CityPerformanceCollector : MonoBehaviour
{
    [Header("References")]
    public CityManager cityManager;
    public BuildingGenerator buildingGenerator;
    public RoadGenerator roadGenerator;
    public FootpathGenerator footpathGenerator;
    public CityChunkManager chunkManager;

    [Header("Reporting")]
    public bool autoReportOnCityGenerationComplete = true;
    public float reportDelayAfterGenerationSeconds = 2f;
    public bool saveReportToJsonFile = false;

    private float fpsSum = 0f;
    private int fpsSamples = 0;
    private float minFps = float.MaxValue;

    [Serializable]
    public class GeneratorMeshBreakdown
    {
        public int building;
        public int road;
        public int footpath;
    }

    [Serializable]
    public class GeneratorLoadBreakdown
    {
        public float building;
        public float road;
        public float footpath;
    }

    [Serializable]
    public class GeneratorGenerationBreakdown
    {
        public float building;
        public float road;
        public float footpath;
    }

    [Serializable]
    public class CityPerformanceSnapshot
    {
        public string timestampUtc;
        public float loadtime;
        public float generationtime;
        public GeneratorMeshBreakdown meshesgeneratedPerGenerator;
        public int trianglecount;
        public int chunkcount;
        public float averagefps;
        public float minfps;
        public int activechunks;
        public GeneratorLoadBreakdown loadtimePerGenerator;
        public GeneratorGenerationBreakdown generationtimePerGenerator;
    }

    private void Awake()
    {
        if (cityManager == null)
            cityManager = FindFirstObjectByType<CityManager>();
        if (buildingGenerator == null)
            buildingGenerator = FindFirstObjectByType<BuildingGenerator>();
        if (roadGenerator == null)
            roadGenerator = FindFirstObjectByType<RoadGenerator>();
        if (footpathGenerator == null)
            footpathGenerator = FindFirstObjectByType<FootpathGenerator>();
        if (chunkManager == null)
            chunkManager = FindFirstObjectByType<CityChunkManager>();
    }

    private void OnEnable()
    {
        if (cityManager != null)
            cityManager.CityGenerationCompleted += HandleCityGenerationCompleted;
    }

    private void OnDisable()
    {
        if (cityManager != null)
            cityManager.CityGenerationCompleted -= HandleCityGenerationCompleted;
    }

    private void Update()
    {
        if (Time.unscaledDeltaTime <= 0f)
            return;

        float fps = 1f / Time.unscaledDeltaTime;
        fpsSum += fps;
        fpsSamples++;
        if (fps < minFps)
            minFps = fps;
    }

    private void HandleCityGenerationCompleted()
    {
        if (!autoReportOnCityGenerationComplete)
            return;

        StartCoroutine(ReportWhenStable());
    }

    private IEnumerator ReportWhenStable()
    {
        if (reportDelayAfterGenerationSeconds > 0f)
            yield return new WaitForSecondsRealtime(reportDelayAfterGenerationSeconds);

        ReportMetrics();
    }

    public CityPerformanceSnapshot CollectSnapshot()
    {
        int buildingMeshes = buildingGenerator != null ? buildingGenerator.transform.childCount : 0;
        int roadMeshes = roadGenerator != null ? roadGenerator.transform.childCount : 0;
        int footpathMeshes = footpathGenerator != null ? footpathGenerator.transform.childCount : 0;

        int triangleCount = 0;
        if (buildingGenerator != null)
            triangleCount += buildingGenerator.totalTriangleCount;
        if (roadGenerator != null)
            triangleCount += roadGenerator.totalTriangleCount;
        if (footpathGenerator != null)
            triangleCount += footpathGenerator.totalTriangleCount;

        float buildingLoad = buildingGenerator != null ? buildingGenerator.loadTimeSeconds : 0f;
        float roadLoad = roadGenerator != null ? roadGenerator.loadTimeSeconds : 0f;
        float footpathLoad = footpathGenerator != null ? footpathGenerator.loadTimeSeconds : 0f;

        float buildingGeneration = buildingGenerator != null ? buildingGenerator.generationTimeSeconds : 0f;
        float roadGeneration = roadGenerator != null ? roadGenerator.generationTimeSeconds : 0f;
        float footpathGeneration = footpathGenerator != null ? footpathGenerator.generationTimeSeconds : 0f;

        float totalLoadTime = buildingLoad + roadLoad + footpathLoad;

        float totalGenerationTime = cityManager != null
            ? cityManager.totalGenerationTimeSeconds
            : (buildingGeneration + roadGeneration + footpathGeneration);

        int totalChunks = chunkManager != null ? chunkManager.TotalChunkCount : 0;
        int activeChunks = chunkManager != null ? chunkManager.ActiveChunkCount : 0;

        float averageFps = fpsSamples > 0 ? fpsSum / fpsSamples : 0f;
        float finalMinFps = minFps == float.MaxValue ? 0f : minFps;

        return new CityPerformanceSnapshot
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            loadtime = totalLoadTime,
            generationtime = totalGenerationTime,
            meshesgeneratedPerGenerator = new GeneratorMeshBreakdown
            {
                building = buildingMeshes,
                road = roadMeshes,
                footpath = footpathMeshes
            },
            trianglecount = triangleCount,
            chunkcount = totalChunks,
            averagefps = averageFps,
            minfps = finalMinFps,
            activechunks = activeChunks,
            loadtimePerGenerator = new GeneratorLoadBreakdown
            {
                building = buildingLoad,
                road = roadLoad,
                footpath = footpathLoad
            },
            generationtimePerGenerator = new GeneratorGenerationBreakdown
            {
                building = buildingGeneration,
                road = roadGeneration,
                footpath = footpathGeneration
            }
        };
    }

    public void ReportMetrics()
    {
        CityPerformanceSnapshot snapshot = CollectSnapshot();
        Debug.Log("[CityMetrics] " + JsonUtility.ToJson(snapshot, true));

        if (!saveReportToJsonFile)
            return;

        string fileName = "city_metrics_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json";
        string outputPath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(outputPath, JsonUtility.ToJson(snapshot, true));
        Debug.Log("[CityMetrics] Saved report to: " + outputPath);
    }
}
