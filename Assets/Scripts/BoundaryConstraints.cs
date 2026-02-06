using UnityEngine;

/// <summary>
/// FIXED VERSION:
/// - Interceptors clamped to Z: -50 to 50 (around fence at Z=0)
/// - Intruders NOT clamped (can cross fence freely)
/// </summary>
public class BoundaryConstraints : MonoBehaviour
{
    private DroneController dc;

    [Header("Interceptor Zone (Top 2 Terrains)")]
    public Vector3 interceptorMin = new Vector3(-50f, 0f, -50f);
    public Vector3 interceptorMax = new Vector3(1050f, 6f, 50f); // ← Z stops at 50, not 1000!

    void Start()
    {
        dc = GetComponent<DroneController>();
        if (dc == null)
        {
            Debug.LogError("[BoundaryConstraints] DroneController missing!");
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (dc == null) return;

        // ✅ ONLY clamp interceptors
        if (dc.IsInterceptor())
        {
            Vector3 p = transform.position;

            // Clamp to interceptor zone
            p.x = Mathf.Clamp(p.x, interceptorMin.x, interceptorMax.x);
            p.y = Mathf.Clamp(p.y, interceptorMin.y, interceptorMax.y);
            p.z = Mathf.Clamp(p.z, interceptorMin.z, interceptorMax.z); // ← Stops at Z=50

            transform.position = p;

            // Debug when near boundary
            if (Mathf.Abs(p.z - interceptorMax.z) < 10f)
            {
                Debug.Log($"[BoundaryConstraints] {name} near fence boundary at Z={p.z:F1}");
            }
        }
        // ❌ Intruders: NO clamping - they cross fence freely
    }

    void OnDrawGizmosSelected()
    {
        if (dc != null && dc.IsInterceptor())
        {
            Gizmos.color = Color.cyan;
            Vector3 center = (interceptorMin + interceptorMax) * 0.5f;
            Vector3 size = interceptorMax - interceptorMin;
            Gizmos.DrawWireCube(center, size);
        }
    }
}