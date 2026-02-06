using UnityEngine;

/// <summary>
/// Shared Phase-3 fused detection track.
/// </summary>
public struct FusedTrack
{
    public int id;
    public Vector3 worldPosition;
    public Vector3 velocity;
    public float confidence;

    public FusedTrack(int id, Vector3 pos, Vector3 vel, float conf)
    {
        this.id = id;
        this.worldPosition = pos;
        this.velocity = vel;
        this.confidence = conf;
    }
}
