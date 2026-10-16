using UnityEngine;

public class GroundGenerator : MonoBehaviour
{
    [SerializeField] private Material groundMaterial;
    [Tooltip("Size of the ground plane in meters")]
    [SerializeField] private float groundSizeMeters = 2000f;
    [Tooltip("Y position of the ground plane (buildings/roads sit above this)")]
    [SerializeField] private float groundYPosition = -0.1f;
    
    private void Start()
    {
        GenerateGround();
    }
    
    private void GenerateGround()
    {
        // Create a simple plane mesh covering the city area
        Mesh groundMesh = new Mesh();
        groundMesh.name = "GroundMesh";
        
        // Simple quad vertices (2 triangles to cover large area)
        Vector3 halfSize = Vector3.one * groundSizeMeters / 2f;
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-halfSize.x, groundYPosition, -halfSize.z),
            new Vector3(halfSize.x, groundYPosition, -halfSize.z),
            new Vector3(-halfSize.x, groundYPosition, halfSize.z),
            new Vector3(halfSize.x, groundYPosition, halfSize.z)
        };
        
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[]
        {
            0, 2, 1,
            1, 2, 3
        };
        
        groundMesh.vertices = vertices;
        groundMesh.uv = uvs;
        groundMesh.triangles = triangles;
        groundMesh.RecalculateNormals();
        
        // Create GameObject with the mesh
        GameObject groundObj = new GameObject("Ground");
        groundObj.transform.SetParent(transform);
        groundObj.transform.localPosition = Vector3.zero;
        
        MeshFilter meshFilter = groundObj.AddComponent<MeshFilter>();
        meshFilter.mesh = groundMesh;
        
        MeshRenderer meshRenderer = groundObj.AddComponent<MeshRenderer>();
        if (groundMaterial != null)
        {
            meshRenderer.material = groundMaterial;
        }
        
        MeshCollider meshCollider = groundObj.AddComponent<MeshCollider>();
        meshCollider.convex = false;
        
        Debug.Log($"Ground generated: {groundSizeMeters}m x {groundSizeMeters}m at Y={groundYPosition}");
    }
}
