using System.Collections.Generic;
using UnityEngine;

public static class MeshUtils
{
    public static float SignedArea(List<Vector2> poly)
    {
        int n = poly.Count;
        double a = 0.0;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = poly[i];
            Vector2 q = poly[(i + 1) % n];
            a += (double)p.x * q.y - (double)q.x * p.y;
        }
        return (float)(a * 0.5);
    }

    public static List<int> Triangulate(List<Vector2> poly)
    {
        int n = poly.Count;
        List<int> indices = new List<int>();
        if (n < 3) return indices;

        List<int> V = new List<int>(n);
        for (int i = 0; i < n; i++) V.Add(i);

        int guard = 0;
        while (V.Count > 3 && guard < 10000)
        {
            bool earFound = false;
            for (int i = 0; i < V.Count; i++)
            {
                int prev = V[(i - 1 + V.Count) % V.Count];
                int curr = V[i];
                int next = V[(i + 1) % V.Count];

                Vector2 a = poly[prev];
                Vector2 b = poly[curr];
                Vector2 c = poly[next];

                if (!IsConvex(a, b, c)) continue;

                bool hasPointInside = false;
                for (int j = 0; j < V.Count; j++)
                {
                    int vi = V[j];
                    if (vi == prev || vi == curr || vi == next) continue;
                    if (PointInTriangle(poly[vi], a, b, c))
                    {
                        hasPointInside = true;
                        break;
                    }
                }
                if (hasPointInside) continue;

                indices.Add(prev);
                indices.Add(curr);
                indices.Add(next);
                V.RemoveAt(i);
                earFound = true;
                break;
            }
            if (!earFound) break;
            guard++;
        }

        if (V.Count == 3)
        {
            indices.Add(V[0]);
            indices.Add(V[1]);
            indices.Add(V[2]);
        }

        return indices;
    }

    public static void CreateWallGrid(
        List<Vector3> vertices,
        List<int> tris,
        Vector3 bottomLeft,
        Vector3 bottomRight,
        float height,
        int verticalSteps,
        float unitsPerMeter,
        float metersPerHorizontalDivision,
        int floors,
        float floorHeight,
        int windowsPerFloor,
        bool hasDoor,
        List<Color> colours,
        FacadePreset facade = null)
    {
        // Use default colors if no facade provided
        Color windowColor = facade != null ? facade.windowColor : new Color(0f, 0.5f, 1f);
        Color doorColor = facade != null ? facade.doorColor : new Color(0.4f, 0.2f, 0f);
        Color wallColor = facade != null ? facade.wallColor : Color.gray;
        float wallLength = Vector3.Distance(bottomLeft, bottomRight);
        float wallLengthMeters = wallLength / unitsPerMeter;

        int horizontalDivisions = Mathf.Max(
            1,
            Mathf.FloorToInt(wallLengthMeters / metersPerHorizontalDivision)
        );

        int vertsPerRow = horizontalDivisions + 1;
        int baseIndex = vertices.Count;

        for (int y = 0; y <= verticalSteps; y++)
        {
            float ty = (float)y / verticalSteps;
            float yOffset = ty * height;

            for (int x = 0; x <= horizontalDivisions; x++)
            {
                float tx = (float)x / horizontalDivisions;
                Vector3 pos = Vector3.Lerp(bottomLeft, bottomRight, tx) + Vector3.up * yOffset;
                vertices.Add(pos);
                colours.Add(wallColor);
            }
        }

        for (int y = 0; y < verticalSteps; y++)
        {
            for (int x = 0; x < horizontalDivisions; x++)
            {
                int i00 = baseIndex + y * vertsPerRow + x;
                int i10 = i00 + 1;
                int i01 = i00 + vertsPerRow;
                int i11 = i01 + 1;

                tris.Add(i00);
                tris.Add(i01);
                tris.Add(i10);

                tris.Add(i10);
                tris.Add(i01);
                tris.Add(i11);
            }
        }

        Vector3 wallDir = (bottomRight - bottomLeft).normalized;
        Vector3 normal = Vector3.Cross(Vector3.up, wallDir).normalized;
        float facadeOffset = 0.05f;

        float wallWidth = Vector3.Distance(bottomLeft, bottomRight);
        float spacing = wallWidth / (windowsPerFloor + 1);

        for (int floor = 0; floor < floors; floor++)
        {
            float floorBottom = floor * floorHeight;
            float windowHeight = floorHeight * 0.6f;
            float windowBottomOffset = floorHeight * 0.2f;
            
            // Check if this floor has enough height for windows
            // Skip if window would extend beyond building height
            float windowTop = floorBottom + windowBottomOffset + windowHeight;
            if (windowTop > height)
            {
                continue; // Skip this floor's windows as they would be out of bounds
            }

            for (int i = 0; i < windowsPerFloor; i++)
            {
                float offset = spacing * (i + 1);
                Vector3 center = bottomLeft + wallDir * offset;

                bool isDoor =
                    hasDoor &&
                    floor == 0 &&
                    i == windowsPerFloor / 2;

                float width = wallWidth / (windowsPerFloor * 2.5f);

                Vector3 left = center - wallDir * width * 0.5f;
                Vector3 right = center + wallDir * width * 0.5f;

                int windowBaseIndex = vertices.Count;

                if (isDoor)
                {
                    float doorHeight = floorHeight * 0.9f;

                    vertices.Add(left + normal * facadeOffset);
                    vertices.Add(right + normal * facadeOffset);
                    vertices.Add(left + normal * facadeOffset + Vector3.up * doorHeight);
                    vertices.Add(right + normal * facadeOffset + Vector3.up * doorHeight);

                    for (int k = 0; k < 4; k++)
                        colours.Add(doorColor);
                }
                else
                {
                    vertices.Add(left + normal * facadeOffset + Vector3.up * (floorBottom + windowBottomOffset));
                    vertices.Add(right + normal * facadeOffset + Vector3.up * (floorBottom + windowBottomOffset));
                    vertices.Add(left + normal * facadeOffset + Vector3.up * (floorBottom + windowBottomOffset + windowHeight));
                    vertices.Add(right + normal * facadeOffset + Vector3.up * (floorBottom + windowBottomOffset + windowHeight));

                    for (int k = 0; k < 4; k++)
                        colours.Add(windowColor); 
                }

                tris.Add(windowBaseIndex + 0);
                tris.Add(windowBaseIndex + 2);
                tris.Add(windowBaseIndex + 1);

                tris.Add(windowBaseIndex + 1);
                tris.Add(windowBaseIndex + 2);
                tris.Add(windowBaseIndex + 3);
            }
        }
    }

    // Creates wall grid without windows or doors (plain solid wall)
    public static void CreateWallGridNoFenestration(
        List<Vector3> vertices,
        List<int> tris,
        Vector3 bottomLeft,
        Vector3 bottomRight,
        float height,
        int verticalSteps,
        List<Color> colours,
        Color wallColor)
    {
        float wallLength = Vector3.Distance(bottomLeft, bottomRight);
        
        int horizontalDivisions = Mathf.Max(1, Mathf.FloorToInt(wallLength / 5f)); // Simple grid division

        int vertsPerRow = horizontalDivisions + 1;
        int baseIndex = vertices.Count;

        for (int y = 0; y <= verticalSteps; y++)
        {
            float ty = (float)y / verticalSteps;
            float yOffset = ty * height;

            for (int x = 0; x <= horizontalDivisions; x++)
            {
                float tx = (float)x / horizontalDivisions;
                Vector3 pos = Vector3.Lerp(bottomLeft, bottomRight, tx) + Vector3.up * yOffset;
                vertices.Add(pos);
                colours.Add(wallColor);
            }
        }

        for (int y = 0; y < verticalSteps; y++)
        {
            for (int x = 0; x < horizontalDivisions; x++)
            {
                int i00 = baseIndex + y * vertsPerRow + x;
                int i10 = i00 + 1;
                int i01 = i00 + vertsPerRow;
                int i11 = i01 + 1;

                tris.Add(i00);
                tris.Add(i01);
                tris.Add(i10);

                tris.Add(i10);
                tris.Add(i01);
                tris.Add(i11);
            }
        }
    }

    static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0f;
    }

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = c - a;
        Vector2 v1 = b - a;
        Vector2 v2 = p - a;

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        float denom = dot00 * dot11 - dot01 * dot01;
        if (Mathf.Approximately(denom, 0f)) return false;
        float u = (dot11 * dot02 - dot01 * dot12) / denom;
        float v = (dot00 * dot12 - dot01 * dot02) / denom;
        return (u >= 0) && (v >= 0) && (u + v < 1);
    }
}
