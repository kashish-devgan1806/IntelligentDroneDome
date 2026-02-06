using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// PHASE 4 EXTENSION: Adds Kalman filtering, trajectory prediction,
/// interception solving, and eco-corridor planning to existing SwarmCoordinator.
/// 
/// USAGE: Attach ALONGSIDE SwarmCoordinator (not a replacement).
/// This component enhances decisions with predictive intelligence.
/// </summary>
[DefaultExecutionOrder(110)] // Run after SwarmCoordinator
public class PredictiveSwarmCoordinator : MonoBehaviour
{
    [Header("References")]
    public SwarmCoordinator baseCoordinator;
    public SensorFusion sensorFusion;
    public DroneManager droneManager;

    [Header("Phase 4 Components")]
    public TrajectoryPredictor trajectoryPredictor;
    public InterceptionSolver interceptionSolver;
    public EcoCorridorPlanner corridorPlanner;

    [Header("Kalman Tracking")]
    public bool useKalmanFiltering = true;
    private Dictionary<int, KalmanTracker> kalmanTrackers = new Dictionary<int, KalmanTracker>();

    [Header("Predictive Settings")]
    public float predictionHorizon = 4f; // seconds ahead
    public float minInterceptConfidence = 0.3f;

    [Header("Eco-Aware Routing")]
    public bool useEcoCorridors = true;
    private Dictionary<int, List<Vector3>> droneCorridors = new Dictionary<int, List<Vector3>>();
    private Dictionary<int, int> corridorWaypointIndices = new Dictionary<int, int>();

    [Header("Debug")]
    public bool showPredictions = false;

    void Awake()
    {
        if (baseCoordinator == null)
            baseCoordinator = GetComponent<SwarmCoordinator>();

        if (sensorFusion == null)
            sensorFusion = FindObjectOfType<SensorFusion>();

        if (droneManager == null)
            droneManager = DroneManager.Instance;

        // Auto-create Phase 4 components if missing
        if (trajectoryPredictor == null)
        {
            trajectoryPredictor = gameObject.AddComponent<TrajectoryPredictor>();
        }

        if (interceptionSolver == null)
        {
            interceptionSolver = gameObject.AddComponent<InterceptionSolver>();
        }

        if (corridorPlanner == null)
        {
            corridorPlanner = gameObject.AddComponent<EcoCorridorPlanner>();
        }
    }

    void Update()
    {
        if (baseCoordinator == null || sensorFusion == null)
            return;

        // Update Kalman trackers with fused detections
        UpdateKalmanTrackers();

        // Enhance interceptor decisions with predictive intelligence
        EnhanceInterceptorCommands();
    }

    /// <summary>
    /// Update Kalman filters for all detected tracks.
    /// </summary>
    void UpdateKalmanTrackers()
    {
        if (!useKalmanFiltering || sensorFusion == null)
            return;

        var tracks = sensorFusion.GetFusedTracks();
        if (tracks == null)
            return;

        // Update existing trackers, create new ones
        foreach (var track in tracks)
        {
            if (!kalmanTrackers.ContainsKey(track.id))
            {
                // Initialize new tracker
                kalmanTrackers[track.id] = new KalmanTracker(track.worldPosition, track.velocity);
            }
            else
            {
                // Predict step
                kalmanTrackers[track.id].Predict(Time.deltaTime);
                
                // Update step with measurement
                kalmanTrackers[track.id].Update(track.worldPosition);
                kalmanTrackers[track.id].UpdateVelocity(track.velocity);
            }
        }

        // Remove old trackers (not seen in last few frames)
        var currentIds = tracks.Select(t => t.id).ToHashSet();
        var toRemove = kalmanTrackers.Keys.Where(id => !currentIds.Contains(id)).ToList();
        foreach (var id in toRemove)
        {
            kalmanTrackers.Remove(id);
        }
    }

    /// <summary>
    /// Enhance interceptor steering with predictive trajectories and eco-corridors.
    /// </summary>
    void EnhanceInterceptorCommands()
    {
        if (droneManager == null || interceptionSolver == null)
            return;

        var interceptors = droneManager.GetInterceptors();
        if (interceptors == null)
            return;

        foreach (var interceptor in interceptors)
        {
            if (interceptor == null || !interceptor.IsAssigned())
                continue;

            Transform target = interceptor.GetAssignedTarget();
            if (target == null)
                continue;

            // Get Kalman-filtered state if available
            IntruderMeta intruderMeta = target.GetComponent<IntruderMeta>();
            if (intruderMeta == null)
                continue;

            int trackId = intruderMeta.GetInstanceID();
            
            Vector3 intruderPos = target.position;
            Vector3 intruderVel = Vector3.zero;

            if (useKalmanFiltering && kalmanTrackers.ContainsKey(trackId))
            {
                intruderPos = kalmanTrackers[trackId].GetPosition();
                intruderVel = kalmanTrackers[trackId].GetVelocity();
            }
            else if (sensorFusion != null)
            {
                intruderVel = sensorFusion.GetVelocity(intruderMeta);
            }

            // Solve optimal intercept
            InterceptSolution solution = interceptionSolver.SolveIntercept(
                interceptor.transform.position,
                interceptor.speed,
                intruderPos,
                intruderVel
            );

            if (!solution.isValid)
                continue;

            // Plan eco-corridor if enabled
            Vector3 targetPoint = solution.interceptPoint;
            
            if (useEcoCorridors && corridorPlanner != null)
            {
                int droneId = interceptor.GetInstanceID();

                // Generate corridor if not exists or target changed significantly
                if (!droneCorridors.ContainsKey(droneId) || 
                    Vector3.Distance(droneCorridors[droneId][droneCorridors[droneId].Count - 1], targetPoint) > 50f)
                {
                    List<Vector3> corridor = corridorPlanner.PlanCorridor(
                        interceptor.transform.position,
                        targetPoint
                    );

                    if (corridor != null && corridor.Count > 0)
                    {
                        droneCorridors[droneId] = corridor;
                        corridorWaypointIndices[droneId] = 0;
                    }
                }

                // Get current waypoint from corridor
                if (droneCorridors.ContainsKey(droneId))
                {
                    int wpIndex = corridorWaypointIndices.GetValueOrDefault(droneId, 0);
                    targetPoint = corridorPlanner.GetCurrentWaypoint(
                        droneCorridors[droneId],
                        interceptor.transform.position,
                        ref wpIndex
                    );
                    corridorWaypointIndices[droneId] = wpIndex;
                }
            }

            // Override interceptor steering with predictive target
            interceptor.OverrideSteering(targetPoint);

            // Debug visualization
            if (showPredictions)
            {
                Debug.DrawLine(interceptor.transform.position, targetPoint, Color.cyan, 0.1f);
                Debug.DrawLine(intruderPos, solution.interceptPoint, Color.magenta, 0.1f);
            }
        }
    }

    /// <summary>
    /// Get predicted intruder position for UI/telemetry.
    /// </summary>
    public Vector3 GetPredictedPosition(IntruderMeta intruder, float timeAhead)
    {
        if (intruder == null || trajectoryPredictor == null)
            return intruder.transform.position;

        int id = intruder.GetInstanceID();
        
        Vector3 pos = intruder.transform.position;
        Vector3 vel = Vector3.zero;

        if (useKalmanFiltering && kalmanTrackers.ContainsKey(id))
        {
            return kalmanTrackers[id].PredictPosition(timeAhead);
        }
        else if (sensorFusion != null)
        {
            vel = sensorFusion.GetVelocity(intruder);
            return trajectoryPredictor.PredictPositionAt(pos, vel, timeAhead);
        }

        return pos + vel * timeAhead;
    }

    /// <summary>
    /// Check if interception is feasible for given drone-intruder pair.
    /// </summary>
    public bool CanIntercept(DroneController interceptor, IntruderMeta intruder)
    {
        if (interceptor == null || intruder == null || interceptionSolver == null)
            return false;

        Vector3 intruderVel = sensorFusion != null 
            ? sensorFusion.GetVelocity(intruder) 
            : Vector3.zero;

        Vector3 corePos = droneManager != null && droneManager.ProtectedCore != null
            ? droneManager.ProtectedCore.position
            : Vector3.zero;

        return interceptionSolver.IsInterceptionFeasible(
            interceptor.transform.position,
            interceptor.speed,
            intruder.transform.position,
            intruderVel,
            corePos
        );
    }

    /// <summary>
    /// Get tracking uncertainty for intruder (for confidence display).
    /// </summary>
    public float GetTrackingUncertainty(IntruderMeta intruder)
    {
        if (!useKalmanFiltering || intruder == null)
            return 0f;

        int id = intruder.GetInstanceID();
        if (kalmanTrackers.ContainsKey(id))
        {
            return kalmanTrackers[id].GetPositionUncertainty();
        }

        return 0f;
    }

    void OnDrawGizmos()
    {
        if (!showPredictions || !Application.isPlaying)
            return;

        // Draw eco-corridors
        if (useEcoCorridors && droneCorridors != null)
        {
            Gizmos.color = Color.green;
            foreach (var corridor in droneCorridors.Values)
            {
                if (corridor == null || corridor.Count < 2)
                    continue;

                for (int i = 0; i < corridor.Count - 1; i++)
                {
                    Gizmos.DrawLine(corridor[i], corridor[i + 1]);
                }
            }
        }
    }
}