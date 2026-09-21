using System;
using UnityEngine;

public static class GeoUtils
{
    const double earthRadius = 6378137.0;

    public static Vector2 latLonToMercator(double latDeg, double lonDeg)
    {
        double x = earthRadius * deg2Rad(lonDeg);
        double latRad = deg2Rad(latDeg);
        double y = earthRadius * Math.Log(Math.Tan(Math.PI / 4.0 + latRad / 2.0));
        return new Vector2((float)x, (float)y);
    }

    static double deg2Rad(double deg) => deg * Math.PI / 180.0;

    public static Vector3 latLonToUnityPosition(double latDeg, double lonDeg, double originLatDeg, double originLonDeg, double height)
    {
        Vector2 mercator = latLonToMercator(latDeg, lonDeg);
        Vector2 mercatorOrigin = latLonToMercator(originLatDeg, originLonDeg);
        float x = mercator.x - mercatorOrigin.x;
        float y = (float)height;
        float z = mercator.y - mercatorOrigin.y;
        return new Vector3(x, y, z);
    }
}
