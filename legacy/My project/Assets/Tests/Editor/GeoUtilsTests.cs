using NUnit.Framework;
using UnityEngine;

public class GeoUtilsTests
{
    [Test]
    public void LatLonToMercator_AtOrigin_ReturnsZero()
    {
        Vector2 mercator = GeoUtils.latLonToMercator(0d, 0d);

        Assert.That(mercator.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(mercator.y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void LatLonToUnityPosition_AtOrigin_ReturnsZeroOffsetAndPreservesHeight()
    {
        Vector3 position = GeoUtils.latLonToUnityPosition(28.5383d, -81.3792d, 28.5383d, -81.3792d, 12.5d);

        Assert.That(position.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(position.z, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(position.y, Is.EqualTo(12.5f).Within(0.0001f));
    }

    [Test]
    public void LatLonToUnityPosition_EastAndNorthOffsets_ArePositive()
    {
        Vector3 position = GeoUtils.latLonToUnityPosition(28.5393d, -81.3782d, 28.5383d, -81.3792d, 0d);

        Assert.That(position.x, Is.GreaterThan(0f));
        Assert.That(position.z, Is.GreaterThan(0f));
    }

    [Test]
    public void LatLonToUnityPosition_MatchesMercatorDelta()
    {
        Vector2 pointMercator = GeoUtils.latLonToMercator(28.5400d, -81.3750d);
        Vector2 originMercator = GeoUtils.latLonToMercator(28.5380d, -81.3800d);
        Vector3 unityPosition = GeoUtils.latLonToUnityPosition(28.5400d, -81.3750d, 28.5380d, -81.3800d, 3d);

        Assert.That(unityPosition.x, Is.EqualTo(pointMercator.x - originMercator.x).Within(0.01f));
        Assert.That(unityPosition.z, Is.EqualTo(pointMercator.y - originMercator.y).Within(0.01f));
        Assert.That(unityPosition.y, Is.EqualTo(3f).Within(0.0001f));
    }
}
