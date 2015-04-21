using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class MathUtilities
{
    public static bool LineIntersects(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        float abX = b.x - a.x;
        float baZ = b.z - a.z;
        float dcX = d.x - c.x;
        float dcZ = d.z - c.z;

        float s = (-baZ * (a.x - c.x) + abX * (a.z - c.z)) / (-dcX * baZ + abX * dcZ);
        float t = (dcX * (a.z - c.z) - dcZ * (a.x - c.x)) / (-dcX * baZ + abX * dcZ);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            return true;
        }

        return false;
    }

    public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
              Func<TSource, TKey> selector)
    {
        return source.MaxBy(selector, Comparer<TKey>.Default);
    }

    public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
           Func<TSource, TKey> selector, IComparer<TKey> comparer)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (selector == null) throw new ArgumentNullException("selector");
        if (comparer == null) throw new ArgumentNullException("comparer");
        using (var sourceIterator = source.GetEnumerator())
        {
            if (!sourceIterator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }
            var max = sourceIterator.Current;
            var maxKey = selector(max);
            while (sourceIterator.MoveNext())
            {
                var candidate = sourceIterator.Current;
                var candidateProjected = selector(candidate);
                if (comparer.Compare(candidateProjected, maxKey) > 0)
                {
                    max = candidate;
                    maxKey = candidateProjected;
                }
            }
            return max;
        }
    }

    public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
           Func<TSource, TKey> selector)
    {
        return source.MinBy(selector, Comparer<TKey>.Default);
    }

    public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
        Func<TSource, TKey> selector, IComparer<TKey> comparer)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (selector == null) throw new ArgumentNullException("selector");
        if (comparer == null) throw new ArgumentNullException("comparer");
        using (var sourceIterator = source.GetEnumerator())
        {
            if (!sourceIterator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }
            var min = sourceIterator.Current;
            var minKey = selector(min);
            while (sourceIterator.MoveNext())
            {
                var candidate = sourceIterator.Current;
                var candidateProjected = selector(candidate);
                if (comparer.Compare(candidateProjected, minKey) < 0)
                {
                    min = candidate;
                    minKey = candidateProjected;
                }
            }
            return min;
        }
    }
}