using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

public class PerformanceAndLoadTests
{
    [Test]
    [Category("Performance")]
    public void GeoUtils_ProjectsManyCoordinatesWithinBudget()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < 100000; i++)
        {
            double lat = 28.4d + i * 0.000001d;
            double lon = -81.5d + i * 0.000001d;
            GeoUtils.latLonToUnityPosition(lat, lon, 28.4d, -81.5d, 0d);
        }

        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2500), $"Projection run took {stopwatch.ElapsedMilliseconds} ms.");
    }

    [Test]
    [Category("Performance")]
    public void MeshTriangulation_RepeatedConcavePolygonRunsWithinBudget()
    {
        List<Vector2> polygon = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(8f, 0f),
            new Vector2(8f, 2f),
            new Vector2(5f, 2f),
            new Vector2(5f, 5f),
            new Vector2(3f, 5f),
            new Vector2(3f, 2f),
            new Vector2(0f, 2f)
        };

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 5000; i++)
        {
            MeshUtils.Triangulate(polygon);
        }
        stopwatch.Stop();

        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2500), $"Triangulation run took {stopwatch.ElapsedMilliseconds} ms.");
    }

    [Test]
    [Category("Performance")]
    public void LOD0MeshBuilder_GeneratesManyMeshesWithinBudget()
    {
        RoadGenerator.roadCenterlines.Clear();
        JArray rings = new JArray { CityTestSupport.CreateSquareRing(-81.3792d, 28.5383d, 0.00015d) };
        LOD0MeshBuilder builder = new LOD0MeshBuilder();
        DetailSettings detailSettings = new DetailSettings();
        FacadePreset facade = CityTestSupport.CreateFacadePreset();

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 250; i++)
            {
                Mesh mesh = builder.GenerateLOD0Mesh(rings, 12d, i, detailSettings, 28.5383d, -81.3792d, facade);
                Assert.That(mesh, Is.Not.Null);
            }
            stopwatch.Stop();

            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), $"LOD0 generation run took {stopwatch.ElapsedMilliseconds} ms.");
        }
        finally
        {
            Object.DestroyImmediate(facade);
        }
    }

    [Test]
    [Category("Load")]
    [Explicit("Large real-data load test.")]
    public void DisneyWorldBuildings_LoadsAndCalculatesBoundsWithinBudget()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        JObject root = CityTestSupport.LoadGeoJson("downtownOrlando_buildings.geojson");
        JArray features = (JArray)root["features"];
        bool success = GeometryConverter.TryGetBoundingBoxCenter(features, out double centerLat, out double centerLon);
        stopwatch.Stop();

        Assert.That(features, Is.Not.Null);
        Assert.That(features.Count, Is.GreaterThan(1000));
        Assert.That(success, Is.True);
        Assert.That(centerLat, Is.InRange(27.0d, 29.5d));
        Assert.That(centerLon, Is.InRange(-82.5d, -80.0d));
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(15000), $"Large dataset load took {stopwatch.ElapsedMilliseconds} ms.");
    }

    [Test]
    [Category("Load")]
    [Explicit("Exercises repeated full-dataset parsing across all shipped GeoJSON files.")]
    public void AllStreamingDatasets_CanBeParsedRepeatedly()
    {
        string[] datasetNames =
        {
            "downtownOrlando_buildings.geojson",
            "downtownOrlando_roads.geojson",
            "downtownOrlando_footpaths.geojson",
            "malinHead_buildings.geojson"
        };

        Stopwatch stopwatch = Stopwatch.StartNew();
        int totalFeatures = 0;

        for (int iteration = 0; iteration < 3; iteration++)
        {
            foreach (string datasetName in datasetNames)
            {
                JArray features = CityTestSupport.GetFeatures(datasetName);
                Assert.That(features, Is.Not.Null, datasetName);
                totalFeatures += features.Count;
            }
        }

        stopwatch.Stop();

        Assert.That(totalFeatures, Is.GreaterThan(0));
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(20000), $"Repeated dataset parsing took {stopwatch.ElapsedMilliseconds} ms.");
    }
}
