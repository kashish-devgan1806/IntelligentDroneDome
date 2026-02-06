using UnityEngine;

/// <summary>
/// Solves optimal interception trajectories for interceptor drones.
/// Computes intercept point, time-to-intercept, and steering commands.
/// </summary>
public class InterceptionSolver : MonoBehaviour
{
    [Header("Solver Settings")]
    public int maxIterations = 10;
    public float convergenceThreshold = 1f; // meters
    public float minInterceptTime = 0.5f;
    public float maxInterceptTime = 8f;

    [Header("References")]
    public TrajectoryPredictor trajectoryPredictor;

    void Awake()
    {
        if (trajectoryPredictor == null)
            trajectoryPredictor = FindObjectOfType<TrajectoryPredictor>();
    }

    /// <summary>
    /// Solve interception problem: find where and when interceptor meets intruder.
    /// Returns intercept solution with point, time, and validity flag.
    /// </summary>
    public InterceptSolution SolveIntercept(
        Vector3 interceptorPos, 
        float interceptorSpeed,
        Vector3 intruderPos,
        Vector3 intruderVel)
    {
        InterceptSolution solution = new InterceptSolution();

        // Initial guess: time based on direct distance and speed ratio
        float directDist = Vector3.Distance(interceptorPos, intruderPos);
        float tGuess = directDist / Mathf.Max(interceptorSpeed, 1f);
        tGuess = Mathf.Clamp(tGuess, minInterceptTime, maxInterceptTime);

        Vector3 interceptPoint = intruderPos;
        float interceptTime = tGuess;
        bool converged = false;

        // Iterative refinement
        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Predict intruder position at current time guess
            if (trajectoryPredictor != null)
            {
                interceptPoint = trajectoryPredictor.PredictPositionAt(intruderPos, intruderVel, interceptTime);
            }
            else
            {
                // Fallback: linear prediction
                interceptPoint = intruderPos + intruderVel * interceptTime;
            }

            // Compute required time for interceptor to reach that point
            float distToIntercept = Vector3.Distance(interceptorPos, interceptPoint);
            float requiredTime = distToIntercept / Mathf.Max(interceptorSpeed, 1f);

            // Check convergence
            float error = Mathf.Abs(requiredTime - interceptTime);
            if (error < convergenceThreshold / Mathf.Max(interceptorSpeed, 1f))
            {
                converged = true;
                break;
            }

            // Update time guess (weighted average for stability)
            interceptTime = Mathf.Lerp(interceptTime, requiredTime, 0.6f);
            interceptTime = Mathf.Clamp(interceptTime, minInterceptTime, maxInterceptTime);
        }

        // Build solution
        solution.interceptPoint = interceptPoint;
        solution.interceptTime = interceptTime;
        solution.isValid = converged && interceptTime < maxInterceptTime;
        solution.relativeVelocity = intruderVel; // could compute actual relative vel if needed

        // Compute required heading
        Vector3 toIntercept = interceptPoint - interceptorPos;
        solution.requiredHeading = toIntercept.normalized;
        solution.distanceToIntercept = toIntercept.magnitude;

        return solution;
    }

    /// <summary>
    /// Check if interception is feasible (can interceptor reach in time?).
    /// </summary>
    public bool IsInterceptionFeasible(
        Vector3 interceptorPos,
        float interceptorSpeed,
        Vector3 intruderPos,
        Vector3 intruderVel,
        Vector3 corePos,
        float criticalRadius = 50f)
    {
        // Estimate time for intruder to reach core
        Vector3 toCore = corePos - intruderPos;
        float intruderSpeed = intruderVel.magnitude;
        float timeToCore = toCore.magnitude / Mathf.Max(intruderSpeed, 1f);

        // Solve intercept
        InterceptSolution solution = SolveIntercept(interceptorPos, interceptorSpeed, intruderPos, intruderVel);

        // Feasible if intercept time < time to core, and solution is valid
        return solution.isValid && solution.interceptTime < timeToCore - 0.5f;
    }

    /// <summary>
    /// Compute optimal steering command for interceptor to reach intercept point.
    /// </summary>
    public Vector3 ComputeSteeringCommand(
        Vector3 interceptorPos,
        Vector3 currentVelocity,
        InterceptSolution solution,
        float maxAcceleration = 20f)
    {
        if (!solution.isValid)
            return Vector3.zero;

        // Desired velocity direction
        Vector3 desiredVel = solution.requiredHeading * currentVelocity.magnitude;

        // Steering force (proportional navigation)
        Vector3 steering = (desiredVel - currentVelocity) * maxAcceleration;

        return Vector3.ClampMagnitude(steering, maxAcceleration);
    }
}

/// <summary>
/// Result of interception calculation.
/// </summary>
public struct InterceptSolution
{
    public Vector3 interceptPoint;
    public float interceptTime;
    public bool isValid;
    public Vector3 requiredHeading;
    public float distanceToIntercept;
    public Vector3 relativeVelocity;
}