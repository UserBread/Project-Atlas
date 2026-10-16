using System;
using UnityEngine;


// Defines the visual appearance of building facades including windows, doors, and colors.
// Create via Assets > Create > City > Facade Preset

[CreateAssetMenu(fileName = "New Facade Preset", menuName = "City/Facade Preset", order = 1)]
public class FacadePreset : ScriptableObject
{
    [Header("Window Settings")]
    [Tooltip("Width of windows in meters")]
    public float windowWidthMeters = 1.2f;
    
    [Tooltip("Height of windows in meters")]
    public float windowHeightMeters = 1.5f;
    
    [Tooltip("Number of windows per floor on each wall")]
    [Range(0, 10)]
    public int windowsPerFloor = 3;
    
    [Tooltip("Vertical offset from floor bottom (meters)")]
    public float windowVerticalOffset = 0.8f;
    
    [Tooltip("Window glass color")]
    public Color windowColor = new Color(0.4f, 0.6f, 0.8f, 1f);
    
    [Header("Door Settings")]
    [Tooltip("Width of doors in meters")]
    public float doorWidthMeters = 1.0f;
    
    [Tooltip("Height of doors in meters")]
    public float doorHeightMeters = 2.2f;
    
    [Tooltip("Door color")]
    public Color doorColor = new Color(0.4f, 0.25f, 0.1f, 1f);
    
    [Header("Wall Settings")]
    [Tooltip("Base wall color")]
    public Color wallColor = Color.gray;
    
    [Tooltip("Roof color")]
    public Color roofColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    [Tooltip("Floor color")]
    public Color floorColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    [Header("Style Settings")]
    [Tooltip("Building style category")]
    public BuildingStyle style = BuildingStyle.Modern;
    
    [Header("Geometry Settings")]
    [Tooltip("Wall thickness in meters")]
    public float wallThicknessMeters = 0.3f;
    
    [Tooltip("Inset offset for windows/doors in meters")]
    public float geometryInsetMeters = 0.02f;
    
    [Tooltip("Enable window frames (for future detail levels)")]
    public bool hasWindowFrames = false;
    
    [Tooltip("Enable balconies (for future detail levels)")]
    public bool hasBalconies = false;
}

[Serializable]
public enum BuildingStyle
{
    Modern,
    Classic,
    Industrial,
    Residential,
    Commercial
}
