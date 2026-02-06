using UnityEngine;

/// <summary>
/// Lightweight classifier that labels fused tracks as Low/Med/High/Hypersonic
/// based on velocity magnitude and optionally calls ThreatPredictor for final scoring.
/// </summary>
public class IntruderClassifier : MonoBehaviour
{
    public ThreatPredictor predictor;

    void Start()
    {
        if (predictor == null) predictor = FindObjectOfType<ThreatPredictor>();
    }

    public IntruderClassifyResult Classify(FusedTrack t)
    {
        float speed = t.velocity.magnitude;
        int level = predictor != null ? predictor.ClassifyThreat(speed) : ClassifyBySpeed(speed);
        return new IntruderClassifyResult { fused = t, threatLevel = level, speed = speed };
    }

    int ClassifyBySpeed(float s)
    {
        if (s > 180f) return 3;
        if (s > 90f) return 2;
        if (s > 40f) return 1;
        return 0;
    }
}

public struct IntruderClassifyResult
{
    public FusedTrack fused;
    public int threatLevel; // 0..3
    public float speed;
}
