using UnityEngine;

[CreateAssetMenu(menuName = "Threat/ThreatProfile", fileName = "ThreatProfile")]
public class ThreatProfile : ScriptableObject
{
    [Header("Identification")]
    public string profileID = "H1-Direct";

    [Header("Speed (m/s)")]
    public float baseSpeed = 180f;
    public float maxSpeed = 320f;

    [Header("Maneuvering")]
    public float agility = 2.0f;
    public float curveStrength = 12f;
    public float maxJitter = 1.2f;

    [Header("Behavior / Family")]
    [Tooltip("0=Direct,1=Arc,2=ZigZag,3=SCurve,4=TerrainHug")]
    public int family = 0;

    [Header("Evasion & Variability")]
    [Range(0f, 1f)]
    public float evasiveProbability = 0.08f;
    [Range(0f, 1f)]
    public float aggressiveness = 0.2f;

    [Header("Altitude band (absolute world Y min,max)")]
    public Vector2 altitudeBand = new Vector2(40f, 140f);

    [Header("Predictive tuning")]
    public float replanInterval = 3.0f;

    [Header("Misc")]
    public int randomSeed = 0;
}
