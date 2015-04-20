using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(TrackBuilder))]
public class RoadBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ((TrackBuilder)target).OnInspectorGUI();
    }
}

#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TrackBuilder : MonoBehaviour
{
    private void Start()
    {
        Debug.Log(MathUtilities.LineIntersects(new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(3, 0)));
        Debug.Log(MathUtilities.LineIntersects(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, -1), new Vector2(0.5f, 1)));
    }

    [SerializeField]
    private bool _worldUvCords = true;

    public void OnInspectorGUI()
    {
#if UNITY_EDITOR

        if (GUILayout.Button("Generate Road"))
        {
            GenerateRoad();
        }

        if (GUILayout.Button("Clear"))
        {
            GetComponent<MeshFilter>().sharedMesh = null;
            GetComponent<MeshCollider>().sharedMesh = null;
            GetComponent<MeshRenderer>().sharedMaterial = null;
        }

#endif
    }

    public void GenerateRoad()
    {
        TrackSegment roadSegment = GetComponentInChildren<TrackSegment>();
        Track path = GetComponentInChildren<Track>();

        Mesh mesh = GenerateMesh(roadSegment, path);

        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = roadSegment.GetComponent<Renderer>().sharedMaterial;
    }

    private Mesh GenerateMesh(TrackSegment roadSegment, Track path)
    {
        bool isClosed = roadSegment.IsClosedPath();
        Vector3[] roadSegmentArr = roadSegment.GetLocalPoints().ToArray();
        Vector3[] pathArr = path.GetLocalPoints().ToArray();
        List<Vector3> vertecies = new List<Vector3>(pathArr.Length);
        List<int> triangles = new List<int>(pathArr.Length * roadSegmentArr.Length * 6);
        List<Vector2> uvs = new List<Vector2>(pathArr.Length);
        float[] uvXCords = CalculateUvXCords(roadSegment);
        float totalPathLength = path.GetLength();
        float currentPathLength = 0;
        for (int n = 0; n < pathArr.Length - (isClosed ? 1 : 0); n++)
        {
            if (n != 0)
            {
                currentPathLength += Vector3.Distance(pathArr[n], pathArr[n - 1]);

                int prevRow = vertecies.Count - roadSegmentArr.Length;
                int currRow = vertecies.Count;
                for (int i = 0; i < roadSegmentArr.Length - 1; i++)
                {
                    int a = prevRow + i + 0;
                    int b = prevRow + i + 1;
                    int c = currRow + i + 0;
                    int d = currRow + i + 1;
                    triangles.Add(a);
                    triangles.Add(d);
                    triangles.Add(c);

                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(d);
                }
            }

            Vector3 normalXZ = GetNormalXZ(pathArr, n);
            Vector3 normalXY = GetNormalXY(pathArr, n, normalXZ, isClosed);
            for (int i = 0; i < roadSegmentArr.Length; i++)
            {
                Vector3 p = pathArr[n] + normalXZ * roadSegmentArr[i].x;
                p += normalXY * roadSegmentArr[i].y;

                vertecies.Add(p);
                uvs.Add(new Vector2(uvXCords[i], _worldUvCords ? currentPathLength * 0.1f : currentPathLength / totalPathLength));
            }
        }

        if (isClosed)
        {
            currentPathLength += Vector3.Distance(pathArr[0], pathArr[pathArr.Length - 2]);

            int prevRow = vertecies.Count - roadSegmentArr.Length;
            int currRow = vertecies.Count;
            for (int i = 0; i < roadSegmentArr.Length - 1; i++)
            {
                int a = prevRow + i + 0;
                int b = prevRow + i + 1;
                int c = currRow + i + 0;
                int d = currRow + i + 1;
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(c);

                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(d);
            }

            for (int i = 0; i < roadSegmentArr.Length; i++)
            {
                vertecies.Add(vertecies[i]);
                uvs.Add(new Vector2(uvXCords[i], _worldUvCords ? currentPathLength * 0.1f : currentPathLength / totalPathLength));
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertecies.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    public Vector3 GetNormalXY(Vector3[] pathArr, int i, Vector3 normalXZ, bool isClosed)
    {
        if (pathArr.Length <= 1) return Vector3.zero;
        if (isClosed)
        {
            Vector3 a = pathArr[i];
            Vector3 b = pathArr[i == 0 ? pathArr.Length - 1 : i - 1];
            Vector3 c = a + normalXZ;
            return Vector3.Cross(b - a, c - a).normalized;
        }
        else
        {
            Vector3 a = pathArr[i == 0 ? 1 : i];
            Vector3 b = pathArr[i == 0 ? 0 : i - 1];
            Vector3 c = a + normalXZ;
            return Vector3.Cross(b - a, c - a).normalized;
        }
    }

    public static Vector3 GetNormalXZ(Vector3[] pathArr, int i)
    {
        if (pathArr.Length <= 2) return Vector3.zero;
        bool isClosed = pathArr[0] == pathArr[pathArr.Length - 1];

        if (!isClosed && i == 0)
        {
            Vector3 a = pathArr[i];
            Vector3 b = pathArr[i + 1];
            return new Vector3(-(b.z - a.z), 0, b.x - a.x).normalized;
        }
        else if (!isClosed && i == pathArr.Length - 1)
        {
            Vector3 a = pathArr[i - 1];
            Vector3 b = pathArr[i];
            return new Vector3(-(b.z - a.z), 0, b.x - a.x).normalized;
        }
        else
        {
            Vector3 a = pathArr[i == 0 ? pathArr.Length - 2 : i - 1];
            Vector3 b = pathArr[i];
            Vector3 c = pathArr[i == pathArr.Length - 1 ? 1 : i + 1];
            Vector3 normalAB = new Vector3(-(b.z - a.z), 0, b.x - a.x).normalized;
            Vector3 normalBC = new Vector3(-(c.z - b.z), 0, c.x - b.x).normalized;
            return ((normalAB + normalBC) * 0.5f);
        }
    }

    private static float[] CalculateUvXCords(TrackSegment roadSegment)
    {
        Vector3[] children = roadSegment.GetLocalPoints().ToArray();
        float[] uvCords = new float[roadSegment.transform.childCount];
        float totalUvLength = roadSegment.GetLength();
        float currUvLength = 0;

        for (int i = 0; i < uvCords.Length; i++)
        {
            if (i > 0)
                currUvLength += Vector2.Distance(children[i - 1], children[i]);
            uvCords[i] = currUvLength / totalUvLength;
        }
        return uvCords;
    }
}