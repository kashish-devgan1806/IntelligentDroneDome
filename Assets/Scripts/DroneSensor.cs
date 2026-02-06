using UnityEngine;

/// <summary>
/// Simple LOS-based sensor that returns confidence [0..1] for a given intruder Transform.
/// </summary>
public class DroneSensor : MonoBehaviour
{
    public float maxRange = 300f;
    public LayerMask intruderLayer;
    public bool requireLOS = true;

    public float GetConfidence(Transform intruder)
    {
        if (intruder == null) return 0f;

        Vector3 intrPos;
        try { intrPos = intruder.position; } catch { return 0f; }

        float d = Vector3.Distance(transform.position, intrPos);
        if (d > maxRange) return 0f;

        if (requireLOS)
        {
            RaycastHit hit;
            Vector3 dir = (intrPos - transform.position).normalized;
            if (Physics.Raycast(transform.position, dir, out hit, maxRange))
            {
                if (hit.transform == null || hit.transform != intruder) return 0f;
            }
        }

        float baseConf = Mathf.Clamp01(1f - (d / maxRange));
        baseConf += Random.Range(-0.04f, 0.04f);
        return Mathf.Clamp01(baseConf);
    }
}
