using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 5: Central telemetry dashboard manager.
/// Controls all UI panels, threat lists, and mission status displays.
/// </summary>
public class TelemetryUIManager : MonoBehaviour
{
    public static TelemetryUIManager Instance;

    [Header("References")]
    public DroneManager droneManager;
    public SwarmCoordinator swarmCoordinator;
    public PredictiveSwarmCoordinator predictiveCoordinator;
    public SensorFusion sensorFusion;
    public MissionTelemetry telemetry;
    public EcoConservationManager ecoManager;

    [Header("UI Panels - Assign from Hierarchy")]
    public GameObject missionStatusPanel;
    public GameObject threatListPanel;
    public GameObject swarmViewPanel;
    public GameObject interceptionTimelinePanel;

    [Header("Mission Status Elements")]
    public TextMeshProUGUI missionStatusText;
    public TextMeshProUGUI missionTimerText;
    public TextMeshProUGUI intrudersNeutralizedText;
    public TextMeshProUGUI breachAttemptsText;
    public Image missionStatusIcon;
    public Color successColor = Color.green;
    public Color failColor = Color.red;
    public Color activeColor = Color.yellow;

    [Header("Threat List Elements")]
    public Transform threatListContent;
    public GameObject threatListItemPrefab;
    public int maxThreatItems = 10;

    [Header("Swarm View Elements")]
    public TextMeshProUGUI activeInterceptorsText;
    public TextMeshProUGUI activeSquadsText;
    public TextMeshProUGUI totalInterceptionsText;
    public Slider swarmHealthSlider;

    [Header("Energy/Eco Display")]
    public TextMeshProUGUI energyUsedText;
    public TextMeshProUGUI carbonEmittedText;
    public Image ecoEfficiencyBar;

    [Header("Interception Timeline")]
    public Transform timelineContent;
    public GameObject timelineEventPrefab;
    public int maxTimelineEvents = 20;

    [Header("Export")]
    public Button exportCSVButton;

    // Runtime tracking
    private List<GameObject> activeThreatItems = new List<GameObject>();
    private List<GameObject> activeTimelineEvents = new List<GameObject>();
    private float missionStartTime;
    private Dictionary<int, ThreatTrackingData> trackedThreats = new Dictionary<int, ThreatTrackingData>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Auto-find references if not set
        if (droneManager == null) droneManager = DroneManager.Instance;
        if (swarmCoordinator == null) swarmCoordinator = FindObjectOfType<SwarmCoordinator>();
        if (predictiveCoordinator == null) predictiveCoordinator = FindObjectOfType<PredictiveSwarmCoordinator>();
        if (sensorFusion == null) sensorFusion = FindObjectOfType<SensorFusion>();
        if (telemetry == null) telemetry = MissionTelemetry.Instance;
        if (ecoManager == null) ecoManager = EcoConservationManager.Instance;

        // Setup export button
        if (exportCSVButton != null)
            exportCSVButton.onClick.AddListener(ExportMissionData);

        missionStartTime = Time.time;
        
        UpdateMissionStatus("MISSION ACTIVE", activeColor);
    }

    void Update()
    {
        UpdateMissionTimer();
        UpdateMissionStats();
        UpdateThreatList();
        UpdateSwarmView();
        UpdateEcoMetrics();
    }

    // ============================================
    // MISSION STATUS
    // ============================================
    void UpdateMissionTimer()
    {
        if (missionTimerText != null)
        {
            float elapsed = Time.time - missionStartTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            missionTimerText.text = $"TIME: {minutes:00}:{seconds:00}";
        }
    }

    void UpdateMissionStats()
    {
        if (telemetry == null) return;

        if (intrudersNeutralizedText != null)
            intrudersNeutralizedText.text = $"Neutralized: {telemetry.intrudersDestroyed}/{telemetry.intrudersSpawned}";

        if (breachAttemptsText != null)
            breachAttemptsText.text = $"Breaches: {telemetry.breachAttempts}";
    }

    public void UpdateMissionStatus(string status, Color color)
    {
        if (missionStatusText != null)
            missionStatusText.text = status;

        if (missionStatusIcon != null)
            missionStatusIcon.color = color;
    }

    public void OnMissionSuccess()
    {
        UpdateMissionStatus("✓ MISSION SUCCESS", successColor);
        AddTimelineEvent("Mission completed successfully", Color.green);
        ExportMissionData();
    }

    public void OnMissionFailed()
    {
        UpdateMissionStatus("✗ CORE BREACHED", failColor);
        AddTimelineEvent("CORE BREACH - Mission failed", Color.red);
        ExportMissionData();
    }

    // ============================================
    // THREAT LIST
    // ============================================
    void UpdateThreatList()
    {
        if (threatListContent == null || threatListItemPrefab == null) return;

        // Get current intruders
        var intruders = FindObjectsOfType<IntruderMeta>();
        
        // Clear old items that are no longer valid
        activeThreatItems.RemoveAll(item => item == null);
        
        // Update or create threat items
        for (int i = 0; i < intruders.Length && i < maxThreatItems; i++)
        {
            var intruder = intruders[i];
            if (intruder == null) continue;

            int id = intruder.GetInstanceID();
            
            // Get threat data
            Vector3 pos = intruder.transform.position;
            Vector3 vel = sensorFusion != null ? sensorFusion.GetVelocity(intruder) : Vector3.zero;
            float speed = vel.magnitude;
            float distToCore = droneManager != null && droneManager.ProtectedCore != null 
                ? Vector3.Distance(pos, droneManager.ProtectedCore.position) 
                : 0f;

            // Create or update threat item
            GameObject item = null;
            if (i < activeThreatItems.Count)
            {
                item = activeThreatItems[i];
            }
            else
            {
                item = Instantiate(threatListItemPrefab, threatListContent);
                activeThreatItems.Add(item);
            }

            UpdateThreatItem(item, intruder.name, speed, distToCore, intruder.isHypersonic);
        }

        // Remove excess items
        while (activeThreatItems.Count > intruders.Length)
        {
            var last = activeThreatItems[activeThreatItems.Count - 1];
            activeThreatItems.RemoveAt(activeThreatItems.Count - 1);
            if (last != null) Destroy(last);
        }
    }

    void UpdateThreatItem(GameObject item, string name, float speed, float distance, bool isHypersonic)
    {
        if (item == null) return;

        var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        var speedText = item.transform.Find("SpeedText")?.GetComponent<TextMeshProUGUI>();
        var distanceText = item.transform.Find("DistanceText")?.GetComponent<TextMeshProUGUI>();
        var threatIcon = item.transform.Find("ThreatIcon")?.GetComponent<Image>();

        if (nameText) nameText.text = name;
        if (speedText) speedText.text = $"{speed:F0} m/s";
        if (distanceText) distanceText.text = $"{distance:F0}m";
        
        if (threatIcon)
        {
            threatIcon.color = isHypersonic ? Color.red : 
                              speed > 90f ? Color.magenta :
                              speed > 40f ? Color.yellow : Color.green;
        }
    }

    // ============================================
    // SWARM VIEW
    // ============================================
    void UpdateSwarmView()
    {
        if (droneManager == null) return;

        var interceptors = droneManager.GetInterceptors();
        int activeCount = interceptors?.Count(i => i != null && i.IsAssigned()) ?? 0;
        int totalCount = interceptors?.Count ?? 0;

        if (activeInterceptorsText != null)
            activeInterceptorsText.text = $"Active: {activeCount}/{totalCount}";

        if (activeSquadsText != null && swarmCoordinator != null)
            activeSquadsText.text = $"Squads: {swarmCoordinator.GetActiveSquadCount()}";

        if (totalInterceptionsText != null && droneManager != null)
            totalInterceptionsText.text = $"Interceptions: {droneManager.totalInterceptions}";

        if (swarmHealthSlider != null)
            swarmHealthSlider.value = totalCount > 0 ? (float)activeCount / totalCount : 0f;
    }

    // ============================================
    // ECO METRICS
    // ============================================
    void UpdateEcoMetrics()
    {
        if (ecoManager == null) return;

        if (energyUsedText != null)
            energyUsedText.text = $"Energy: {ecoManager.GetTotalEnergyUsed():F1} kJ";

        if (carbonEmittedText != null)
            carbonEmittedText.text = $"Carbon: {ecoManager.GetTotalCarbonEmitted():F2} kg";

        if (ecoEfficiencyBar != null)
        {
            // Efficiency: lower is better (inverted scale)
            float efficiency = Mathf.Clamp01(1f - (ecoManager.GetTotalCarbonEmitted() / 100f));
            ecoEfficiencyBar.fillAmount = efficiency;
            ecoEfficiencyBar.color = Color.Lerp(Color.red, Color.green, efficiency);
        }
    }

    // ============================================
    // INTERCEPTION TIMELINE
    // ============================================
    public void AddTimelineEvent(string message, Color color)
    {
        if (timelineContent == null || timelineEventPrefab == null) return;

        // Create event item
        GameObject eventItem = Instantiate(timelineEventPrefab, timelineContent);
        
        var timeText = eventItem.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
        var messageText = eventItem.transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
        var icon = eventItem.transform.Find("Icon")?.GetComponent<Image>();

        float elapsed = Time.time - missionStartTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);

        if (timeText) timeText.text = $"{minutes:00}:{seconds:00}";
        if (messageText) messageText.text = message;
        if (icon) icon.color = color;

        activeTimelineEvents.Add(eventItem);

        // Limit timeline events
        while (activeTimelineEvents.Count > maxTimelineEvents)
        {
            var oldest = activeTimelineEvents[0];
            activeTimelineEvents.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }

        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        if (timelineContent.GetComponent<ScrollRect>() != null)
        {
            timelineContent.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
        }
    }

    // ============================================
    // DATA EXPORT
    // ============================================
    void ExportMissionData()
    {
        if (telemetry != null)
        {
            telemetry.ExportToCSV();
            AddTimelineEvent("Mission data exported", Color.cyan);
            Debug.Log("[TelemetryUI] CSV export triggered");
        }
    }

    // ============================================
    // PUBLIC API
    // ============================================
    public void RegisterIntercept(string interceptorName, string intruderName)
    {
        AddTimelineEvent($"{interceptorName} neutralized {intruderName}", Color.yellow);
    }

    public void RegisterBreach()
    {
        AddTimelineEvent("⚠ BREACH ATTEMPT", Color.red);
    }

    public void RegisterWaveStart(int waveNumber)
    {
        AddTimelineEvent($"Wave {waveNumber} spawned", Color.cyan);
    }
}

// ============================================
// DATA STRUCTURES
// ============================================
public struct ThreatTrackingData
{
    public int id;
    public float firstSeenTime;
    public Vector3 lastPosition;
    public float lastSpeed;
}