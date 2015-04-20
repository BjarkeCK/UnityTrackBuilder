using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Track : LineBuilderComponent
{
    private float roadSegmentMinX = 2;
    private float roadSegmentMaxX = 2;
    private Vector3[] path = null;

    private void Awake()
    {
        SetMinMaxX(true);
    }

    private void SetMinMaxX(bool force)
    {
        var segment = this.transform.parent.GetComponentInChildren<TrackSegment>();
        if (segment != null)
        {
            var s = segment.GetLocalPoints().ToArray();
            roadSegmentMaxX = s.Max(x => x.x);
            roadSegmentMinX = s.Min(x => x.x);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Divide"))
        {
            Divide();
        }

        if (GUILayout.Button("Smooth intersections"))
        {
            Smooth(true);
        }

        if (GUILayout.Button("Smooth path"))
        {
            Smooth(false);
        }
    }

    private void Smooth(bool onlyBlurIntersections)
    {
        bool isClosed = IsClosedPath();
        if (isClosed)
        {
            GameObject.DestroyImmediate(this.transform.GetChild(this.transform.childCount - 1).gameObject);
        }
        Vector3[] avg = new Vector3[3];
        bool intersectionFound = true;
        int n = 0;
        while ((!onlyBlurIntersections || intersectionFound) && n < (onlyBlurIntersections ? 1000 : 3))
        {
            this.path = GetLocalPoints().ToArray();
            intersectionFound = false;
            for (int i = 0; i < this.transform.childCount; i++)
            {
                if (!onlyBlurIntersections || IsIntersecting(i, path))
                {
                    int prevI = i - 1;
                    if (prevI < 0) prevI = this.transform.childCount - 1;

                    int nextI = i + 1;
                    if (nextI >= this.transform.childCount) nextI = 0;

                    avg[0] = path[i];
                    avg[1] = path[prevI];
                    avg[2] = path[nextI];

                    if (isClosed == false && (i == 0 || i == this.transform.childCount - 1))
                    {
                        this.transform.GetChild(i).localPosition = (path[i] + path[i == 0 ? 1 : this.transform.childCount - 2]) / 2;
                    }
                    else
                    {
                        this.transform.GetChild(i).localPosition = new Vector3(avg.Sum(x => x.x), avg.Sum(x => x.y), avg.Sum(x => x.z)) / 3f;
                    }

                    intersectionFound = true;
                }
            }
            n++;
        }

        if (isClosed)
        {
            AddWaypoint(this.transform.GetChild(0).localPosition, this.transform.childCount);
        }

        this.path = GetLocalPoints().ToArray();
    }

    private bool IsIntersecting(int i, Vector3[] path)
    {
        Vector3 n1 = TrackBuilder.GetNormalXZ(path, i);
        Vector3 a = path[i] + n1 * roadSegmentMaxX;
        Vector3 b = path[i] + n1 * roadSegmentMinX;

        //for (int j = Mathf.Max(0, i - 1); j < Mathf.Min(path.Length, i + 1); j++)
        for (int j = 0; j < path.Length; j++)
        {
            if (i != j)
            {
                Vector3 n2 = TrackBuilder.GetNormalXZ(path, j);
                Vector3 c = path[j] + n2 * roadSegmentMaxX;
                Vector3 d = path[j] + n2 * roadSegmentMinX;
                if (MathUtilities.LineIntersects(a, b, c, d))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void Divide()
    {
        _snapToGrid = false;
        var points = GetLocalPoints().ToArray();
        Clear();
        int n = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 dir = (b - a).normalized;
            float distance = (int)Vector3.Distance(a, b);
            if (distance > 1)
            {
                float step = distance / (int)distance;

                for (int j = 0; j < (int)distance; j++)
                {
                    AddWaypoint(a + dir * j * step, n);
                    n++;
                }
            }
            else
            {
                AddWaypoint(points[i], n);
                n++;
            }
        }

        AddWaypoint(points.Last(), n);
    }

    private void AddWaypoint(Vector3 pos, int n)
    {
        var g = new GameObject(n + ".");
        g.transform.parent = this.transform;
        g.transform.localPosition = pos;
    }

    protected override void OnDrawGizmos()
    {
        Gizmos.color = IsClosedPath() ? Color.green : new Color(1, 0.64f, 0);

        if (IsVisible)
        {
            if (path == null || IsDirty)
            {
                path = GetLocalPoints().ToArray();
            }

            SetMinMaxX(false);

            if (path != null)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    Vector3 c = this.transform.TransformPoint(path[i]);
                    Vector3 n = TrackBuilder.GetNormalXZ(path, i);
                    Vector3 a = c + n * roadSegmentMaxX;
                    Vector3 b = c + n * roadSegmentMinX;
                    //Gizmos.color = IsIntersecting(i, path) ? Color.red : Color.green;
                    Gizmos.DrawLine(c, a);
                    Gizmos.DrawLine(c, b);

                    //Gizmos.DrawLine(c, c + n * roadSegmentMaxX);
                    //Gizmos.DrawLine(c, c + n * roadSegmentMinX);
                }
            }
        }
        base.OnDrawGizmos();
    }
}