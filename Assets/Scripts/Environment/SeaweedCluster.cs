using UnityEngine;

[ExecuteInEditMode]
public class SeaweedCluster : MonoBehaviour
{
    [Header("Sample Mesh")]
    public Mesh seaweedSampleMesh; // Образец одной водоросли

    [Header("Cluster Settings")]
    [Range(5, 100)] public int seaweedCount = 12;
    [Range(1f, 20f)] public float clusterRadius = 2f;
    [Range(0.5f, 3f)] public float minScale = 0.8f;
    [Range(0.5f, 3f)] public float maxScale = 1.2f;

    [Header("Auto-Generation")]
    public bool autoGenerate = true;
    public Material clusterMaterial;

    private Mesh generatedMesh;

    void Start()
    {
        if (autoGenerate)
            GenerateMeshFromSample();
    }

    void OnValidate()
    {
        if (autoGenerate && !Application.isPlaying && seaweedSampleMesh != null)
            GenerateMeshFromSample();
    }

    [ContextMenu("Generate Mesh From Sample")]
    public void GenerateMeshFromSample()
    {
        if (seaweedSampleMesh == null)
        {
            Debug.LogWarning("No sample mesh assigned!");
            return;
        }

        generatedMesh = CreateClusterFromSample();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = generatedMesh;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
        if (clusterMaterial != null) renderer.sharedMaterial = clusterMaterial;
    }

    Mesh CreateClusterFromSample()
    {
        Mesh mesh = new Mesh();
        mesh.name = $"SeaweedCluster_FromSample_{seaweedCount}";

        // Получаем данные из образца
        Vector3[] sampleVertices = seaweedSampleMesh.vertices;
        Vector2[] sampleUV = seaweedSampleMesh.uv;
        int[] sampleTriangles = seaweedSampleMesh.triangles;

        int verticesPerSeaweed = sampleVertices.Length;
        int trianglesPerSeaweed = sampleTriangles.Length;

        Vector3[] vertices = new Vector3[seaweedCount * verticesPerSeaweed];
        Vector2[] uv = new Vector2[seaweedCount * verticesPerSeaweed];
        int[] triangles = new int[seaweedCount * trianglesPerSeaweed];

        System.Random random = new System.Random(GetInstanceID());

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < seaweedCount; i++)
        {
            // Случайная позиция и поворот
            float randomAngle = (float)random.NextDouble() * Mathf.PI * 2f;
            float randomDistance = (float)random.NextDouble() * clusterRadius;
            float randomRotation = (float)random.NextDouble() * 360f;
            float randomScale = minScale + (float)random.NextDouble() * (maxScale - minScale);

            Vector2 randomPos = new Vector2(
                Mathf.Cos(randomAngle) * randomDistance,
                Mathf.Sin(randomAngle) * randomDistance
            );

            Vector3 basePosition = new Vector3(randomPos.x, 0, randomPos.y);
            Quaternion rotation = Quaternion.Euler(0, randomRotation, 0);

            // Копируем и трансформируем вершины образца
            for (int v = 0; v < verticesPerSeaweed; v++)
            {
                // Применяем поворот, масштаб и позицию
                Vector3 transformedVertex = rotation * (sampleVertices[v] * randomScale) + basePosition;
                vertices[vertexIndex + v] = transformedVertex;
                uv[vertexIndex + v] = sampleUV[v]; // UV остаются теми же
            }

            // Копируем треугольники (с учетом смещения вершин)
            for (int t = 0; t < trianglesPerSeaweed; t++)
            {
                triangles[triangleIndex + t] = sampleTriangles[t] + vertexIndex;
            }

            vertexIndex += verticesPerSeaweed;
            triangleIndex += trianglesPerSeaweed;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}