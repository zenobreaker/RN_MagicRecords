using UnityEngine;

public class WarningSign_Fan : WarningSign
{
    private MeshFilter mainMeshFilter;
    private MeshFilter subMeshFilter;

    protected override void OnEnable()
    {
        base.OnEnable();

        InitializeMesh();
    }

    private void InitializeMesh()
    {
        if (mainPlane == null || subPlane == null)
            return;

        mainMeshFilter = mainPlane.GetComponent<MeshFilter>();
        subMeshFilter = subPlane.GetComponent<MeshFilter>();

        if (mainMeshFilter == null || subMeshFilter == null)
            return;

        // Main의 Mesh를 Sub에도 동일하게 적용
        subMeshFilter.sharedMesh = mainMeshFilter.sharedMesh;
    }

    public override void Setup(IWarningData data, float duration)
    {
        base.Setup(data, duration);

        if (data == null)
            return;

        SetFan(data.FanRadius, data.FanAngle);
    }

    public void SetFan(float radius, float angle)
    {
        if (mainMeshFilter == null || subMeshFilter == null)
            InitializeMesh();

        if (mainMeshFilter == null || subMeshFilter == null)
            return;

        Mesh fanMesh = CreateFanMesh(radius, angle);

        if (fanMesh == null)
            return;

        mainMeshFilter.sharedMesh = fanMesh;
        subMeshFilter.sharedMesh = fanMesh;
    }

    private Mesh CreateFanMesh(float radius, float angle)
    {
        int segments = Mathf.Max(2, Mathf.CeilToInt(angle / 5f));

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float halfAngle = angle * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;

            float currentAngle = Mathf.Lerp(
                -halfAngle,
                halfAngle,
                t
            );

            float radian = currentAngle * Mathf.Deg2Rad;

            vertices[i + 1] = new Vector3(
                Mathf.Sin(radian) * radius,
                0f,
                Mathf.Cos(radian) * radius
            );
        }

        for (int i = 0; i < segments; i++)
        {
            int index = i * 3;

            triangles[index] = 0;
            triangles[index + 1] = i + 1;
            triangles[index + 2] = i + 2;
        }

        Mesh mesh = new Mesh
        {
            name = $"FanWarning_{radius}_{angle}"
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}