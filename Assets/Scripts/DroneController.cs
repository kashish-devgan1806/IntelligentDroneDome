using System;
using UnityEngine;

/// <summary>
/// UPDATED: Interceptors roam freely inside red zone, chase intruders that enter
/// </summary>
[RequireComponent(typeof(Collider))]
public class DroneController : MonoBehaviour
{
    public enum DroneRole { Interceptor, Intruder }
    public DroneRole Role = DroneRole.Interceptor;

    public bool isInterceptor => Role == DroneRole.Interceptor;
    public bool IsInterceptor() => Role == DroneRole.Interceptor;

    [Header("Movement")]
    public float speed = 60f;
    public float turnSpeed = 4f;
    public float maxHeight = 6f;

    [Header("Patrol (Interceptor)")]
    public float roamRadius = 100f; // Random roaming distance
    public float roamChangeInterval = 3f; // Change direction every 3 seconds

    [Header("Combat")]
    public float neutralizeDistance = 40f; // Dynamic, set by DroneManager

    // Interceptor state
    Transform assignedTarget;
    Action<DroneController> onFreeCallback;
    bool assigned = false;
    bool stopped = false;

    // Patrol state
    Vector3 roamTarget;
    float nextRoamTime;

    // Bounds
    private Vector3 zoneMin;
    private Vector3 zoneMax;

    // Intruder approach
    private Vector3 intruderTargetPosition;

    public Vector3 lastLoggedPosition => transform.position;

    public Transform GetAssignedTarget() => assignedTarget;

    public void ConfigureAsInterceptor(Vector3 zoneMin, Vector3 zoneMax)
    {
        Role = DroneRole.Interceptor;
        this.zoneMin = zoneMin;
        this.zoneMax = zoneMax;
        assigned = false;
        stopped = false;

        // Set initial roam target
        PickNewRoamTarget();
    }

    public void ConfigureAsIntruder(Vector3 zoneMin, Vector3 zoneMax, Transform core)
    {
        Role = DroneRole.Intruder;
        this.zoneMin = zoneMin;
        this.zoneMax = zoneMax;
        intruderTargetPosition = core != null ? core.position : Vector3.zero;
        assigned = false;
        stopped = false;
    }

    void Update()
    {
        if (stopped) return;

        // Clamp height
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, zoneMin.y, Mathf.Min(zoneMax.y, maxHeight));
        transform.position = pos;

        if (Role == DroneRole.Interceptor)
            InterceptorTick();
        else
            IntruderTick();
    }

    // ============================================
    // INTERCEPTOR: Roam freely or chase target
    // ============================================
    void InterceptorTick()
    {
        if (assigned && assignedTarget != null)
        {
            // Chase assigned target
            ChaseTarget();
        }
        else
        {
            // Free roam inside red zone
            FreeRoam();
        }
    }

    void FreeRoam()
    {
        // Pick new random target periodically
        if (Time.time >= nextRoamTime)
        {
            PickNewRoamTarget();
        }

        // Move toward roam target
        MoveTowards(roamTarget);

        // If reached target, pick new one
        if (Vector3.Distance(transform.position, roamTarget) < 10f)
        {
            PickNewRoamTarget();
        }
    }

    void PickNewRoamTarget()
    {
        // Random point inside red zone
        roamTarget = new Vector3(
            UnityEngine.Random.Range(zoneMin.x + 50f, zoneMax.x - 50f),
            UnityEngine.Random.Range(1f, 4f),
            UnityEngine.Random.Range(zoneMin.z + 50f, zoneMax.z - 50f)
        );

        nextRoamTime = Time.time + roamChangeInterval;
    }

    void ChaseTarget()
    {
        if (assignedTarget == null)
        {
            // Target lost, release assignment
            ReleaseAssignment();
            return;
        }

        Vector3 targetPos = assignedTarget.position;
        MoveTowards(targetPos);

        // Check neutralize distance
        float dist = Vector3.Distance(transform.position, targetPos);
        if (dist <= neutralizeDistance)
        {
            // Neutralize intruder
            var meta = assignedTarget.GetComponent<IntruderMeta>();
            if (meta != null)
            {
                Debug.Log($"[DroneController] {name} neutralized {assignedTarget.name} at distance {dist:F1}m");
                meta.OnNeutralized();
            }

            ReleaseAssignment();
        }
    }

    void OnNeutralize(IntruderMeta intruder)
    {
        // Existing code...
        
        // ADD THIS:
        if (TelemetryUIManager.Instance != null)
            TelemetryUIManager.Instance.RegisterIntercept(name, intruder.name);
        
        if (EnhancedCSVExporter.Instance != null)
            EnhancedCSVExporter.Instance.RecordIntercept(name, intruder.name, transform.position, true);
    }

    // ============================================
    // INTRUDER: Approach core
    // ============================================
    void IntruderTick()
    {
        Vector3 dir = (intruderTargetPosition - transform.position);
        if (dir.sqrMagnitude < 0.5f) return;

        // Curved approach with sine wave
        Vector3 lateral = Vector3.Cross(Vector3.up, dir.normalized) * 
                         Mathf.Sin(Time.time * 1.5f + transform.position.x) * 8f;
        Vector3 desired = intruderTargetPosition + lateral;
        
        MoveTowards(desired);
    }

    // ============================================
    // MOVEMENT
    // ============================================
    void MoveTowards(Vector3 worldTarget)
    {
        Vector3 dir = (worldTarget - transform.position);
        
        if (dir.sqrMagnitude > 0.001f)
        {
            // Smooth rotation
            Quaternion desiredRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * turnSpeed);
        }

        // Move forward
        Vector3 move = Vector3.MoveTowards(transform.position, worldTarget, speed * Time.deltaTime);
        move.y = Mathf.Clamp(move.y, zoneMin.y, Mathf.Min(zoneMax.y, maxHeight));
        transform.position = move;
    }

    // ============================================
    // ASSIGNMENT API
    // ============================================
    public bool AssignTarget(Transform target, Action<DroneController> onFree)
    {
        if (Role != DroneRole.Interceptor) return false;
        if (assigned) return false;
        if (target == null) return false;

        assignedTarget = target;
        assigned = true;
        onFreeCallback = onFree;
        
        Debug.Log($"[LOG] target locked at {target.position}");
        return true;
    }

    public void ReleaseAssignment()
    {
        assigned = false;
        assignedTarget = null;
        onFreeCallback?.Invoke(this);
        onFreeCallback = null;

        // Resume roaming
        PickNewRoamTarget();
    }

    public bool IsAssigned() => assigned;

    public void ForceStop()
    {
        stopped = true;
        assigned = false;
        assignedTarget = null;
    }

    public void OverrideSteering(Vector3 worldPoint)
    {
        if (!assigned) return;
        MoveTowards(worldPoint);
    }

    public void SetTemporarySpeed(float multiplier, float duration)
    {
        StartCoroutine(BoostRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator BoostRoutine(float mult, float dur)
    {
        float original = speed;
        speed = original * mult;
        yield return new WaitForSeconds(dur);
        speed = original;
    }

    // Add this field near other public fields (top of class)
    [Header("Movement / Boundary")]
    public bool intruderCanCrossFence = true; // set in inspector if you want intruders to roam freely

    // Replace existing ClampPositionToZone() with this implementation:
    private void ClampPositionToZone()
    {
        // Interceptors are clamped to their zone. Intruders may be allowed to cross.
        if (Role == DroneRole.Intruder && intruderCanCrossFence)
        {
            // allow intruders to roam freely — only clamp Y to keep them in playable height
            Vector3 p = transform.position;
            p.y = Mathf.Clamp(p.y, zoneMin.y, Mathf.Min(zoneMax.y, maxHeight));
            transform.position = p;
            return;
        }

        // For interceptors (or intruders if crossing disabled) clamp XZ + Y
        Vector3 q = transform.position;
        q.x = Mathf.Clamp(q.x, zoneMin.x, zoneMax.x);
        q.z = Mathf.Clamp(q.z, zoneMin.z, zoneMax.z);
        q.y = Mathf.Clamp(q.y, zoneMin.y, Mathf.Min(zoneMax.y, maxHeight));
        transform.position = q;
    }

}