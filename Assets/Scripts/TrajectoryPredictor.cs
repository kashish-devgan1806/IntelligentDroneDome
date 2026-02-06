using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Predicts hypersonic intruder trajectory 3-5 seconds ahead.
/// Uses physics-based motion model with drag, curved paths, and evasive maneuvers.
/// </summary>
public class TrajectoryPredictor : MonoBehaviour
{
    [Header("Prediction Settings")]
    public float maxPredictionTime = 5f;
    public float predictionStep = 0.2f; // sample every 0.2s
    public bool accountForDrag = true;
    public bool accountForCurvature = true;

    [Header("Physics Model")]
    public float dragCoefficient = 0.02f;
    public float lateralCurveAmplitude = 8f; // for evasive sine-wave motion
    public float lateralCurveFrequency = 1.5f;

    [Header("Debug")]
    public bool drawPredictions = false;

    /// <summary>
    /// Predict future trajectory waypoints for an intruder.
    /// Returns list of predicted positions at time intervals.
    /// </summary>
    public List<Vector3> PredictTrajectory(Vector3 currentPos, Vector3 currentVel, float targetTime)
    {
        List<Vector3> waypoints = new List<Vector3>();
        
        Vector3 pos = currentPos;
        Vector3 vel = currentVel;
        float t = 0f;

        // Add current position
        waypoints.Add(pos);

        int steps = Mathf.CeilToInt(targetTime / predictionStep);
        
        for (int i = 0; i < steps; i++)
        {
            t += predictionStep;
            
            // Apply drag (velocity decay)
            if (accountForDrag)
            {
                float speed = vel.magnitude;
                Vector3 dragForce = -vel.normalized * dragCoefficient * speed * speed;
                vel += dragForce * predictionStep;
            }

            // Apply lateral curvature (evasive maneuver simulation)
            if (accountForCurvature)
            {
                Vector3 forward = vel.normalized;
                Vector3 lateral = Vector3.Cross(Vector3.up, forward);
                float curve = Mathf.Sin(t * lateralCurveFrequency + currentPos.x) * lateralCurveAmplitude;
                pos += lateral * curve * predictionStep;
            }

            // Integrate position
            pos += vel * predictionStep;

            // Clamp height to reasonable bounds
            pos.y = Mathf.Clamp(pos.y, 0.5f, 10f);

            waypoints.Add(pos);
        }

        if (drawPredictions)
        {
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Debug.DrawLine(waypoints[i], waypoints[i + 1], Color.magenta, 0.5f);
            }
        }

        return waypoints;
    }

    /// <summary>
    /// Predict single position at specific time ahead (fast version).
    /// </summary>
    public Vector3 PredictPositionAt(Vector3 currentPos, Vector3 currentVel, float timeAhead)
    {
        Vector3 pos = currentPos;
        Vector3 vel = currentVel;
        float t = 0f;

        int steps = Mathf.CeilToInt(timeAhead / predictionStep);

        for (int i = 0; i < steps; i++)
        {
            t += predictionStep;

            if (accountForDrag)
            {
                float speed = vel.magnitude;
                vel -= vel.normalized * dragCoefficient * speed * speed * predictionStep;
            }

            if (accountForCurvature)
            {
                Vector3 forward = vel.normalized;
                Vector3 lateral = Vector3.Cross(Vector3.up, forward);
                float curve = Mathf.Sin(t * lateralCurveFrequency + currentPos.x) * lateralCurveAmplitude;
                pos += lateral * curve * predictionStep;
            }

            pos += vel * predictionStep;
            pos.y = Mathf.Clamp(pos.y, 0.5f, 10f);
        }

        return pos;
    }

    /// <summary>
    /// Get predicted velocity at time ahead (useful for relative velocity calculations).
    /// </summary>
    public Vector3 PredictVelocityAt(Vector3 currentVel, float timeAhead)
    {
        Vector3 vel = currentVel;
        float t = 0f;

        int steps = Mathf.CeilToInt(timeAhead / predictionStep);

        for (int i = 0; i < steps; i++)
        {
            t += predictionStep;

            if (accountForDrag)
            {
                float speed = vel.magnitude;
                vel -= vel.normalized * dragCoefficient * speed * speed * predictionStep;
            }
        }

        return vel;
    }
}