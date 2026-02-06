using UnityEngine;

/// <summary>
/// Predictive interceptor — asks ThreatPredictor for a lead point and assigns target or moves toward lead point.
/// </summary>
[RequireComponent(typeof(DroneController))]
public class PredictiveInterceptor : MonoBehaviour
{
    public ThreatPredictor predictor;
    public float hypersonicBoostMultiplier = 1.7f;
    public float normalBoostMultiplier = 1.0f;

    DroneController controller;
    float originalSpeed;

    void Awake()
    {
        controller = GetComponent<DroneController>();
        if (predictor == null) predictor = FindObjectOfType<ThreatPredictor>();
        originalSpeed = controller != null ? controller.speed : 0f;
    }

    public void EngageIntruder(Transform intruderTransform, Vector3 intruderEstimatedVelocity)
    {
        if (controller == null || intruderTransform == null) return;
        if (predictor == null) predictor = FindObjectOfType<ThreatPredictor>();

        ThreatInfo info = predictor != null ? predictor.EvaluateThreatFor(intruderTransform, intruderEstimatedVelocity) : new ThreatInfo();

        Vector3 interceptPoint = predictor != null ? predictor.GetPredictedPoint(intruderTransform, intruderEstimatedVelocity) : intruderTransform.position;

        bool isHypersonic = predictor != null && predictor.IsHypersonic(info.speed);
        controller.speed = originalSpeed * (isHypersonic ? hypersonicBoostMultiplier : normalBoostMultiplier);

        bool assigned = controller.AssignTarget(intruderTransform, OnFreed);
        if (!assigned)
        {
            // fallback: micro-move to interceptPoint (non-teleport)
            Vector3 desired = interceptPoint;
            desired.y = Mathf.Clamp(desired.y, 0f, controller.maxHeight);
            transform.position = Vector3.MoveTowards(transform.position, desired, controller.speed * Time.deltaTime);

            Vector3 dir = (desired - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * controller.turnSpeed);
            }
        }
    }

    void OnFreed(DroneController dc)
    {
        if (controller != null) controller.speed = originalSpeed;
    }

    void OnDisable()
    {
        if (controller != null) controller.speed = originalSpeed;
    }
}
