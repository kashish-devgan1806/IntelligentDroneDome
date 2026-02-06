using UnityEngine;

[CreateAssetMenu(menuName = "Threat/ThreatScenario", fileName = "ThreatScenario")]
public class ThreatScenario : ScriptableObject
{
    public int intruderCount = 12;      // batch size
    public float spawnInterval = 0.4f;  // used if batching over time
    public float windStrength = 0f;

    [Range(0,1)] public float p_H1 = 0.4f;
    [Range(0,1)] public float p_H2 = 0.2f;
    [Range(0,1)] public float p_H3 = 0.2f;
    [Range(0,1)] public float p_H4 = 0.1f;
    [Range(0,1)] public float p_H5 = 0.1f;
}
