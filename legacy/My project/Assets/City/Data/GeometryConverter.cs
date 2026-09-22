using Newtonsoft.Json.Linq;

public static class GeometryConverter
{
    public static void ApplySharedOrigin(ref double originLat, ref double originLon, bool useCustomOrigin)
    {
        if (useCustomOrigin)
        {
            SharedGeoJsonOrigin.Lat = originLat;
            SharedGeoJsonOrigin.Lon = originLon;
            SharedGeoJsonOrigin.IsSet = true;
            return;
        }

        if (SharedGeoJsonOrigin.IsSet)
        {
            originLat = SharedGeoJsonOrigin.Lat;
            originLon = SharedGeoJsonOrigin.Lon;
            return;
        }

        SharedGeoJsonOrigin.Lat = originLat;
        SharedGeoJsonOrigin.Lon = originLon;
        SharedGeoJsonOrigin.IsSet = true;
    }

    public static bool TryGetOriginFromLineFeatures(JArray features, out double lat, out double lon)
    {
        lat = 0;
        lon = 0;

        foreach (var f in features)
        {
            var geom = f["geometry"];
            if (geom == null) continue;
            string type = (string)geom["type"];
            if (type == "LineString")
            {
                var coords = (JArray)geom["coordinates"];
                if (coords != null && coords.Count > 0)
                {
                    lon = (double)coords[0][0];
                    lat = (double)coords[0][1];
                    return true;
                }
            }
            else if (type == "MultiLineString")
            {
                var lines = (JArray)geom["coordinates"];
                if (lines != null && lines.Count > 0)
                {
                    var coords = (JArray)lines[0];
                    if (coords != null && coords.Count > 0)
                    {
                        lon = (double)coords[0][0];
                        lat = (double)coords[0][1];
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static bool TryGetOriginFromPolygonFeatures(JArray features, out double lat, out double lon)
    {
        lat = 0;
        lon = 0;

        foreach (var f in features)
        {
            var geom = f["geometry"];
            if (geom == null) continue;
            string type = (string)geom["type"];
            if (type == "Polygon")
            {
                var rings = (JArray)geom["coordinates"];
                if (TryGetFirstCoord(rings, out lat, out lon)) return true;
            }
            else if (type == "MultiPolygon")
            {
                var polys = (JArray)geom["coordinates"];
                if (polys != null && polys.Count > 0)
                {
                    var poly = (JArray)polys[0];
                    if (TryGetFirstCoord(poly, out lat, out lon)) return true;
                }
            }
        }

        return false;
    }

    static bool TryGetFirstCoord(JArray rings, out double lat, out double lon)
    {
        lat = 0;
        lon = 0;

        if (rings == null || rings.Count == 0) return false;

        var outer = (JArray)rings[0];
        if (outer == null || outer.Count == 0) return false;

        lon = (double)outer[0][0];
        lat = (double)outer[0][1];
        return true;
    }

    public static bool TryGetBoundingBoxCenter(JArray features, out double centerLat, out double centerLon)
    {
        centerLat = 0;
        centerLon = 0;

        if (features == null || features.Count == 0) return false;

        double minLat = double.MaxValue, maxLat = double.MinValue;
        double minLon = double.MaxValue, maxLon = double.MinValue;
        bool foundCoords = false;

        foreach (var f in features)
        {
            var geom = f["geometry"];
            if (geom == null) continue;
            string type = (string)geom["type"];
            
            if (type == "Polygon" || type == "MultiPolygon" || type == "LineString" || type == "MultiLineString")
            {
                CollectBoundsFromFeature(geom, type, ref minLat, ref maxLat, ref minLon, ref maxLon, ref foundCoords);
            }
        }

        if (!foundCoords) return false;

        centerLat = (minLat + maxLat) / 2.0;
        centerLon = (minLon + maxLon) / 2.0;
        return true;
    }

    static void CollectBoundsFromFeature(JToken geom, string type, ref double minLat, ref double maxLat, ref double minLon, ref double maxLon, ref bool found)
    {
        if (type == "Polygon")
        {
            var rings = (JArray)geom["coordinates"];
            CollectBoundsFromRings(rings, ref minLat, ref maxLat, ref minLon, ref maxLon, ref found);
        }
        else if (type == "MultiPolygon")
        {
            var polys = (JArray)geom["coordinates"];
            if (polys != null)
            {
                foreach (var poly in polys)
                {
                    CollectBoundsFromRings((JArray)poly, ref minLat, ref maxLat, ref minLon, ref maxLon, ref found);
                }
            }
        }
        else if (type == "LineString")
        {
            var coords = (JArray)geom["coordinates"];
            CollectBoundsFromCoords(coords, ref minLat, ref maxLat, ref minLon, ref maxLon, ref found);
        }
        else if (type == "MultiLineString")
        {
            var lines = (JArray)geom["coordinates"];
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    CollectBoundsFromCoords((JArray)line, ref minLat, ref maxLat, ref minLon, ref maxLon, ref found);
                }
            }
        }
    }

    static void CollectBoundsFromRings(JArray rings, ref double minLat, ref double maxLat, ref double minLon, ref double maxLon, ref bool found)
    {
        if (rings == null || rings.Count == 0) return;
        var outer = (JArray)rings[0];
        CollectBoundsFromCoords(outer, ref minLat, ref maxLat, ref minLon, ref maxLon, ref found);
    }

    static void CollectBoundsFromCoords(JArray coords, ref double minLat, ref double maxLat, ref double minLon, ref double maxLon, ref bool found)
    {
        if (coords == null || coords.Count == 0) return;

        foreach (var coord in coords)
        {
            var c = (JArray)coord;
            if (c != null && c.Count >= 2)
            {
                double lon = (double)c[0];
                double lat = (double)c[1];
                
                minLat = System.Math.Min(minLat, lat);
                maxLat = System.Math.Max(maxLat, lat);
                minLon = System.Math.Min(minLon, lon);
                maxLon = System.Math.Max(maxLon, lon);
                found = true;
            }
        }
    }
}
