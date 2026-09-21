using UnityEngine;

public class CityChunk : MonoBehaviour
{
    public Vector2Int coord;
    public Vector3 center;

    private float highDistance;
    private float mediumDistance;

    public ChunkLOD currentLOD = ChunkLOD.High;

    private MeshFilter meshFilter;

    private Mesh highMesh;
    private Mesh mediumMesh;
    private Mesh lowMesh;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void SetDistances(float high, float medium)
    {
        highDistance = high;
        mediumDistance = medium;
    }

    public void SetChunkCenter(float chunkSizeUnits)
    {
        // Calculate world position from chunk coordinates
        center = new Vector3(
            coord.x * chunkSizeUnits + chunkSizeUnits * 0.5f,
            0f,
            coord.y * chunkSizeUnits + chunkSizeUnits * 0.5f
        );
    }

    public void SetLODMeshes(Mesh high, Mesh medium, Mesh low)
    {
        highMesh = high;
        mediumMesh = medium;
        lowMesh = low;

        ApplyLOD(); // Apply initial LOD
    }

    public void UpdateLOD(Vector3 playerPosition)
    {
        float distance = Vector3.Distance(playerPosition, center);

        ChunkLOD newLOD;

        if (distance <= highDistance)
            newLOD = ChunkLOD.High;
        else if (distance <= mediumDistance)
            newLOD = ChunkLOD.Medium;
        else
            newLOD = ChunkLOD.Low;

        if (newLOD != currentLOD)
        {
            // Debug.Log($"[{name}] Distance: {distance:F1} | LOD Change: {currentLOD} → {newLOD}");
            currentLOD = newLOD;
            ApplyLOD();
        }
    }

    void ApplyLOD()
    {
        if (meshFilter == null)
            return;

        Mesh oldMesh = meshFilter.mesh;
        int oldTriCount = oldMesh != null ? oldMesh.triangles.Length / 3 : 0;

        switch (currentLOD)
        {
            case ChunkLOD.High:
                meshFilter.mesh = highMesh;
                break;
            case ChunkLOD.Medium:
                meshFilter.mesh = mediumMesh;
                break;
            case ChunkLOD.Low:
                meshFilter.mesh = lowMesh;
                break;
        }

        int newTriCount = meshFilter.mesh != null ? meshFilter.mesh.triangles.Length / 3 : 0;
        // Debug.Log($"[{name}] LOD: {currentLOD} | Triangles: {oldTriCount} → {newTriCount}");
    }
}
