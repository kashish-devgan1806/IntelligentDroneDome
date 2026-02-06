using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// PHASE 1 CLEANUP: Real file logging with CSV export support
/// </summary>
public class SimulationLogger : MonoBehaviour
{
    [Header("Logging Config")]
    public bool enableFileLogging = true;
    public bool enableConsoleLogging = true;
    public string logFileName = "simulation_log";

    private string logFilePath;
    private StringBuilder logBuffer = new StringBuilder();
    private int logCount = 0;

    void Awake()
    {
        if (enableFileLogging)
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(Application.persistentDataPath, $"{logFileName}_{timestamp}.csv");

            // Write CSV header
            logBuffer.AppendLine("Timestamp,Category,Message");
            Debug.Log($"[SimulationLogger] Logging to: {logFilePath}");
        }
    }

    public void Log(string message)
    {
        logCount++;
        string timestamp = Time.time.ToString("F2");

        // Console output
        if (enableConsoleLogging)
        {
            Debug.Log($"[LOG {logCount}] {message}");
        }

        // File output
        if (enableFileLogging)
        {
            string category = ExtractCategory(message);
            string cleanMsg = message.Replace(",", ";"); // CSV-safe
            logBuffer.AppendLine($"{timestamp},{category},{cleanMsg}");

            // Flush every 10 logs
            if (logCount % 10 == 0)
            {
                FlushToFile();
            }
        }
    }

    string ExtractCategory(string msg)
    {
        if (msg.Contains("Spawn")) return "SPAWN";
        if (msg.Contains("neutralized")) return "NEUTRALIZE";
        if (msg.Contains("BREACH")) return "BREACH";
        if (msg.Contains("SUCCESS")) return "SUCCESS";
        if (msg.Contains("assigned")) return "ASSIGN";
        return "INFO";
    }

    void FlushToFile()
    {
        if (!enableFileLogging || string.IsNullOrEmpty(logFilePath))
            return;

        try
        {
            File.AppendAllText(logFilePath, logBuffer.ToString());
            logBuffer.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimulationLogger] Failed to write log: {e.Message}");
        }
    }

    void OnDestroy()
    {
        FlushToFile();
        if (enableFileLogging)
        {
            Debug.Log($"[SimulationLogger] Final log saved to: {logFilePath}");
        }
    }

    void OnApplicationQuit()
    {
        FlushToFile();
    }
}