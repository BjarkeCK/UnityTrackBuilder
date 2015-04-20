using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(RoadPathGenerator), true)]
public class RoadPathGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ((RoadPathGenerator)target).OnInspectorGUI();
    }
}

#endif

[RequireComponent(typeof(RoadPath))]
public class RoadPathGenerator : MonoBehaviour
{
    [SerializeField, Range(0, 0.35f)]
    private float complexity = 0.1f;

    [SerializeField, Range(30, 250)]
    private float size = 20;

    [SerializeField, Range(1, 1000)]
    public int smoothCount = 4;

    [SerializeField, Range(0, 100)]
    public float hillHeight = 4;

    [SerializeField, Range(0, 100)]
    public float hillSize = 50;

    [SerializeField]
    private string seed = "Empty";

    public void OnInspectorGUI()
    {
        if (GUILayout.Button("Generate"))
        {
            Clear();

            Vector2 startPos = FindIsland();
            var island = GetIsland(startPos);
            island = ExpandIsland(island);
            island = Outline(island);

            List<Vector3> road = CreatePath(island);
            SmoothRoad(road);
            CenterRoad(road);
            road.Add(road.First());
            for (int i = 0; i < road.Count; i++)
            {
                var g = new GameObject(i + ".");
                g.transform.parent = this.transform;
                g.transform.localPosition = road[i];
            }
            Selection.activeGameObject = this.gameObject;
        }

        if (GUILayout.Button("Clear"))
        {
            Clear();
        }
    }

    private void Clear()
    {
        GameObject[] gos = new GameObject[this.transform.childCount];
        for (int i = 0; i < gos.Length; i++)
            gos[i] = this.transform.GetChild(i).gameObject;
        for (int i = 0; i < gos.Length; i++)
            GameObject.DestroyImmediate(gos[i]);
    }

    private void CenterRoad(List<Vector3> roadOrdered)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        float maxZ = float.MinValue;

        foreach (var item in roadOrdered)
        {
            if (item.x < minX) minX = item.x;
            if (item.y < minY) minY = item.y;
            if (item.z < minZ) minZ = item.z;
            if (item.x > maxX) maxX = item.x;
            if (item.y > maxY) maxY = item.y;
            if (item.z > maxZ) maxZ = item.z;
        }

        Vector3 offset = new Vector3(minX, minY, minZ) + new Vector3((maxX - minX) / 2, (maxY - minY) / 2, (maxZ - minZ) / 2);

        for (int i = 0; i < roadOrdered.Count; i++)
            roadOrdered[i] -= offset;
    }

    private void SmoothRoad(List<Vector3> roadOrdered)
    {
        for (int n = 0; n < smoothCount; n++)
        {
            for (int i = 0; i < roadOrdered.Count; i++)
            {
                int prevI = i - 1;
                if (prevI < 0)
                    prevI = roadOrdered.Count - 1;

                int nextI = i + 1;
                if (nextI >= roadOrdered.Count)
                    nextI = 0;

                Vector3[] arr = new Vector3[] {
                     roadOrdered[i],
                     roadOrdered[prevI],
                     roadOrdered[nextI]
                };
                roadOrdered[i] = new Vector3(arr.Sum(x => x.x), arr.Sum(x => x.y), arr.Sum(x => x.z)) / 3f;
            }
        }
    }

    private HashSet<Vector2> Outline(HashSet<Vector2> island)
    {
        HashSet<Vector2> result = new HashSet<Vector2>();
        foreach (var p in island)
        {
            if (IsEdge(p, island))
            {
                result.Add(p);
                continue;
            }
        }

        return result;
    }

    private bool IsEdge(Vector2 p, HashSet<Vector2> island)
    {
        return
            // Left
                island.Contains(new Vector2(p.x - 1, p.y)) == false ||
            // Up
                island.Contains(new Vector2(p.x, p.y - 1)) == false ||
            // Right
                island.Contains(new Vector2(p.x + 1, p.y)) == false ||
            // Down
                island.Contains(new Vector2(p.x, p.y + 1)) == false;
    }

    private HashSet<Vector2> ExpandIsland(HashSet<Vector2> island)
    {
        HashSet<Vector2> expanded = new HashSet<Vector2>();
        foreach (var p in island)
        {
            if (IsEdge(p, island))
            {
                expanded.Add(new Vector2(p.x + 1, p.y + 0));
                expanded.Add(new Vector2(p.x - 1, p.y + 0));
                expanded.Add(new Vector2(p.x + 0, p.y + 1));
                expanded.Add(new Vector2(p.x + 0, p.y - 1));
                expanded.Add(new Vector2(p.x + 1, p.y + 1));
                expanded.Add(new Vector2(p.x + 1, p.y - 1));
                expanded.Add(new Vector2(p.x - 1, p.y + 1));
                expanded.Add(new Vector2(p.x - 1, p.y - 1));
            }
            expanded.Add(p);
        }
        return expanded;
    }

    private bool IsOnIsland(Vector2 point)
    {
        return SimplexNoise.Generate(40 + point.x / size, (seed.GetHashCode() / 3.33f) % 30, 40 + point.y / size) > (0.35f - complexity);
    }

    private HashSet<Vector2> GetIsland(Vector2 startPos)
    {
        HashSet<Vector2> visited = new HashSet<Vector2>();
        HashSet<Vector2> unvisited = new HashSet<Vector2>();
        unvisited.Add(startPos);

        do
        {
            Vector2 currentPoint = unvisited.First();
            unvisited.Remove(currentPoint);
            visited.Add(currentPoint);

            // Left
            Vector2 searchDirection = new Vector2(currentPoint.x - 1, currentPoint.y);
            if (!visited.Contains(searchDirection) && IsOnIsland(searchDirection)) unvisited.Add(searchDirection);
            // Up
            searchDirection = new Vector2(currentPoint.x, currentPoint.y + 1);
            if (!visited.Contains(searchDirection) && IsOnIsland(searchDirection)) unvisited.Add(searchDirection);
            // Right
            searchDirection = new Vector2(currentPoint.x + 1, currentPoint.y);
            if (!visited.Contains(searchDirection) && IsOnIsland(searchDirection)) unvisited.Add(searchDirection);
            // Down
            searchDirection = new Vector2(currentPoint.x, currentPoint.y - 1);
            if (!visited.Contains(searchDirection) && IsOnIsland(searchDirection)) unvisited.Add(searchDirection);
        } while (unvisited.Count != 0);

        return visited;
    }

    private Vector2 FindIsland()
    {
        Vector2 point = new Vector2(0, 0);

        while (IsOnIsland(point) == false)
        {
            point.x++;
            point.y++;
        }

        return point;
    }

    private List<Vector3> CreatePath(HashSet<Vector2> road)
    {
        HashSet<Vector3> result = new HashSet<Vector3>();

        Vector2 prevPoint = road.First();
        result.Add(new Vector3(prevPoint.x, GetHeight(prevPoint.x, prevPoint.y), prevPoint.y));
        Vector2[] directions = new Vector2[] {
            new Vector2(-1, +0),
            new Vector2(+1, +0),
            new Vector2(+0, +1),
            new Vector2(+0, -1),
            new Vector2(-1, -1),
            new Vector2(-1, +1),
            new Vector2(+1, -1),
            new Vector2(+1, +1)
        };

        while (true)
        {
            bool dirFound = false;
            foreach (var dir in directions)
            {
                Vector2 nextP = new Vector2(prevPoint.x + dir.x, prevPoint.y + dir.y);
                Vector3 nextV = new Vector3(prevPoint.x + dir.x, 0, prevPoint.y + dir.y);
                nextV.y = GetHeight(nextV.x, nextV.y);
                if (road.Contains(nextP) && result.Contains(nextV) == false)
                {
                    result.Add(nextV);
                    prevPoint = nextP;
                    dirFound = true;
                    break;
                }
            }
            if (dirFound == false)
            {
                break;
            }
        }
        return result.ToList();
    }

    private float GetHeight(float x, float z)
    {
        float t = 1 - hillSize / 100;
        return SimplexNoise.Generate(x / t, z / t, 0) * hillHeight;
    }
}