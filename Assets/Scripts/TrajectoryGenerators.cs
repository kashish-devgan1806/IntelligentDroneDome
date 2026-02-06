using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility class that creates control points and returns an interpolated Catmull-Rom path.
/// Also creates special families: arc (climb->dive), zig-zag evasive, and S-curve.
/// </summary>
public static class TrajectoryGenerator
{
    // Generate a direct path control points (start -> mid -> target)
    public static Vector3[] GenerateDirectPath(Vector3 start, Vector3 target, float altitude)
    {
        Vector3 p0 = start;
        Vector3 p1 = Vector3.Lerp(start, target, 0.35f) + Vector3.up * Mathf.Max(0, altitude * 0.25f);
        Vector3 p2 = Vector3.Lerp(start, target, 0.7f) + Vector3.up * Mathf.Max(0, altitude * 0.05f);
        Vector3 p3 = target;
        return new Vector3[] { p0, p1, p2, p3 };
    }

    // Arc: climb then dive toward target
    public static Vector3[] GenerateArcPath(Vector3 start, Vector3 target, float peakAltitude)
    {
        Vector3 mid = Vector3.Lerp(start, target, 0.5f);
        Vector3 p0 = start;
        Vector3 p1 = Vector3.Lerp(start, mid, 0.33f) + Vector3.up * (peakAltitude * 0.6f);
        Vector3 p2 = Vector3.Lerp(mid, target, 0.66f) + Vector3.up * (peakAltitude * 0.3f);
        Vector3 p3 = target;
        return new Vector3[] { p0, p1, p2, p3 };
    }

    // S-curve
    public static Vector3[] GenerateSCurve(Vector3 start, Vector3 target, float lateralMagnitude, int segments = 4)
    {
        Vector3 dir = (target - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        List<Vector3> pts = new List<Vector3>();
        pts.Add(start);

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / (segments + 1);
            Vector3 basePoint = Vector3.Lerp(start, target, t);
            float s = Mathf.Sin(t * Mathf.PI * 2f); // S-shape oscillation
            Vector3 offset = right * s * lateralMagnitude;
            pts.Add(basePoint + offset + Vector3.up * Mathf.Lerp(0.2f, -0.1f, t) * lateralMagnitude);
        }

        pts.Add(target);
        return pts.ToArray();
    }

    // Zig-zag evasive pattern (alternating lateral offsets)
    public static Vector3[] GenerateZigZag(Vector3 start, Vector3 target, float lateral, int zigCount = 4)
    {
        Vector3 dir = (target - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        List<Vector3> pts = new List<Vector3>();
        pts.Add(start);
        for (int i = 1; i <= zigCount; i++)
        {
            float t = (float)i / (zigCount + 1);
            Vector3 basePoint = Vector3.Lerp(start, target, t);
            float sign = (i % 2 == 0) ? -1f : 1f;
            pts.Add(basePoint + right * lateral * sign + Vector3.up * Random.Range(-3f, 3f));
        }
        pts.Add(target);
        return pts.ToArray();
    }

    // Catmull-Rom interpolation (uniform) sampling
    public static Vector3[] SampleCatmullRom(Vector3[] controlPts, int samplesPerSegment = 16)
    {
        if (controlPts == null || controlPts.Length < 2) return new Vector3[0];

        List<Vector3> outPts = new List<Vector3>();

        // For endpoints, use duplicated neighbors to keep endpoints exact
        Vector3[] pts = new Vector3[controlPts.Length + 2];
        pts[0] = controlPts[0];
        for (int i = 0; i < controlPts.Length; i++) pts[i + 1] = controlPts[i];
        pts[pts.Length - 1] = controlPts[controlPts.Length - 1];

        for (int i = 0; i < pts.Length - 3; i++)
        {
            Vector3 p0 = pts[i];
            Vector3 p1 = pts[i + 1];
            Vector3 p2 = pts[i + 2];
            Vector3 p3 = pts[i + 3];

            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                Vector3 p = CatmullRom(p0, p1, p2, p3, t);
                outPts.Add(p);
            }
        }

        // ensure final point is the last control point
        outPts.Add(controlPts[controlPts.Length - 1]);
        return outPts.ToArray();
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // standard Catmull-Rom spline
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
