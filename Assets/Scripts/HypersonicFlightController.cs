using UnityEngine;

/// <summary>
/// FIXED: Compatible with new DroneManager field names
/// Fast approach for specialized intruder prefab
/// </summary>
[RequireComponent(typeof(DroneController))]
public class HypersonicFlightController : MonoBehaviour
{
    public Transform approachTarget;
    public float approachSpeed = 250f;

    private DroneController dc;
    private Vector3 zoneMin;
    private Vector3 zoneMax;
    private bool initialized = false;

    void Start()
    {
        dc = GetComponent<DroneController>();
        
        if (approachTarget == null && DroneManager.Instance != null)
        {
            approachTarget = DroneManager.Instance.ProtectedCore;
        }

        if (approachTarget == null)
        {
            Debug.LogError("[Hypersonic] ProtectedCore not found in scene!");
            enabled = false;
            return;
        }

        // Get bounds from DroneManager
        if (DroneManager.Instance != null)
        {
            // Use world bounds (allows crossing fence)
            zoneMin = new Vector3(-100f, 0f, -1100f);
            zoneMax = new Vector3(2100f, 6f, 1100f);
            
            // Configure DroneController
            dc.ConfigureAsIntruder(zoneMin, zoneMax, approachTarget);
            
            initialized = true;
        }
        else
        {
            Debug.LogError("[Hypersonic] DroneManager.Instance is null!");
            enabled = false;
        }
    }

    void Update()
    {
        if (!initialized || approachTarget == null) return;

        // Move toward target with hypersonic speed
        Vector3 dir = (approachTarget.position - transform.position).normalized;
        Vector3 next = transform.position + dir * approachSpeed * Time.deltaTime;

        // Clamp to zone bounds
        next.x = Mathf.Clamp(next.x, zoneMin.x, zoneMax.x);
        next.y = Mathf.Clamp(next.y, zoneMin.y, Mathf.Min(zoneMax.y, dc.maxHeight));
        next.z = Mathf.Clamp(next.z, zoneMin.z, zoneMax.z);

        transform.position = next;
        
        // Face movement direction
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        }
    }

    public void SetTarget(Transform t)
    {
        approachTarget = t;
    }
}