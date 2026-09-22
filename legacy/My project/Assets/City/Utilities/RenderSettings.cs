using System;
using UnityEngine;

[Serializable]
public class DetailSettings
{
    [Tooltip("Scale factor applied to meters -> Unity units")]
    public float unitsPerMeter = 1f;

    [Tooltip("Height of each floor in meters")]
    public float metersPerFloor = 3f;

    [Tooltip("Number of vertical divisions per floor for wall detail")]
    public int verticalDivisions = 5;

    [Tooltip("Meters per horizontal division on walls")]
    public float metersPerHorizontalDivision = 2f;
}

[Serializable]
public class SmoothingSettings
{
    [Tooltip("If true, performs Catmull-Rom smoothing on each polyline")]
    public bool smooth = true;

    [Range(1, 20)]
    public int smoothSubdivision = 2;
}

[Serializable]
public class ChunkingSettings
{
    public bool combinePerChunk = true;
    public float chunkSizeMeters = 100f;
}

[Serializable]
public class CustomOrigin
{
    [Tooltip("Optional override origin; if empty, first coordinate of the first LineString will be used")]
    public bool useCustomOrigin = false;
    public double originLat = 0;
    public double originLon = 0;
}

[Serializable]
public class LevelOfDetail
{
    [Tooltip("Radius in Unity units (metres if unitsPerMeter=1) around the player within which chunks are visible")]
    public float loadRadius = 500f;

    [Header("LOD Distances (Unity units from player)")]
    public float highDistance = 250;
    public float mediumDistance = 500;
}