using System;
using System.Linq;
using UnityEngine;

public class CatmullRom
{
    private Vector3[] curvePoints;
    public Vector3 p0;
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public Vector3 pointZero;
    public Vector3 pointLast;

    public CatmullRom(Vector3[] curvePoints)
    {
        if (curvePoints.Length < 2)
        {
            throw new ArgumentException("At least two points are needed for the CatmulRomSpline!");
        }
        
        this.curvePoints = curvePoints;
        this.curvePoints = (curvePoints.OrderBy(point => point.z)).ToArray();
        
        FindZeroAndLast();
    }

    private void FindZeroAndLast()
    {
        var len = curvePoints.Length;
        var first = curvePoints[0];
        var second = curvePoints[1];
        var dist = second - first;
        pointZero = first - dist.normalized;

        first = curvePoints[len - 2];
        second = curvePoints[len - 1];
        dist = second - first;
        pointLast = second + dist.normalized;
    }

    public void UpdatePoints(Vector3[] newPoints)
    {
        curvePoints = newPoints;
        curvePoints = (curvePoints.OrderBy(point => point.z)).ToArray();
    }
    
    public Vector3 Interpolate(Vector3 pointToInterpolate, float tension, float alpha)
    {
        var z = new float[curvePoints.Length];
        
        for (int i = 0; i < curvePoints.Length; i++)
        {
            z[i] = curvePoints[i].z;
        }
        
        var val = Array.BinarySearch(z, pointToInterpolate.z);
        if (val < 0)
            val = ~val;
        
        if (val == curvePoints.Length)
        {
            val--;
        }

        if (val == 0)
        {
            val++;
        }
        
        p0 = (val == 1) ? pointZero : curvePoints[val - 2];
        p1 = curvePoints[val - 1];
        p2 = curvePoints[val];
        p3 = (val == curvePoints.Length - 1) ? pointLast : curvePoints[val + 1];
        var distanceA = pointToInterpolate.z - p1.z;
        var distanceB = p2.z - p1.z;
        var t = distanceA / distanceB;
        
        return CreateCatmulRom(t, p0, p1, p2, p3, tension, alpha);;
    }
    
    private Vector3 CreateCatmulRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float tension, float alpha)
    {
        var t01 = (float)Math.Pow(Vector3.Distance(p0, p1), alpha);
        var t12 = (float)Math.Pow(Vector3.Distance(p1, p2), alpha);
        var t23 = (float)Math.Pow(Vector3.Distance(p2, p3), alpha);
        
        var m1 = (1.0f - tension) *
                 (p2 - p1 + t12 * ((p1 - p0) / t01 - (p2 - p0) / (t01 + t12)));
        var m2 = (1.0f - tension) *
                 (p2 - p1 + t12 * ((p3 - p2) / t23 - (p3 - p1) / (t12 + t23)));

        var a = 2.0f * (p1 - p2) + m1 + m2;
        var b = -3.0f * (p1 - p2) - m1 - m1 - m2;
        var c = m1;
        var d = p1;
        
        var point = a * (t * t * t) + b * (t * t) + c * t + d;
        
        return point;
    }
}
