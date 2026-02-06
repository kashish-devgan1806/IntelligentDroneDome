using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Global environmental brain.
/// Computes penalties, energy, carbon footprint, pushes drones away from eco zones.
/// </summary>
public class EcoConservationManager : MonoBehaviour
{
    public static EcoConservationManager Instance;

    [Header("Environmental Zones")]
    public List<EcoZone> ecoZones = new List<EcoZone>();

    [Header("Energy Model")]
    public float baseEnergyCost = 1.0f;
    public float turnCostMultiplier = 0.3f;
    public float speedCostMultiplier = 0.8f;
    public float carbonCoefficient = 0.004f; // grams per joule

    [Header("Debug")]
    public bool drawZones = true;

    float totalEnergyUsed = 0f;
    float totalCarbonEmitted = 0f;

    public float GetTotalEnergyUsed() => totalEnergyUsed;
    public float GetTotalCarbonEmitted() => totalCarbonEmitted;



    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!drawZones) return;
        foreach (var zone in ecoZones)
            if (zone != null) zone.DrawDebug();
    }

    // -----------------------------
    // ZONE CHECKING
    // -----------------------------
    public float GetZonePenalty(Vector3 position)
    {
        float penalty = 0f;

        foreach (var zone in ecoZones)
        {
            if (zone == null) continue;
            if (zone.Contains(position))
                penalty += zone.ecoPenalty;
        }

        return Mathf.Clamp(penalty, 0, 5f);
    }

    public Vector3 GetEcoRepulsion(Vector3 pos)
    {
        Vector3 total = Vector3.zero;

        foreach (var zone in ecoZones)
        {
            if (zone == null || !zone.Contains(pos)) continue;

            Vector3 away = (pos - zone.center);
            if (away.sqrMagnitude > 0.01f)
                total += away.normalized * zone.repulsionStrength;
        }

        return total;
    }

    // -----------------------------
    // ENERGY MODEL
    // -----------------------------
    public void ApplyEnergyCost(EcoFlightController flight, float speed, float turnMagnitude)
    {
        float zonePenalty = GetZonePenalty(flight.transform.position);

        float cost = baseEnergyCost 
                    + speedCostMultiplier * speed
                    + turnCostMultiplier * turnMagnitude;

        cost *= (1f + zonePenalty);

        totalEnergyUsed += cost;
        totalCarbonEmitted += cost * carbonCoefficient;
    }

    public float GetTotalEnergyUsedSafe()
    {
        // return stored total if you keep a running total, else 0
        return totalEnergyUsed; // you must have this variable; if not, compute or return 0f
    }

    public float GetTotalCarbonEmittedSafe()
    {
        return totalCarbonEmitted; // same as above
    }


}
