using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BezierPath : LineBuilderComponent
{
    protected override void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.64f, 0);
        Vector3[] arr = GetLocalPoints().ToArray();

        base.OnDrawGizmos();
        Gizmos.color = Color.green;
        List<Vector3> points = new List<Vector3>();
        for (int j = 0; j < arr.Length; j += 2)
        {
            if (j + 3 <= arr.Length)
            {
                Vector3 abDir = (arr[j + 1] - arr[j + 0]).normalized;
                Vector3 bcDir = (arr[j + 2] - arr[j + 1]).normalized;
                float abDis = Vector3.Distance(arr[j + 0], arr[j + 1]);
                float bcDis = Vector3.Distance(arr[j + 1], arr[j + 2]);
                float step = 1 / (abDis + bcDis);
                for (float v = 0; v <= 2; v += step)
                {
                    if (v > 1) v = 1;
                    Vector3 a = arr[j + 0] + abDir * abDis * v;
                    Vector3 b = arr[j + 1] + bcDir * bcDis * v;
                    Vector3 dir = (b - a).normalized;
                    float dis = Vector3.Distance(a, b);
                    points.Add(a + dir * dis * v);
                    if (v == 1) break;
                }
            }
        }
        for (int i = 0; i < points.Count - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }
    }
}