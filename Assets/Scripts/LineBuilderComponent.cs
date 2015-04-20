using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(LineBuilderComponent), true)]
public class LineBuilderComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ((LineBuilderComponent)target).OnInspectorGUI();
    }
}

#endif

public abstract class LineBuilderComponent : MonoBehaviour
{
    private GameObject[] _currentlySelectedGameObjects;
    protected bool IsVisible;

    [SerializeField, HideInInspector]
    protected bool _snapToGrid = true;

    [SerializeField, HideInInspector]
    protected float _snapSize = 0.2f;

    private Vector3[] _prevPoints;

    protected virtual float PointSize { get { return 0.1f; } }

    public bool IsDirty { get; private set; }

    public IEnumerable<Vector3> GetLocalPoints()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            yield return transform.GetChild(i).localPosition;
        }
    }

    public void Clear()
    {
        GameObject[] gos = new GameObject[this.transform.childCount];
        for (int i = 0; i < gos.Length; i++)
            gos[i] = this.transform.GetChild(i).gameObject;
        for (int i = 0; i < gos.Length; i++)
            GameObject.DestroyImmediate(gos[i]);
    }

    public bool IsClosedPath()
    {
        if (this.transform.childCount == 0)
        {
            return false;
        }
        return this.transform.GetChild(0).localPosition == this.transform.GetChild(this.transform.childCount - 1).localPosition;
    }

    public List<Vector3> GetLocalPointsDistinct()
    {
        List<Vector3> result = new List<Vector3>();
        Vector3 prev = Vector3.zero;
        for (int i = 0; i < transform.childCount; i++)
        {
            var p = transform.GetChild(i).position;
            if (i == 0 || p != prev)
            {
                result.Add(p);
            }
            prev = p;
        }
        return result;
    }

    public IEnumerable<Vector3> GetWorldPoints()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            yield return transform.GetChild(i).position;
        }
    }

    public float GetLength()
    {
        float length = 0;
        Vector3 currPos = transform.GetChild(0).position;
        for (int i = 1; i < transform.childCount; i++)
        {
            length += Vector3.Distance(currPos, currPos = transform.GetChild(i).position);
        }
        return length;
    }

    protected Transform[] GetChildren()
    {
        Transform[] transforms = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            transforms[i] = transform.GetChild(i);
        }
        return transforms;
    }

    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        this.transform.localScale = Vector3.one;

        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            _currentlySelectedGameObjects = Selection.gameObjects;

        bool draw = false;

        // Draw if this is selected
        if (_currentlySelectedGameObjects != null && _currentlySelectedGameObjects.Contains(this.gameObject))
            draw = true;

        // Prevent deselcting when an object gets deleted.
        if (_currentlySelectedGameObjects != null && _currentlySelectedGameObjects.Length > 0 && _currentlySelectedGameObjects[0] == null && IsVisible)
            draw = true;
        else
        {
            // Draw if any child is selected
            for (int i = 0; i < transform.childCount; i++)
            {
                if (_currentlySelectedGameObjects != null && _currentlySelectedGameObjects.Contains(transform.GetChild(i).gameObject))
                {
                    if (draw)
                    {
                        Selection.objects = _currentlySelectedGameObjects.Where(x => x != this.gameObject).ToArray();
                    }
                    draw = true;
                    break;
                }
            }
        }

        if (draw)
        {
            var tmpPoints = GetLocalPoints().ToArray();
            float snapStep = 1 / _snapSize;
            bool repositiion = false;
            for (int i = 0; i < transform.childCount; i++)
            {
                var g1 = transform.GetChild(i + 0).transform;
                if (_snapToGrid)
                    g1.localPosition = new Vector3(
                        Mathf.Round(g1.localPosition.x * snapStep) / snapStep,
                        Mathf.Round(g1.localPosition.y * snapStep) / snapStep,
                        Mathf.Round(g1.localPosition.z * snapStep) / snapStep);

                if (g1.name != i.ToString() + ".")
                {
                    repositiion = true;
                }

                if (g1.GetComponent<MeshRenderer>() == null)
                {
                    GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    g1.gameObject.AddComponent<MeshFilter>().sharedMesh = g.GetComponent<MeshFilter>().sharedMesh;
                    var c = g1.gameObject.AddComponent<MeshRenderer>();
                    c.sharedMaterial = g.GetComponent<MeshRenderer>().sharedMaterial;
                    c.sharedMaterial.color = Color.red;
                    GameObject.DestroyImmediate(g);
                }

                g1.localScale = Vector3.one * PointSize;

                if (i + 1 != transform.childCount)
                {
                    var g2 = transform.GetChild(i + 1).transform;
                    Gizmos.DrawLine(g1.position, g2.position);
                }
            }

            if (repositiion)
            {
                RepositionChildren();
            }

            IsDirty = _prevPoints == null || (Enumerable.SequenceEqual(_prevPoints, tmpPoints)) == false;
            _prevPoints = tmpPoints;
        }

        if (IsVisible != draw)
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(draw);

        IsVisible = draw;
#endif
    }

    private void RepositionChildren()
    {
#if UNITY_EDITOR
        var children = GetChildren().Select(x => x.gameObject).ToArray();

        bool nameAll = false;
        foreach (var item in children)
        {
            string numberInName = Regex.Match(item.name, @"\d+").Value;
            if (string.IsNullOrEmpty(numberInName))
            {
                nameAll = true;
                break;
            }
            else
            {
                item.name = int.Parse(numberInName).ToString() + ".";
            }
        }

        if (nameAll)
        {
            int i = 0;
            foreach (var item in children)
            {
                item.name = i.ToString() + ".";
                i++;
            }
        }
        else
        {
            var positiions = children.OrderBy(x => int.Parse(x.name.Split('.')[0])).Select(x => new { pos = x.transform.localPosition, go = x }).ToArray();
            bool swapped = false;
            for (int i = 0; i < children.Length; i++)
            {
                children[i].name = i.ToString() + ".";
                if (children[i].transform.localPosition != positiions[i].pos)
                {
                    children[i].transform.localPosition = positiions[i].pos;
                    if (!swapped)
                    {
                        Selection.activeGameObject = children[i];
                        swapped = true;
                    }
                }
            }
        }
#endif
    }

    public virtual void OnInspectorGUI()
    {
#if UNITY_EDITOR
        _snapToGrid = EditorGUILayout.Toggle("Snap To Grid", _snapToGrid);

        if (_snapToGrid)
        {
            _snapSize = EditorGUILayout.FloatField("Snap Size", _snapSize);
        }
#endif
    }
}