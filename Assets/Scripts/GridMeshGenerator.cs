using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridMeshGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [Min(2)] public int xSegments = 100;
    [Min(2)] public int zSegments = 100;
    [Min(0.1f)] public float width = 50f;
    [Min(0.1f)] public float length = 50f;

    [Header("UV Settings")]
    public float uvScale = 1f;

    [Header("Generation")]
    public bool generateOnStart = true;

    private MeshFilter meshFilter;
    private Mesh generatedMesh;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    [ContextMenu("Generate Grid Mesh")]
    public void Generate()
    {
        // Create a new Mesh instance.
        generatedMesh = new Mesh();
        generatedMesh.name = "ProceduralWaterGrid";

        // Compute (xSegments + 1) * (zSegments + 1) vertices laid out on the XZ plane.
        int xVerts = xSegments + 1;
        int zVerts = zSegments + 1;
        int vertexCount = xVerts * zVerts;

        // Use UInt32 index format if the vertex count exceeds 65535.
        if (vertexCount > 65535)
        {
            generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        // Allocate the arrays.
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        // Calculate the step sizes.
        float xStep = width / xSegments;
        float zStep = length / zSegments;
        float halfWidth = width * 0.5f;
        float halfLength = length * 0.5f;

        // Setup the vertices and the UVs.
        for (int z = 0; z < zVerts; z++)
        {
            for (int x = 0; x < xVerts; x++)
            {
                int index = z * xVerts + x;
                float xPos = x * xStep - halfWidth;
                float zPos = z * zStep - halfLength;
                vertices[index] = new Vector3(xPos, 0f, zPos);

                // Assign UVs in the [0,1] x [0,1] range.
                float u = (float)x / xSegments * uvScale;
                float v = (float)z / zSegments * uvScale;
                uvs[index] = new Vector2(u, v);
            }
        }

        // Generate the triangles.
        int triangleCount = xSegments * zSegments * 6;
        int[] triangles = new int[triangleCount];

        int triIndex = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                // Calculate all the corner indices.
                int bottomLeft = z * xVerts + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * xVerts + x;
                int topRight = topLeft + 1;

                // Generate the triangles based on the indices.
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomRight;
            }
        }

        // Assign vertices / triangles / UVs to the mesh.
        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;

        // Recalculate the normals.
        generatedMesh.RecalculateNormals();

        // Recalculate the bounds.
        generatedMesh.RecalculateBounds();

        // Assign the mesh to the MeshFilter.
        meshFilter.mesh = generatedMesh;

        Debug.Log($"Generated water grid mesh: {vertexCount} vertices, {triangleCount / 3} triangles");
    }

    public void RegenerateMesh()
    {
        Generate();
    }
}