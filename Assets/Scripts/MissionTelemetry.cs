using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

public class MissionTelemetry : MonoBehaviour
{
    public static MissionTelemetry Instance;

    [Header("Links")]
    public DroneManager droneManager;
    public SwarmCoordinator swarm;
    public EcoConservationManager eco;
    public MockClassifier mock;

    [Header("Live Stats")]
    public int wavesSpawned = 0;
    public int intrudersSpawned = 0;
    public int intrudersDestroyed = 0;
    public int breachAttempts = 0;

    public bool missionActive = false;

    private float missionStartTime;
    private float missionEndTime;

    private int totalSpawns = 0;
    private List<Vector3> spawnPositions = new List<Vector3>();
    private List<Vector3> neutralizationPositions = new List<Vector3>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!droneManager) droneManager = DroneManager.Instance;
        if (!swarm) swarm = FindObjectOfType<SwarmCoordinator>();
        if (!eco) eco = EcoConservationManager.Instance;
        if (!mock) mock = MockClassifier.Instance;

    }

    public void BeginMission()
    {
        missionActive = true;
        wavesSpawned = 0;
        intrudersSpawned = 0;
        intrudersDestroyed = 0;
        breachAttempts = 0;
        totalSpawns = 0;
        spawnPositions.Clear();
        neutralizationPositions.Clear();
        missionStartTime = Time.time;
        Debug.Log("[Telemetry] Mission Started");
    }

    public void EndMission()
    {
        missionActive = false;
        missionEndTime = Time.time;
        Debug.Log("[Telemetry] Mission Ended");
        ExportToCSV();
        ExportHeatmapCSV();
        DumpConsoleSummary();
        if (mock != null)
        {
            mock.FinalizeMetrics();
        }
        else
        {
            Debug.LogWarning("[Telemetry] MockClassifier not assigned — skipping FinalizeMetrics().");
        }
    }

    public void SetMissionStatus(string status)
    {
        Debug.Log("[Telemetry] Mission Status → " + status);
    }

    public void RegisterWave(int count)
    {
        wavesSpawned++;
        Debug.Log($"[Telemetry] Wave {wavesSpawned} spawned ({count} intruders)");
    }

    public void RegisterSpawn(IntruderMeta meta)
    {
        intrudersSpawned++;
        totalSpawns++;
        if (meta != null) spawnPositions.Add(meta.transform.position);
        Debug.Log("[Telemetry] Spawn registered");
    }

    public void RegisterKill()
    {
        intrudersDestroyed++;
    }

    public void RecordBreachAttempt()
    {
        breachAttempts++;
    }

    public void CoreBreached()
    {
        breachAttempts++;
        Debug.Log("[Telemetry] CORE BREACHED!");
    }

    public void RegisterNeutralizationPosition(Vector3 p)
    {
        neutralizationPositions.Add(p);
    }

    // CSV output
    public void ExportToCSV()
    {
        string basePath = Application.persistentDataPath;
        string filename = "MissionReport.csv";
        string path = Path.Combine(basePath, filename);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine("Mission Duration," + (missionEndTime - missionStartTime));
        sb.AppendLine("Waves Spawned," + wavesSpawned);
        sb.AppendLine("Intruders Spawned," + intrudersSpawned);
        sb.AppendLine("Intruders Destroyed," + intrudersDestroyed);
        sb.AppendLine("Breach Attempts," + breachAttempts);

        if (swarm) sb.AppendLine("Active Squads," + swarm.GetActiveSquadCount());
        if (eco)
        {
            sb.AppendLine("Energy Used," + eco.GetTotalEnergyUsed());
            sb.AppendLine("Carbon Emitted," + eco.GetTotalCarbonEmitted());
        }

        // Safe write: try, if locked write with timestamp suffix
        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[Telemetry] Mission report exported → " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("[Telemetry] Could not write CSV: " + e.Message);
            // fallback
            string alt = Path.Combine(basePath, $"MissionReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            try
            {
                File.WriteAllText(alt, sb.ToString());
                Debug.Log("[Telemetry] Mission report exported (alt) → " + alt);
            }
            catch (Exception ee)
            {
                Debug.LogError("[Telemetry] Failed backup CSV write: " + ee.Message);
            }
        }
    }

    // Heatmap CSV for neutralizations
    public void ExportHeatmapCSV()
    {
        if (neutralizationPositions == null || neutralizationPositions.Count == 0)
        {
            Debug.Log("[Telemetry] No neutralizations to export for heatmap.");
            return;
        }

        string basePath = Application.persistentDataPath;
        string filename = "NeutralizationHeatmap.csv";
        string path = Path.Combine(basePath, filename);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("x,y,z");
        foreach (var p in neutralizationPositions)
            sb.AppendLine($"{p.x:F2},{p.y:F2},{p.z:F2}");

        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[Telemetry] Heatmap CSV exported → " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("[Telemetry] Could not write heatmap CSV: " + e.Message);
            string alt = Path.Combine(basePath, $"NeutralizationHeatmap_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            try
            {
                File.WriteAllText(alt, sb.ToString());
                Debug.Log("[Telemetry] Heatmap CSV exported (alt) → " + alt);
            }
            catch (Exception ee)
            {
                Debug.LogError("[Telemetry] Failed backup heatmap CSV write: " + ee.Message);
            }
        }
    }

    void DumpConsoleSummary()
    {
        Debug.Log("----- MISSION SUMMARY -----");
        Debug.Log($"Duration: {missionEndTime - missionStartTime:F2}s");
        Debug.Log($"Waves: {wavesSpawned}");
        Debug.Log($"Spawned: {intrudersSpawned}");
        Debug.Log($"Neutralized/Destroyed: {intrudersDestroyed}");
        Debug.Log($"Breach attempts: {breachAttempts}");
        Debug.Log("---------------------------");
    }

    // Small wrappers for external debug
    public int GetTotalSpawns() => totalSpawns;
    public List<Vector3> GetNeutralizationPositions() => new List<Vector3>(neutralizationPositions);

    // NOTE: placeholders in case other code calls these
}
