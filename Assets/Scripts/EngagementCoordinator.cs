using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Keeps one-to-one mapping of intruder -> interceptor assignments.
/// Periodically tries to match free interceptors to nearest intruders by threat (distance to core).
/// </summary>
public class EngagementCoordinator : MonoBehaviour
{
    private List<DroneController> interceptors = new List<DroneController>();
    private Dictionary<IntruderMeta, DroneController> assignments = new Dictionary<IntruderMeta, DroneController>();

    public float assignmentCooldown = 0.25f;
    private float lastAssignmentTime;

    void Start()
    {
        var dm = DroneManager.Instance;
        if (dm != null) interceptors = dm.GetInterceptors();
    }

    void Update()
    {
        if (Time.time - lastAssignmentTime < assignmentCooldown) return;

        var intruders = FindObjectsOfType<IntruderMeta>().Where(x => x != null).ToList();
        if (intruders.Count == 0) return;

        // sort by threat: distance to core (closer = higher priority)
        var scored = intruders.OrderBy(i => Vector3.Distance(i.transform.position, DroneManager.Instance.ProtectedCore.position)).ToList();

        foreach (var intr in scored)
        {
            if (assignments.ContainsKey(intr)) continue;

            // find nearest free interceptor
            var free = interceptors.Where(d => d != null && !d.IsAssigned()).OrderBy(d => Vector3.SqrMagnitude(d.transform.position - intr.transform.position)).ToList();
            if (free.Count == 0) continue;

            var chosen = free[0];
            bool ok = chosen.AssignTarget(intr.transform, OnInterceptorFreed);
            if (ok)
            {
                assignments[intr] = chosen;
                lastAssignmentTime = Time.time;
                Debug.Log($"[LOG] interceptor assigned at {chosen.transform.position} -> target {intr.transform.position}");
            }
        }

        // cleanup dead assignments
        var keys = assignments.Keys.ToList();
        foreach (var k in keys)
        {
            var ic = assignments[k];
            if (ic == null || k == null) { assignments.Remove(k); continue; }
            if (!k.gameObject.activeInHierarchy)
            {
                ic.ReleaseAssignment();
                assignments.Remove(k);
            }
        }
    }

    private void OnInterceptorFreed(DroneController ic)
    {
        var entry = assignments.FirstOrDefault(kv => kv.Value == ic);
        if (!entry.Equals(default(KeyValuePair<IntruderMeta, DroneController>)))
        {
            assignments.Remove(entry.Key);
        }
    }

    public void OnIntruderNeutralized(IntruderMeta meta)
    {
        if (assignments.ContainsKey(meta))
            assignments.Remove(meta);
    }

    public void OnMissionEnded(bool success)
    {
        foreach (var ic in interceptors) if (ic != null) ic.ForceStop();
        Debug.Log("[EngagementCoordinator] mission ended: " + success);
    }
}
