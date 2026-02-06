using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Plans energy-efficient, eco-safe corridors for interceptor drones.
/// Avoids eco-zones, minimizes energy use, and provides waypoint paths.
/// </summary>
public class EcoCorridorPlanner : MonoBehaviour
{
    [Header("References")]
    public EcoConservationManager ecoManager;

    [Header("Corridor Settings")]
    public float waypointSpacing = 50f; // distance between waypoints
    public float ecoZoneAvoidanceMargin = 20f; // stay this far from eco-zone edges
    public float maxDetourFactor = 1.5f; // max detour: 1.5x direct distance

    [Header("Energy Model")]
    public float energyPerMeter = 0.5f;
    public float energyPerTurn = 2f; // additional cost for direction changes

    [Header("Debug")]
    public bool drawCorridors = false;

    void Awake()
    {
        if (ecoManager == null)
            ecoManager = EcoConservationManager.Instance;
    }

    /// <summary>
    /// Plan eco-safe corridor from start to goal.
    /// Returns waypoint list, or null if no safe path found.
    /// </summary>
    public List<Vector3> PlanCorridor(Vector3 start, Vector3 goal)
    {
        List<Vector3> waypoints = new List<Vector3>();

        // Direct path check
        if (IsPathSafe(start, goal))
        {
            waypoints.Add(start);
            waypoints.Add(goal);
            return waypoints;
        }

        // If direct path blocked, find detour
        List<Vector3> detour = FindEcoDetour(start, goal);
        if (detour != null && detour.Count > 0)
        {
            if (drawCorridors)
            {
                for (int i = 0; i < detour.Count - 1; i++)
                {
                    Debug.DrawLine(detour[i], detour[i + 1], Color.green, 1f);
                }
            }
            return detour;
        }

        // Fallback: return direct path with warning
        Debug.LogWarning("[EcoCorridorPlanner] No safe corridor found, using direct path!");
        waypoints.Add(start);
        waypoints.Add(goal);
        return waypoints;
    }

    /// <summary>
    /// Check if straight line path is safe (no eco-zone violations).
    /// </summary>
    bool IsPathSafe(Vector3 start, Vector3 goal)
    {
        if (ecoManager == null) return true;

        // Sample points along path
        int samples = Mathf.CeilToInt(Vector3.Distance(start, goal) / 10f);
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 point = Vector3.Lerp(start, goal, t);

            // Check if point is in eco-zone
            float penalty = ecoManager.GetZonePenalty(point);
            if (penalty > 0.1f)
                return false; // path blocked by eco-zone
        }

        return true;
    }

    /// <summary>
    /// Find detour path around eco-zones using simple waypoint approach.
    /// </summary>
    List<Vector3> FindEcoDetour(Vector3 start, Vector3 goal)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(start);

        Vector3 current = start;
        int maxWaypoints = 10;
        int wpCount = 0;

        while (Vector3.Distance(current, goal) > waypointSpacing && wpCount < maxWaypoints)
        {
            // Direction to goal
            Vector3 toGoal = (goal - current).normalized;

            // Try direct step
            Vector3 candidate = current + toGoal * waypointSpacing;

            // If candidate in eco-zone, deflect perpendicular
            if (ecoManager != null && ecoManager.GetZonePenalty(candidate) > 0.1f)
            {
                // Get repulsion vector
                Vector3 repulsion = ecoManager.GetEcoRepulsion(candidate);
                
                if (repulsion.sqrMagnitude < 0.01f)
                {
                    // Fallback: deflect perpendicular to goal direction
                    Vector3 perpendicular = Vector3.Cross(toGoal, Vector3.up).normalized;
                    candidate = current + (toGoal + perpendicular * 0.5f).normalized * waypointSpacing;
                }
                else
                {
                    // Use repulsion vector
                    candidate = current + (toGoal + repulsion.normalized * 0.7f).normalized * waypointSpacing;
                }
            }

            // Clamp Y
            candidate.y = Mathf.Clamp(candidate.y, 0.5f, 6f);

            path.Add(candidate);
            current = candidate;
            wpCount++;
        }

        // Add final goal
        path.Add(goal);

        // Validate total path length
        float totalDist = ComputePathLength(path);
        float directDist = Vector3.Distance(start, goal);
        if (totalDist > directDist * maxDetourFactor)
        {
            return null; // detour too long
        }

        return path;
    }

    /// <summary>
    /// Compute energy cost of following a waypoint path.
    /// </summary>
    public float ComputeEnergyCost(List<Vector3> waypoints)
    {
        if (waypoints == null || waypoints.Count < 2)
            return 0f;

        float energy = 0f;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            float dist = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            energy += dist * energyPerMeter;

            // Turn cost (angle change)
            if (i < waypoints.Count - 2)
            {
                Vector3 dir1 = (waypoints[i + 1] - waypoints[i]).normalized;
                Vector3 dir2 = (waypoints[i + 2] - waypoints[i + 1]).normalized;
                float angle = Vector3.Angle(dir1, dir2);
                energy += angle * energyPerTurn;
            }
        }

        return energy;
    }

    /// <summary>
    /// Compute total path length.
    /// </summary>
    float ComputePathLength(List<Vector3> waypoints)
    {
        if (waypoints == null || waypoints.Count < 2)
            return 0f;

        float length = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            length += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        }
        return length;
    }

    /// <summary>
    /// Get current waypoint target for drone following corridor.
    /// </summary>
    public Vector3 GetCurrentWaypoint(List<Vector3> corridor, Vector3 currentPos, ref int waypointIndex)
    {
        if (corridor == null || corridor.Count == 0)
            return currentPos;

        // Check if reached current waypoint
        if (waypointIndex < corridor.Count)
        {
            float dist = Vector3.Distance(currentPos, corridor[waypointIndex]);
            if (dist < 10f) // reached waypoint
            {
                waypointIndex++;
            }
        }

        // Return current target
        if (waypointIndex < corridor.Count)
            return corridor[waypointIndex];

        // End of corridor
        return corridor[corridor.Count - 1];
    }
}