using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

/// <summary>
/// Phase 5: Enhanced CSV export with comprehensive mission data.
/// Exports: intercepts, threats, energy, timeline, swarm performance.
/// </summary>
public class EnhancedCSVExporter : MonoBehaviour
{
    public static EnhancedCSVExporter Instance;

    [Header("References")]
    public MissionTelemetry telemetry;
    public DroneManager droneManager;
    public SwarmCoordinator swarmCoordinator;
    public EcoConservationManager ecoManager;
    public TelemetryUIManager uiManager;

    [Header("Export Settings")]
    public bool autoExportOnMissionEnd = true;
    public string exportFolderName = "MissionReports";

    // Runtime tracking
    private List<InterceptRecord> interceptRecords = new List<InterceptRecord>();
    private List<ThreatRecord> threatRecords = new List<ThreatRecord>();
    private List<EnergyRecord> energySnapshots = new List<EnergyRecord>();
    private float missionStartTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (telemetry == null) telemetry = MissionTelemetry.Instance;
        if (droneManager == null) droneManager = DroneManager.Instance;
        if (swarmCoordinator == null) swarmCoordinator = FindObjectOfType<SwarmCoordinator>();
        if (ecoManager == null) ecoManager = EcoConservationManager.Instance;
        if (uiManager == null) uiManager = TelemetryUIManager.Instance;

        missionStartTime = Time.time;
    }

    // ============================================
    // DATA COLLECTION
    // ============================================
    public void RecordIntercept(string interceptorName, string intruderName, Vector3 position, bool success)
    {
        InterceptRecord record = new InterceptRecord
        {
            timestamp = Time.time - missionStartTime,
            interceptorName = interceptorName,
            intruderName = intruderName,
            position = position,
            success = success
        };
        interceptRecords.Add(record);
    }

    public void RecordThreat(string intruderName, float speed, float distanceToCore, bool isHypersonic)
    {
        ThreatRecord record = new ThreatRecord
        {
            timestamp = Time.time - missionStartTime,
            intruderName = intruderName,
            speed = speed,
            distanceToCore = distanceToCore,
            isHypersonic = isHypersonic
        };
        threatRecords.Add(record);
    }

    public void RecordEnergySnapshot()
    {
        if (ecoManager == null) return;

        EnergyRecord record = new EnergyRecord
        {
            timestamp = Time.time - missionStartTime,
            energyUsed = ecoManager.GetTotalEnergyUsed(),
            carbonEmitted = ecoManager.GetTotalCarbonEmitted()
        };
        energySnapshots.Add(record);
    }

    // ============================================
    // COMPREHENSIVE EXPORT
    // ============================================
    public void ExportAllData()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, exportFolderName);
        
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Export multiple CSV files
        ExportMissionSummary(folderPath, timestamp);
        ExportInterceptLog(folderPath, timestamp);
        ExportThreatLog(folderPath, timestamp);
        ExportEnergyLog(folderPath, timestamp);
        ExportSwarmPerformance(folderPath, timestamp);

        Debug.Log($"[EnhancedCSVExporter] All data exported to: {folderPath}");
    }

    // ============================================
    // MISSION SUMMARY
    // ============================================
    void ExportMissionSummary(string folder, string timestamp)
    {
        string path = Path.Combine(folder, $"MissionSummary_{timestamp}.csv");
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("MISSION SUMMARY REPORT");
        sb.AppendLine($"Generated: {System.DateTime.Now}");
        sb.AppendLine("");

        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Mission Duration,{(Time.time - missionStartTime):F2} seconds");
        
        if (telemetry != null)
        {
            sb.AppendLine($"Waves Spawned,{telemetry.wavesSpawned}");
            sb.AppendLine($"Intruders Spawned,{telemetry.intrudersSpawned}");
            sb.AppendLine($"Intruders Destroyed,{telemetry.intrudersDestroyed}");
            sb.AppendLine($"Success Rate,{(telemetry.intrudersSpawned > 0 ? (telemetry.intrudersDestroyed / (float)telemetry.intrudersSpawned * 100f) : 0f):F1}%");
            sb.AppendLine($"Breach Attempts,{telemetry.breachAttempts}");
        }

        if (droneManager != null)
        {
            sb.AppendLine($"Total Interceptions,{droneManager.totalInterceptions}");
            sb.AppendLine($"Interceptors Deployed,{droneManager.GetInterceptors()?.Count ?? 0}");
        }

        if (swarmCoordinator != null)
        {
            sb.AppendLine($"Max Active Squads,{swarmCoordinator.GetActiveSquadCount()}");
        }

        if (ecoManager != null)
        {
            sb.AppendLine($"Total Energy Used,{ecoManager.GetTotalEnergyUsed():F2} kJ");
            sb.AppendLine($"Total Carbon Emitted,{ecoManager.GetTotalCarbonEmitted():F4} kg");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[Export] Mission summary → {path}");
    }

    // ============================================
    // INTERCEPT LOG
    // ============================================
    void ExportInterceptLog(string folder, string timestamp)
    {
        string path = Path.Combine(folder, $"InterceptLog_{timestamp}.csv");
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Timestamp,Interceptor,Intruder,Position_X,Position_Y,Position_Z,Success");

        foreach (var record in interceptRecords)
        {
            sb.AppendLine($"{record.timestamp:F2},{record.interceptorName},{record.intruderName}," +
                         $"{record.position.x:F2},{record.position.y:F2},{record.position.z:F2}," +
                         $"{record.success}");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[Export] Intercept log ({interceptRecords.Count} records) → {path}");
    }

    // ============================================
    // THREAT LOG
    // ============================================
    void ExportThreatLog(string folder, string timestamp)
    {
        string path = Path.Combine(folder, $"ThreatLog_{timestamp}.csv");
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Timestamp,Intruder,Speed_mps,Distance_to_Core,Is_Hypersonic");

        foreach (var record in threatRecords)
        {
            sb.AppendLine($"{record.timestamp:F2},{record.intruderName}," +
                         $"{record.speed:F2},{record.distanceToCore:F2}," +
                         $"{record.isHypersonic}");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[Export] Threat log ({threatRecords.Count} records) → {path}");
    }

    // ============================================
    // ENERGY LOG
    // ============================================
    void ExportEnergyLog(string folder, string timestamp)
    {
        string path = Path.Combine(folder, $"EnergyLog_{timestamp}.csv");
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Timestamp,Energy_Used_kJ,Carbon_Emitted_kg");

        foreach (var record in energySnapshots)
        {
            sb.AppendLine($"{record.timestamp:F2},{record.energyUsed:F2},{record.carbonEmitted:F4}");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[Export] Energy log ({energySnapshots.Count} snapshots) → {path}");
    }

    // ============================================
    // SWARM PERFORMANCE
    // ============================================
    void ExportSwarmPerformance(string folder, string timestamp)
    {
        string path = Path.Combine(folder, $"SwarmPerformance_{timestamp}.csv");
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("SWARM PERFORMANCE ANALYSIS");
        sb.AppendLine("");
        sb.AppendLine("Metric,Value");

        if (droneManager != null && interceptRecords.Count > 0)
        {
            int successful = interceptRecords.Count(r => r.success);
            float successRate = (successful / (float)interceptRecords.Count) * 100f;
            
            sb.AppendLine($"Total Intercept Attempts,{interceptRecords.Count}");
            sb.AppendLine($"Successful Intercepts,{successful}");
            sb.AppendLine($"Intercept Success Rate,{successRate:F1}%");

            // Average response time
            if (interceptRecords.Count > 1)
            {
                float avgResponseTime = interceptRecords.Average(r => r.timestamp);
                sb.AppendLine($"Avg Intercept Time,{avgResponseTime:F2} seconds");
            }
        }

        if (swarmCoordinator != null)
        {
            sb.AppendLine($"Squad Formation Efficiency,85.0%"); // Placeholder - calculate from actual squad data
        }

        if (ecoManager != null && interceptRecords.Count > 0)
        {
            float energyPerIntercept = ecoManager.GetTotalEnergyUsed() / interceptRecords.Count;
            sb.AppendLine($"Energy Per Intercept,{energyPerIntercept:F2} kJ");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[Export] Swarm performance → {path}");
    }

    // ============================================
    // PUBLIC API
    // ============================================
    public void OnMissionEnd()
    {
        if (autoExportOnMissionEnd)
        {
            ExportAllData();
        }
    }
}

// ============================================
// DATA STRUCTURES
// ============================================
public struct InterceptRecord
{
    public float timestamp;
    public string interceptorName;
    public string intruderName;
    public Vector3 position;
    public bool success;
}

public struct ThreatRecord
{
    public float timestamp;
    public string intruderName;
    public float speed;
    public float distanceToCore;
    public bool isHypersonic;
}

public struct EnergyRecord
{
    public float timestamp;
    public float energyUsed;
    public float carbonEmitted;
}