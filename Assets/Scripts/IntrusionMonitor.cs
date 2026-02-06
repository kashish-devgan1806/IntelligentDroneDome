using UnityEngine;

/// <summary>
/// Optional component attached to intruder or protected core area to check for breach and emergency assignment.
/// We removed "dangerous area" — assignment is direct: if intruder close to core (< emergencyAssignDistance) it will request nearest free interceptor.
/// </summary>
[RequireComponent(typeof(IntruderMeta))]
public class IntrusionMonitor : MonoBehaviour
{
    public Transform protectedCore;
    public float emergencyAssignDistance = 200f;
    public float autoKillDistance = 60f; // intruder auto-neutralize by inner ring? optional

    IntruderMeta meta;
    bool neutralized = false;

    void Start()
    {
        meta = GetComponent<IntruderMeta>();
        if (protectedCore == null && DroneManager.Instance != null) protectedCore = DroneManager.Instance.ProtectedCore;
        if (protectedCore == null) Debug.LogWarning("[IntrusionMonitor] protectedCore not assigned.");
    }

    void Update()
    {
        if (protectedCore == null || neutralized) return;
        float d = Vector3.Distance(transform.position, protectedCore.position);

        if (d < emergencyAssignDistance)
        {
            EmergencyAssignNearest();
        }

        if (d < autoKillDistance)
        {
            neutralized = true;
            meta.OnNeutralized();
            return;
        }

        // breach handled by IntruderMeta distance check now
    }

    private void EmergencyAssignNearest()
    {
        var all = DroneManager.Instance?.GetInterceptors();
        if (all == null || all.Count == 0) return;

        DroneController nearest = null;
        float best = float.MaxValue;
        foreach (var d in all)
        {
            if (d == null) continue;
            if (!d.IsInterceptor()) continue;
            if (d.IsAssigned()) continue;
            float s = (d.transform.position - transform.position).sqrMagnitude;
            if (s < best) { best = s; nearest = d; }
        }

        if (nearest != null)
        {
            bool ok = nearest.AssignTarget(transform, (DroneController ic) => { /* freed */ });
            if (ok)
            {
                Debug.Log($"[LOG] target locked at {transform.position}");
            }
        }
    }

    void OnCollisionEnter(Collision c)
    {
        // interceptors can collide to neutralize
        if (c.collider.CompareTag("Interceptor"))
        {
            if (!neutralized)
            {
                neutralized = true;
                meta.OnNeutralized();
            }
        }
    }
}
