using UnityEngine;

/// <summary>
/// Optional helper: if interceptor tries to cross into intruder zone, push it back.
/// Attach to a trigger collider at the dividing fence or use manager bounds checks instead.
/// </summary>
public class NoFlyZone : MonoBehaviour
{
    public Vector3 allowedMin;
    public Vector3 allowedMax;

    void OnTriggerStay(Collider other)
    {
        var dc = other.GetComponent<DroneController>();
        if (dc == null) return;
        if (!dc.IsInterceptor()) return;

        // clamp interceptor position inside allowed box
        Vector3 p = other.transform.position;
        p.x = Mathf.Clamp(p.x, allowedMin.x, allowedMax.x);
        p.y = Mathf.Clamp(p.y, allowedMin.y, Mathf.Min(allowedMax.y, dc.maxHeight));
        p.z = Mathf.Clamp(p.z, allowedMin.z, allowedMax.z);
        other.transform.position = p;
    }
}
