using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class CityTestSupport
{
    public static string GetStreamingAssetPath(string fileName)
    {
        return Path.Combine(Application.dataPath, "StreamingAssets", fileName);
    }

    public static JObject LoadGeoJson(string fileName)
    {
        string path = GetStreamingAssetPath(fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Dataset not found: {path}", path);
        }

        return JObject.Parse(File.ReadAllText(path));
    }

    public static JArray GetFeatures(string fileName)
    {
        return (JArray)LoadGeoJson(fileName)["features"];
    }

    public static JArray CreateSquareRing(double minLon = 0.0, double minLat = 0.0, double size = 0.001)
    {
        return new JArray
        {
            new JArray(minLon, minLat),
            new JArray(minLon + size, minLat),
            new JArray(minLon + size, minLat + size),
            new JArray(minLon, minLat + size),
            new JArray(minLon, minLat)
        };
    }

    public static JArray CreateConcavePolygon()
    {
        return new JArray
        {
            new JArray(0.0, 0.0),
            new JArray(4.0, 0.0),
            new JArray(4.0, 1.0),
            new JArray(2.0, 1.0),
            new JArray(2.0, 3.0),
            new JArray(0.0, 3.0)
        };
    }

    public static float PolygonArea(IReadOnlyList<Vector2> polygon)
    {
        float area = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current = polygon[i];
            Vector2 next = polygon[(i + 1) % polygon.Count];
            area += current.x * next.y - next.x * current.y;
        }

        return Mathf.Abs(area) * 0.5f;
    }

    public static float TrianglesArea(IReadOnlyList<Vector2> polygon, IReadOnlyList<int> indices)
    {
        float area = 0f;
        for (int i = 0; i < indices.Count; i += 3)
        {
            Vector2 a = polygon[indices[i]];
            Vector2 b = polygon[indices[i + 1]];
            Vector2 c = polygon[indices[i + 2]];
            area += Mathf.Abs((a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y)) * 0.5f);
        }

        return area;
    }

    public static FacadePreset CreateFacadePreset()
    {
        FacadePreset facade = ScriptableObject.CreateInstance<FacadePreset>();
        facade.windowsPerFloor = 2;
        facade.windowWidthMeters = 0.9f;
        facade.windowHeightMeters = 1.2f;
        facade.windowVerticalOffset = 0.8f;
        facade.doorWidthMeters = 1.0f;
        facade.doorHeightMeters = 2.1f;
        facade.wallThicknessMeters = 0.15f;
        facade.geometryInsetMeters = 0.02f;
        return facade;
    }
}
