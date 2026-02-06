using UnityEngine;

/// <summary>
/// Enforces red zone boundaries for INTERCEPTORS ONLY
/// - Clamps position inside zone
/// - Bounces back when trying to escape
/// - Intruders can pass through freely
/// </summary>
public class RedZoneBoundaryEnforcer : MonoBehaviour
{
    private DroneController dc;
    private Vector3 lastValidPosition;
    private Vector3 bounceVelocity;

    [Header("Bounce Settings")]
    public float bounceForce = 20f;
    public float dampening = 0.95f;

    void Start()
    {
        dc = GetComponent<DroneController>();
        if (dc == null)
        {
            Debug.LogError("[RedZoneBoundaryEnforcer] DroneController missing!");
            enabled = false;
            return;
        }

        lastValidPosition = transform.position;
    }

    void LateUpdate()
    {
        if (dc == null || DroneManager.Instance == null) return;

        // ✅ ONLY enforce for interceptors
        if (!dc.IsInterceptor()) return;

        Vector3 redMin = DroneManager.Instance.redZoneMin;
        Vector3 redMax = DroneManager.Instance.redZoneMax;

        Vector3 pos = transform.position;
        bool outOfBounds = false;
        Vector3 bounceDir = Vector3.zero;

        // Check X boundaries
        if (pos.x < redMin.x)
        {
            pos.x = redMin.x;
            bounceDir.x = bounceForce;
            outOfBounds = true;
        }
        else if (pos.x > redMax.x)
        {
            pos.x = redMax.x;
            bounceDir.x = -bounceForce;
            outOfBounds = true;
        }

        // Check Z boundaries
        if (pos.z < redMin.z)
        {
            pos.z = redMin.z;
            bounceDir.z = bounceForce;
            outOfBounds = true;
        }
        else if (pos.z > redMax.z)
        {
            pos.z = redMax.z;
            bounceDir.z = -bounceForce;
            outOfBounds = true;
        }

        // Check Y boundaries
        if (pos.y < redMin.y)
        {
            pos.y = redMin.y;
        }
        else if (pos.y > redMax.y)
        {
            pos.y = redMax.y;
        }

        // Apply bounce effect
        if (outOfBounds)
        {
            transform.position = pos;
            bounceVelocity = bounceDir;
        }

        // Apply bounce velocity
        if (bounceVelocity.sqrMagnitude > 0.1f)
        {
            transform.position += bounceVelocity * Time.deltaTime;
            bounceVelocity *= dampening;
        }

        // Update last valid position
        if (!outOfBounds)
        {
            lastValidPosition = transform.position;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (DroneManager.Instance == null) return;
        if (dc == null || !dc.IsInterceptor()) return;

        Gizmos.color = Color.yellow;
        Vector3 min = DroneManager.Instance.redZoneMin;
        Vector3 max = DroneManager.Instance.redZoneMax;
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        Gizmos.DrawWireCube(center, size);
    }
}