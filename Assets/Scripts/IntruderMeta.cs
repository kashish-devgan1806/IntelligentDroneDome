using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IntruderMeta : MonoBehaviour
{
    public Vector3 lastKnownPosition;
    public bool neutralized = false;
    public float breachDistance = 30f;

    private Vector3 corePos;
    private bool isActualIntruder = true;

    private Vector3 lastPosition;
    private Vector3 estimatedVelocity;

    // hypersonic control
    public bool isHypersonic = false;
    public float hypersonicSpeedBoost = 2.5f;

    void Awake()
    {
        if (gameObject.name.Contains("ProtectedZone") || gameObject.name.Contains("Protected"))
        {
            Debug.LogError($"[IntruderMeta] This script should NOT be on {name}! Destroying component.");
            isActualIntruder = false;
            Destroy(this);
            return;
        }
    }

    // public void Initialize(Vector3 corePosition)
    // {
    //     if (!isActualIntruder) return;
    //     corePos = corePosition;
    // }

    // public void SetHypersonic(bool enable)
    // {
    //     isHypersonic = enable;
    //     Rigidbody rb = GetComponent<Rigidbody>();
    //     if (rb != null && enable)
    //     {
    //         rb.velocity *= hypersonicSpeedBoost;
    //     }
    // }

    public void OnNeutralized()
    {
        if (!isActualIntruder) return;
        if (neutralized) return;

        neutralized = true;
        lastKnownPosition = transform.position;

        Debug.Log($"[LOG] neutralized at {FormatPos(lastKnownPosition)}");

        // telemetry + classifier (if present)
        MissionTelemetry.Instance?.RegisterKill();
        MissionTelemetry.Instance?.RegisterNeutralizationPosition(transform.position);
        MockClassifier.Instance?.RecordNeutralization(this.gameObject, true);

        // Inform DroneManager
        if (DroneManager.Instance != null)
            DroneManager.Instance.NotifyIntruderNeutralized(this);

        Destroy(gameObject, 0.1f);
    }

    public void OnDestroyed()
    {
        if (!isActualIntruder) return;
        lastKnownPosition = transform.position;

        Debug.Log($"[LOG] destroyed at {FormatPos(lastKnownPosition)}");

        MissionTelemetry.Instance?.RegisterKill();
        MissionTelemetry.Instance?.RegisterNeutralizationPosition(transform.position);
        MockClassifier.Instance?.RecordNeutralization(this.gameObject, false);

        if (DroneManager.Instance != null)
            DroneManager.Instance.NotifyIntruderDestroyed(this);

        Destroy(gameObject, 0.1f);
    }

    void Update()
    {
        if (!isActualIntruder || neutralized) return;

        float distToCore = Vector3.Distance(transform.position, corePos);
        if (distToCore <= breachDistance)
        {
            Debug.LogError($"❌ CORE BREACHED by {name} at {distToCore:F1}m — Mission Failed!");
            MissionTelemetry.Instance?.RecordBreachAttempt();
            if (DroneManager.Instance != null) DroneManager.Instance.EndSimulationFailure();
            Destroy(gameObject);
            return;
        }

        estimatedVelocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
    }

    public void Initialize(Vector3 corePosition)
    {
        if (!isActualIntruder) return;
        corePos = corePosition;
        lastPosition = transform.position; // IMPORTANT: seed the lastPosition so estimatedVelocity starts correctly
        estimatedVelocity = Vector3.zero;
    }

    // ensure this FormatPos helper exists (near bottom of class)
    private string FormatPos(Vector3 p) => $"({p.x:F1}, {p.y:F1}, {p.z:F1})";

    // SetHypersonic: safe multiplier; also set initial forward velocity if needed
    public void SetHypersonic(bool enable)
    {
        isHypersonic = enable;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && enable)
        {
            // give the intruder an initial speed boost in its current forward direction (if any)
            Vector3 forward = (corePos - transform.position).normalized;
            float boost = hypersonicSpeedBoost;
            rb.velocity = forward * Mathf.Max(rb.velocity.magnitude, 1f) * boost;
        }
    }

    public Vector3 GetEstimatedVelocity() => estimatedVelocity;

}
