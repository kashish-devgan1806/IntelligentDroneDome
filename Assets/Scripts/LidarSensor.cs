using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Short-range point-sampling lidar. Produces accurate positions (with small noise).
/// Lower range than radar but high accuracy at close-in distances.
/// </summary>
[DisallowMultipleComponent]
public class LidarSensor : MonoBehaviour
{
    [Header("LIDAR Specs")]
    public float maxRange = 120f;
    public int samplesPerScan = 64;
    public float verticalSpreadDeg = 10f;

    [Header("Detection")]
    public LayerMask detectionMask = ~0;
    [Range(0f,1f)] public float baseDetectionProb = 0.98f;
    public bool doLineOfSight = true;

    [Header("Noise")]
    public float rangeStdDev = 0.2f;   // less noisy than radar
    public float posJitter = 0.5f;

    public struct LidarHit
    {
        public Vector3 worldPos;
        public float confidence;
        public Transform sourceTransform;
    }

    public List<LidarHit> Sample()
    {
        var outList = new List<LidarHit>();
        var intrs = FindObjectsOfType<IntruderMeta>();
        foreach (var im in intrs)
        {
            if (im == null) continue;
            Vector3 rel = im.transform.position - transform.position;
            float r = rel.magnitude;
            if (r > maxRange) continue;

            // small detection failure chance
            if (UnityEngine.Random.value > baseDetectionProb) continue;

            // LOS
            bool los = true;
            if (doLineOfSight)
            {
                if (Physics.Raycast(transform.position, rel.normalized, out var hit, r, detectionMask))
                {
                    if (hit.transform != im.transform) los = false;
                }
            }

            float conf = los ? 1f : 0.6f;

            Vector3 jitter = new Vector3(Gauss(rangeStdDev), Gauss(rangeStdDev), Gauss(rangeStdDev)) + Random.insideUnitSphere * posJitter;
            var pos = im.transform.position + jitter;

            outList.Add(new LidarHit { worldPos = pos, confidence = conf, sourceTransform = im.transform });
        }
        return outList;
    }

    private float Gauss(float sd)
    {
        float u1 = 1.0f - Random.value;
        float u2 = 1.0f - Random.value;
        return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2) * sd;
    }
}
