using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class DataValidationTests
{
    [TestCase("downtownOrlando_buildings.geojson")]
    [TestCase("downtownOrlando_roads.geojson")]
    [TestCase("downtownOrlando_footpaths.geojson")]
    public void StreamingDatasets_Exist(string fileName)
    {
        Assert.That(File.Exists(CityTestSupport.GetStreamingAssetPath(fileName)), Is.True, fileName);
    }

    [TestCase("downtownOrlando_buildings.geojson")]
    [TestCase("downtownOrlando_roads.geojson")]
    [TestCase("downtownOrlando_footpaths.geojson")]
    public void StreamingDatasets_ParseToFeatureCollections(string fileName)
    {
        JObject root = CityTestSupport.LoadGeoJson(fileName);
        JArray features = (JArray)root["features"];

        Assert.That((string)root["type"], Is.EqualTo("FeatureCollection"));
        Assert.That(features, Is.Not.Null);
        Assert.That(features.Count, Is.GreaterThan(0));
    }

    [Test]
    public void BuildingDataset_AllFeaturesHaveSupportedPolygonGeometry()
    {
        JArray features = CityTestSupport.GetFeatures("downtownOrlando_buildings.geojson");

        foreach (JToken feature in features)
        {
            JToken geometry = feature["geometry"];
            Assert.That(geometry, Is.Not.Null);

            string type = (string)geometry["type"];
            Assert.That(type == "Polygon" || type == "MultiPolygon", Is.True, type);

            JToken coordinates = geometry["coordinates"];
            Assert.That(coordinates, Is.Not.Null);
            Assert.That(((JArray)coordinates).Count, Is.GreaterThan(0));
        }
    }

    [TestCase("downtownOrlando_roads.geojson")]
    [TestCase("downtownOrlando_footpaths.geojson")]
    public void LinearDatasets_AllFeaturesHaveAtLeastTwoPoints(string fileName)
    {
        JArray features = CityTestSupport.GetFeatures(fileName);

        foreach (JToken feature in features)
        {
            JToken geometry = feature["geometry"];
            Assert.That(geometry, Is.Not.Null);

            string type = (string)geometry["type"];
            Assert.That(type == "LineString" || type == "MultiLineString", Is.True, type);

            if (type == "LineString")
            {
                AssertValidCoordinateArray((JArray)geometry["coordinates"], 2);
                continue;
            }

            foreach (JArray line in (JArray)geometry["coordinates"])
            {
                AssertValidCoordinateArray(line, 2);
            }
        }
    }

    [TestCase("downtownOrlando_buildings.geojson")]
    [TestCase("downtownOrlando_roads.geojson")]
    [TestCase("downtownOrlando_footpaths.geojson")]
    public void GeometryConverter_CalculatesBoundingBoxCenter_ForRealDatasets(string fileName)
    {
        JArray features = CityTestSupport.GetFeatures(fileName);

        bool success = GeometryConverter.TryGetBoundingBoxCenter(features, out double centerLat, out double centerLon);

        Assert.That(success, Is.True);
        Assert.That(centerLat, Is.InRange(27.0d, 29.5d));
        Assert.That(centerLon, Is.InRange(-82.5d, -80.0d));
    }

    [Test]
    public void GeometryConverter_GetsOriginFromPolygonFeatures()
    {
        JArray features = CityTestSupport.GetFeatures("downtownOrlando_buildings.geojson");

        bool success = GeometryConverter.TryGetOriginFromPolygonFeatures(features, out double lat, out double lon);

        Assert.That(success, Is.True);
        Assert.That(lat, Is.InRange(27.0d, 29.5d));
        Assert.That(lon, Is.InRange(-82.5d, -80.0d));
    }

    [Test]
    public void GeometryConverter_GetsOriginFromLineFeatures()
    {
        JArray features = CityTestSupport.GetFeatures("downtownOrlando_roads.geojson");

        bool success = GeometryConverter.TryGetOriginFromLineFeatures(features, out double lat, out double lon);

        Assert.That(success, Is.True);
        Assert.That(lat, Is.InRange(27.0d, 29.5d));
        Assert.That(lon, Is.InRange(-82.5d, -80.0d));
    }

    private static void AssertValidCoordinateArray(JArray coords, int minimumCount)
    {
        Assert.That(coords, Is.Not.Null);
        Assert.That(coords.Count, Is.GreaterThanOrEqualTo(minimumCount));

        foreach (JArray coord in coords)
        {
            Assert.That(coord, Is.Not.Null);
            Assert.That(coord.Count, Is.GreaterThanOrEqualTo(2));

            double lon = (double)coord[0];
            double lat = (double)coord[1];
            Assert.That(double.IsNaN(lon), Is.False);
            Assert.That(double.IsNaN(lat), Is.False);
        }
    }
}
