using UnityEngine;

/// <summary>
/// Threat predictor / evaluator that returns a full ThreatInfo packet.
/// </summary>
public class ThreatPredictor : MonoBehaviour
{
    [Header("Speed Thresholds (m/s)")]
    public float mediumSpeed = 40f;
    public float highSpeed = 90f;
    public float hypersonicSpeed = 180f;

    [Header("Lead settings")]
    public float minLeadSeconds = 0.2f;
    public float maxLeadSeconds = 1.2f;

    [Header("Debug")]
    public bool showDebug = false;

    public Vector3 PredictFuturePosition(Vector3 position, Vector3 velocity, float leadSeconds)
    {
        leadSeconds = Mathf.Clamp(leadSeconds, minLeadSeconds, maxLeadSeconds);
        return position + velocity * leadSeconds;
    }

    public Vector3 GetPredictedPoint(Transform intr, Vector3 velocity)
    {
        float speed = velocity.magnitude;
        float scaledLead = Mathf.Lerp(minLeadSeconds, maxLeadSeconds, Mathf.InverseLerp(0f, hypersonicSpeed, speed));
        return PredictFuturePosition(intr.position, velocity, scaledLead);
    }

    public ThreatInfo EvaluateThreat(Vector3 position, Vector3 velocity, Vector3 corePosition)
    {
        ThreatInfo info = new ThreatInfo();

        float speed = velocity.magnitude;
        info.speed = speed;
        info.threatLevel = speed >= hypersonicSpeed ? 3 : (speed >= highSpeed ? 2 : (speed >= mediumSpeed ? 1 : 0));
        info.distanceToCore = Vector3.Distance(position, corePosition);

        // TTC (safe guard)
        Vector3 dir = (corePosition - position);
        float speedAlong = Vector3.Dot(velocity, dir.normalized);
        info.timeToCore = speedAlong > 0.01f ? dir.magnitude / speedAlong : Mathf.Infinity;

        info.leadTimeUsed = Mathf.Lerp(minLeadSeconds, maxLeadSeconds, Mathf.InverseLerp(0f, hypersonicSpeed, speed));
        info.predictedPosition = PredictFuturePosition(position, velocity, info.leadTimeUsed);

        if (showDebug)
        {
            Debug.DrawLine(position, info.predictedPosition, info.threatLevel == 3 ? Color.red : Color.yellow);
            Debug.Log($"[ThreatPredictor] speed={info.speed:F1} class={info.threatLevel} ttc={info.timeToCore:F1}");
        }

        return info;
    }

    public ThreatInfo EvaluateThreatFor(Transform intr, Vector3 estVel)
    {
        Vector3 core = DroneManager.Instance != null && DroneManager.Instance.ProtectedCore != null ?
                       DroneManager.Instance.ProtectedCore.position : Vector3.zero;
        return EvaluateThreat(intr.position, estVel, core);
    }

    // ------------------------------------------------------------
    // THREAT CLASSIFICATION (RESTORED FOR COMPATIBILITY)
    // ------------------------------------------------------------

    // NEW (your vector-based version remains supported)
    public int ClassifyThreat(Vector3 velocity)
    {
        float speed = velocity.magnitude;
        return ClassifyThreat(speed);
    }

    // REQUIRED BY PHASE 2, 3, and IntruderClassifier
    public int ClassifyThreat(float speed)
    {
        if (speed >= hypersonicSpeed) return 3;
        if (speed >= highSpeed) return 2;
        if (speed >= mediumSpeed) return 1;
        return 0;
    }

    // NEW (vector overload remains supported)
    public bool IsHypersonic(Vector3 velocity)
    {
        return velocity.magnitude >= hypersonicSpeed;
    }

    // REQUIRED BY PredictiveInterceptor, IntruderClassifier
    public bool IsHypersonic(float speed)
    {
        return speed >= hypersonicSpeed;
    }

    public bool IsHypersonic(ThreatInfo info)
    {
        return info.threatLevel >= 3;
    }

}

/// <summary>
/// Unified threat packet used across modules.
/// </summary>
public struct ThreatInfo
{
    public int id;
    public Vector3 position;
    public Vector3 velocity;
    public float confidence;

    // richer fields for predictors
    public float speed;
    public int threatLevel;
    public float distanceToCore;
    public float timeToCore;
    public float leadTimeUsed;
    public Vector3 predictedPosition;
}
