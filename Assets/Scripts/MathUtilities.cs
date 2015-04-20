using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class MathUtilities
{
    public static bool LineIntersects(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        float ex, ey, fx, fy;
        ex = b.x - a.x; ey = b.z - a.z;
        fx = d.x - c.x; fy = d.z - c.z;

        float s, t;
        s = (-ey * (a.x - c.x) + ex * (a.z - c.z)) / (-fx * ey + ex * fy);
        t = (fx * (a.z - c.z) - fy * (a.x - c.x)) / (-fx * ey + ex * fy);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            return true;
        }

        return false;

        //float denominator = ((b.x - a.x) * (d.z - c.z)) - ((b.z - a.z) * (d.x - c.x));
        //float numerator1 = ((a.z - c.z) * (d.x - c.x)) - ((a.x - c.x) * (d.z - c.z));
        //float numerator2 = ((a.z - c.z) * (b.x - a.x)) - ((a.x - c.x) * (b.z - a.z));

        //if (denominator == 0) return numerator1 == 0 && numerator2 == 0;

        //float r = numerator1 / denominator;
        //float s = numerator2 / denominator;

        //return (r >= 0 && r <= 1) && (s >= 0 && s <= 1);
    }
}