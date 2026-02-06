using UnityEngine;

/// <summary>
/// CRASH-PROOF interceptor setup - runs once in Awake
/// </summary>
public class InterceptorCollisionSetup : MonoBehaviour
{
    void Awake()
    {
        // Ensure collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 2f;
        }
        col.isTrigger = false;

        // Ensure rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        // Ensure tag
        try
        {
            if (!gameObject.CompareTag("Interceptor"))
            {
                gameObject.tag = "Interceptor";
            }
        }
        catch
        {
            Debug.LogWarning($"[InterceptorCollisionSetup] Tag 'Interceptor' does not exist. Create it in Tags & Layers.");
        }
    }
}