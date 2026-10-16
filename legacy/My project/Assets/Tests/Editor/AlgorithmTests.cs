using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

public class AlgorithmTests
{
    [Test]
    public void SignedArea_ReturnsPositiveForCounterClockwisePolygon_AndNegativeForClockwisePolygon()
    {
        List<Vector2> counterClockwise = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(2f, 0f),
            new Vector2(2f, 2f),
            new Vector2(0f, 2f)
        };

        List<Vector2> clockwise = new List<Vector2>(counterClockwise);
        clockwise.Reverse();

        Assert.That(MeshUtils.SignedArea(counterClockwise), Is.GreaterThan(0f));
        Assert.That(MeshUtils.SignedArea(clockwise), Is.LessThan(0f));
    }

    [Test]
    public void Triangulate_ConvexPolygon_CoversWholePolygonArea()
    {
        List<Vector2> polygon = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(2f, 0f),
            new Vector2(2f, 2f),
            new Vector2(0f, 2f)
        };

        List<int> indices = MeshUtils.Triangulate(polygon);

        Assert.That(indices, Has.Count.EqualTo(6));
        Assert.That(CityTestSupport.TrianglesArea(polygon, indices), Is.EqualTo(CityTestSupport.PolygonArea(polygon)).Within(0.001f));
    }

    [Test]
    public void Triangulate_ConcavePolygon_CoversWholePolygonArea()
    {
        JArray concave = CityTestSupport.CreateConcavePolygon();
        List<Vector2> polygon = new List<Vector2>();
        foreach (JArray coord in concave)
        {
            polygon.Add(new Vector2((float)coord[0], (float)coord[1]));
        }

        List<int> indices = MeshUtils.Triangulate(polygon);

        Assert.That(indices.Count % 3, Is.EqualTo(0));
        Assert.That(indices.Count, Is.EqualTo(12));
        Assert.That(CityTestSupport.TrianglesArea(polygon, indices), Is.EqualTo(CityTestSupport.PolygonArea(polygon)).Within(0.001f));
    }

    [Test]
    public void CleanupRingPoints_RemovesDuplicateClosingCoordinate()
    {
        JArray ring = CityTestSupport.CreateSquareRing();

        int pointCount = BuildingMeshHelper.CleanupRingPoints(ring);

        Assert.That(pointCount, Is.EqualTo(4));
    }

    [Test]
    public void GenerateLOD0Mesh_ForValidSquarePolygon_ReturnsMesh()
    {
        RoadGenerator.roadCenterlines.Clear();
        JArray rings = new JArray { CityTestSupport.CreateSquareRing(-81.3792d, 28.5383d, 0.0002d) };
        DetailSettings detailSettings = new DetailSettings();
        FacadePreset facade = CityTestSupport.CreateFacadePreset();

        try
        {
            Mesh mesh = new LOD0MeshBuilder().GenerateLOD0Mesh(rings, 12d, 1, detailSettings, 28.5383d, -81.3792d, facade);

            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.vertexCount, Is.GreaterThan(0));
            Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
            Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
        }
        finally
        {
            Object.DestroyImmediate(facade);
        }
    }

    [Test]
    public void GenerateLOD0Mesh_ForInvalidPolygon_ReturnsNull()
    {
        JArray rings = new JArray
        {
            new JArray
            {
                new JArray(-81.3792d, 28.5383d),
                new JArray(-81.3791d, 28.5383d)
            }
        };

        Mesh mesh = new LOD0MeshBuilder().GenerateLOD0Mesh(rings, 8d, 1, new DetailSettings(), 28.5383d, -81.3792d, null);

        Assert.That(mesh, Is.Null);
    }

    [Test]
    public void RoadCatmullRomSpline_IncreasesPointCount_AndPreservesEndpoints()
    {
        List<Vector3> source = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0.5f),
            new Vector3(2f, 0f, 1.5f),
            new Vector3(3f, 0f, 2.5f)
        };

        List<Vector3> result = InvokeRoadSpline(source, 3);

        Assert.That(result.Count, Is.GreaterThan(source.Count));
        Assert.That(result[0], Is.EqualTo(source[0]));
        Assert.That(result[result.Count - 1], Is.EqualTo(source[source.Count - 1]));
    }

    [Test]
    public void FootpathCatmullRomSpline_FlattensAllPointsToInitialHeight()
    {
        List<Vector3> source = new List<Vector3>
        {
            new Vector3(0f, 2f, 0f),
            new Vector3(1f, 3f, 0.5f),
            new Vector3(2f, 4f, 1.5f),
            new Vector3(3f, 5f, 2.5f)
        };

        List<Vector3> result = InvokeFootpathSpline(source, 2);

        Assert.That(result.Count, Is.GreaterThan(source.Count));
        foreach (Vector3 point in result)
        {
            Assert.That(point.y, Is.EqualTo(source[0].y).Within(0.0001f));
        }
    }

    private static List<Vector3> InvokeRoadSpline(List<Vector3> points, int subdivision)
    {
        MethodInfo method = typeof(RoadGenerator).GetMethod("CatmullRomSpline", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (List<Vector3>)method.Invoke(null, new object[] { points, subdivision });
    }

    private static List<Vector3> InvokeFootpathSpline(List<Vector3> points, int subdivision)
    {
        MethodInfo method = typeof(FootpathGenerator).GetMethod("CatmullRomSpline", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (List<Vector3>)method.Invoke(null, new object[] { points, subdivision });
    }
}
