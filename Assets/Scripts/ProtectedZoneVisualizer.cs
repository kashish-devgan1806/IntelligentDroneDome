using UnityEngine;

/// <summary>
/// Attach to your ProtectedZone red capsule to keep it visible during play
/// </summary>
public class ProtectedZoneKeeper : MonoBehaviour
{
    public Color zoneColor = new Color(1f, 0f, 0f, 0.5f); // Red, semi-transparent
    
    private MeshRenderer meshRenderer;
    private Material originalMaterial;

    void Awake()
    {
        // Get or create mesh renderer
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Save original material
        if (meshRenderer.sharedMaterial != null)
        {
            originalMaterial = new Material(meshRenderer.sharedMaterial);
            originalMaterial.color = zoneColor;
        }
        else
        {
            // Create new material
            originalMaterial = new Material(Shader.Find("Standard"));
            originalMaterial.color = zoneColor;
        }

        meshRenderer.material = originalMaterial;
        meshRenderer.enabled = true;

        // Ensure mesh filter exists
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("[ProtectedZoneKeeper] No mesh found, creating primitive");
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = capsule.GetComponent<MeshFilter>().sharedMesh;
            }
            Destroy(capsule);
        }

        // Remove collider (zone shouldn't block movement)
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    void Update()
    {
        // Force renderer to stay enabled
        if (meshRenderer != null && !meshRenderer.enabled)
        {
            meshRenderer.enabled = true;
        }

        // Restore material if it gets lost
        if (meshRenderer != null && meshRenderer.material == null)
        {
            meshRenderer.material = originalMaterial;
        }
    }

    void OnDrawGizmos()
    {
        // Always visible in Scene view
        Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x * 0.5f);
    }
}