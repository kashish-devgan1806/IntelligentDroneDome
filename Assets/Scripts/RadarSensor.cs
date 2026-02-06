using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cone sweep radar. Produces coarse detections: world position (approx), range, azimuth, confidence.
/// Attach to a GameObject (e.g. sensor tower). Call Sample() each update or let Fusion pull from it.
/// </summary>
[DisallowMultipleComponent]
public class RadarSensor : MonoBehaviour
{
    [Header("Coverage")]
    public float range = 2000f;
    [Range(0f, 360f)] public float fovDegrees = 90f;
    public float elevationFOV = 20f; // vertical acceptance

    [Header("Sweep")]
    public bool sweep = true;
    [Range(0.1f, 10f)] public float sweepSpeedDegPerSec = 30f;
    private float currentAzimuth = 0f;

    [Header("Detection")]
    [Range(0f,1f)] public float baseDetectionProb = 0.95f;
    public LayerMask detectionMask = ~0;
    public bool lineOfSightCheck = true;

    [Header("Noise")]
    public float rangeStdDev = 5f;      // meters
    public float angleStdDevDeg = 1f;   // degrees

    public struct RadarDetection
    {
        public Vector3 approxWorldPos;
        public float range;
        public float azimuth; // local azimuth deg
        public float elevation; // deg
        public float confidence;
        public Transform sourceTransform; // the actual target transform if known (null otherwise)
    }

    void Update()
    {
        if (sweep)
            currentAzimuth = (currentAzimuth + sweepSpeedDegPerSec * Time.deltaTime) % 360f;
    }

    /// <summary>
    /// Returns detections seen this frame. Passive scan when azimuthMask==null.
    /// </summary>
    public List<RadarDetection> Sample()
    {
        List<RadarDetection> outList = new List<RadarDetection>();

        // gather all intruders in roughly range (fast culling)
        var all = FindObjectsOfType<Transform>();
        // cheaper: find by tag if intruders tagged
        // but generic approach: query objects with IntruderMeta
        var intrs = FindObjectsOfType<IntruderMeta>();
        foreach (var im in intrs)
        {
            if (im == null) continue;
            Transform t = im.transform;
            Vector3 rel = t.position - transform.position;
            float r = rel.magnitude;
            if (r > range) continue;

            // compute azimuth / elevation
            Vector3 forward = transform.forward;
            float az = Vector3.SignedAngle(new Vector3(forward.x, 0, forward.z), new Vector3(rel.x, 0, rel.z), Vector3.up);
            float el = Vector3.SignedAngle(new Vector3(rel.x, 0, rel.z), rel, Vector3.Cross(Vector3.up, new Vector3(rel.x, 0, rel.z)));

            if (Mathf.Abs(az) > fovDegrees * 0.5f) continue;
            if (Mathf.Abs(el) > elevationFOV * 0.5f) continue;

            // LOS check
            bool los = true;
            if (lineOfSightCheck)
            {
                Ray rRay = new Ray(transform.position, rel.normalized);
                if (Physics.Raycast(rRay, out var hit, r, detectionMask))
                {
                    if (hit.transform != t) los = false;
                }
            }

            // detection probability (base * LOS modifier)
            float p = baseDetectionProb * (los ? 1f : 0.2f);
            if (UnityEngine.Random.value > p) continue;

            // apply noise
            float noisyRange = r + GaussianNoise(rangeStdDev);
            float noisyAz = az + GaussianNoise(angleStdDevDeg);
            float noisyEl = el + GaussianNoise(angleStdDevDeg * 0.5f);

            Vector3 approx = transform.position + Quaternion.Euler(0f, noisyAz, 0f) * transform.forward * noisyRange;
            RadarDetection d = new RadarDetection
            {
                approxWorldPos = approx,
                range = noisyRange,
                azimuth = noisyAz,
                elevation = noisyEl,
                confidence = Mathf.Clamp01(p),
                sourceTransform = t
            };

            outList.Add(d);
        }

        return outList;
    }

    private float GaussianNoise(float stddev)
    {
        // Box-Muller
        float u1 = 1.0f - UnityEngine.Random.value;
        float u2 = 1.0f - UnityEngine.Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return randStdNormal * stddev;
    }
}
