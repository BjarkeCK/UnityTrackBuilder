using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;
using System.Collections.Generic;

#endif

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class RoadSegment : LineBuilderComponent
{
    private const int TEXTURE_SIZE = 2048;

    [SerializeField, HideInInspector]
    private bool _preview;

    [SerializeField, HideInInspector]
    private float _previewLength = 3;

    private Mesh _mesh;

    [SerializeField]
    private bool _liveBuilding = true;

    protected override void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (_preview && IsDirty)
        {
            UpdateMesh();
        }

        GetComponent<Renderer>().enabled = _preview && IsVisible;

        base.OnDrawGizmos();

        if (base.IsDirty)
        {
            foreach (var item in GetChildren()) { item.transform.localPosition = new Vector3(item.transform.localPosition.x, item.transform.localPosition.y, 0); }
        }
#endif
    }

    private void UpdateMesh()
    {
#if UNITY_EDITOR
        Vector3[] points = GetLocalPoints().ToArray();
        Vector3[] verts = new Vector3[points.Length * 2];
        Vector2[] uvs = new Vector2[points.Length * 2];
        float[] uvCords = new float[points.Length];
        int[] tris = new int[(verts.Length - 1) * 2 * 3];

        float totalUvLength = 0;
        float currUvLength = 0;
        int i = 0;
        for (i = 1; i < points.Length; i++) totalUvLength += Vector3.Distance(points[i - 1], points[i]);
        for (i = 1; i < points.Length; i++) { uvCords[i] = (currUvLength += Vector3.Distance(points[i - 1], points[i])) / totalUvLength; }

        i = 0;
        for (int z = -1; z <= 1; z += 2)
        {
            int offset = (z == -1) ? 0 : points.Length;
            for (int p = offset; p < offset + points.Length; p++)
            {
                verts[i] = points[p - offset];
                verts[i].z = _previewLength / 2f * z;
                uvs[i] = new Vector2(uvCords[p - offset], (z == -1) ? 0 : 1);
                i++;
            }
        }

        for (i = 0; i < points.Length - 1; i++)
        {
            int ti = i * 6;
            tris[ti + 0] = i;
            tris[ti + 1] = points.Length + i;
            tris[ti + 2] = points.Length + i + 1;
            tris[ti + 3] = i;
            tris[ti + 4] = points.Length + i + 1;
            tris[ti + 5] = i + 1;
        }

        if (_mesh == null)
        {
            this.GetComponent<MeshFilter>().sharedMesh = _mesh = new Mesh();
        }

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
        if (_liveBuilding)
        {
            this.transform.parent.GetComponent<RoadBuilder>().GenerateRoad();
        }
#endif
    }

    public override void OnInspectorGUI()
    {
#if UNITY_EDITOR
        base.OnInspectorGUI();

        if (_preview = EditorGUILayout.Toggle("Preview", _preview))
            if (_previewLength != (_previewLength = EditorGUILayout.FloatField("Preview Length", _previewLength)))
                UpdateMesh();

        if (GUILayout.Button("Generate Image"))
        {
            Texture2D texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE);
            float length = base.GetLength();
            int currentChild = 0;
            Color color = Color.green;
            var distinctPoints = base.GetLocalPointsDistinct();

            for (int x = 0; x < TEXTURE_SIZE; x++)
            {
                if (currentChild != (currentChild = GetChildIndex(x / (float)TEXTURE_SIZE, length, distinctPoints)))
                {
                    color = (color == Color.green ? Color.red : Color.green);
                }

                for (int y = 0; y < TEXTURE_SIZE; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            SaveTexture(texture);
        }
#endif
    }

    private int GetChildIndex(float f, float length, List<Vector3> distinctPoints)
    {
        float currLength = 0;
        Vector3 currPos = distinctPoints[0];
        for (int i = 1; i < distinctPoints.Count; i++)
        {
            currLength += Vector3.Distance(currPos, currPos = distinctPoints[i]);
            if (currLength / length > f)
            {
                return i - 1;
            }
        }
        return distinctPoints.Count - 1;
    }

    private void SaveTexture(Texture2D texture)
    {
#if UNITY_EDITOR
        BinaryWriter writer = new BinaryWriter(File.Create(Application.dataPath + "/" + this.gameObject.name + ".png"));
        writer.Write(texture.EncodeToPNG());
        writer.Close();
        AssetDatabase.Refresh();
        var test = AssetDatabase.LoadAssetAtPath("Assets/" + this.gameObject.name + ".png", typeof(Texture2D));
        Debug.Log("Image have been saved to " + "Assets/" + this.gameObject.name + ".png");
#endif
    }
}