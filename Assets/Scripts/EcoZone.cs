using UnityEngine;

/// <summary>
/// Simple axis-aligned eco-zone with penalty + repulsion.
/// </summary>
[System.Serializable]
public class EcoZone
{
    public string zoneName;
    public Vector3 center;
    public Vector3 size;
    public float ecoPenalty = 1.5f;        
    public float repulsionStrength = 3f;   

    public bool Contains(Vector3 pos)
    {
        Vector3 half = size * 0.5f;
        return (pos.x >= center.x - half.x && pos.x <= center.x + half.x &&
                pos.y >= center.y - half.y && pos.y <= center.y + half.y &&
                pos.z >= center.z - half.z && pos.z <= center.z + half.z);
    }

    public void DrawDebug()
    {
        Vector3 half = size * 0.5f;
        Color c = Color.green;
        c.a = 0.3f;
        Debug.DrawLine(center - half, center + half, c);
    }
}
