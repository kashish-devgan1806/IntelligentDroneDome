using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 5: Interactive interception timeline visualization.
/// Shows past, current, and predicted future intercepts on a temporal graph.
/// </summary>
public class InterceptionTimelineDisplay : MonoBehaviour
{
    [Header("References")]
    public DroneManager droneManager;
    public SwarmCoordinator swarmCoordinator;
    public PredictiveSwarmCoordinator predictiveCoordinator;

    [Header("UI Elements")]
    public RectTransform timelineCanvas;
    public GameObject eventMarkerPrefab;
    public TextMeshProUGUI timeScaleText;
    public Slider timeRangeSlider;

    [Header("Timeline Settings")]
    public float timelineWidth = 800f;
    public float timelineHeight = 400f;
    public float timeWindowSeconds = 30f; // show last 30 seconds
    public float futureWindowSeconds = 10f; // predict 10 seconds ahead

    [Header("Visual Settings")]
    public Color pastEventColor = Color.gray;
    public Color currentEventColor = Color.yellow;
    public Color futureEventColor = Color.cyan;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    [Header("Event Tracking")]
    public int maxEventsDisplayed = 50;

    // Runtime data
    private List<TimelineEvent> events = new List<TimelineEvent>();
    private List<GameObject> eventMarkers = new List<GameObject>();
    private float missionStartTime;
    private float currentTime => Time.time - missionStartTime;

    void Start()
    {
        if (droneManager == null) droneManager = DroneManager.Instance;
        if (swarmCoordinator == null) swarmCoordinator = FindObjectOfType<SwarmCoordinator>();
        if (predictiveCoordinator == null) predictiveCoordinator = FindObjectOfType<PredictiveSwarmCoordinator>();

        missionStartTime = Time.time;

        if (timeRangeSlider != null)
            timeRangeSlider.onValueChanged.AddListener(OnTimeRangeChanged);

        AddEvent("Mission started", TimelineEventType.MissionStart, 0f);
    }

    void Update()
    {
        UpdateTimeline();
        PredictFutureIntercepts();
        CleanOldEvents();
    }

    // ============================================
    // TIMELINE UPDATE
    // ============================================
    void UpdateTimeline()
    {
        if (timelineCanvas == null) return;

        // Clear old markers
        foreach (var marker in eventMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        eventMarkers.Clear();

        // Draw timeline axis
        DrawTimelineAxis();

        // Draw events
        float minTime = currentTime - timeWindowSeconds;
        float maxTime = currentTime + futureWindowSeconds;

        foreach (var evt in events)
        {
            if (evt.time < minTime || evt.time > maxTime) continue;

            DrawEventMarker(evt, minTime, maxTime);
        }

        // Update time scale display
        if (timeScaleText != null)
        {
            timeScaleText.text = $"Time: {currentTime:F1}s | Window: {timeWindowSeconds:F0}s";
        }
    }

    void DrawTimelineAxis()
    {
        // Time axis line (horizontal)
        // This would ideally use a LineRenderer or UI.Image for the actual line
        // For simplicity, we'll use event markers to show the axis points
    }

    void DrawEventMarker(TimelineEvent evt, float minTime, float maxTime)
    {
        if (eventMarkerPrefab == null || timelineCanvas == null) return;

        // Calculate normalized position on timeline
        float normalizedTime = Mathf.InverseLerp(minTime, maxTime, evt.time);
        float xPos = normalizedTime * timelineWidth - (timelineWidth * 0.5f);

        // Y position based on event type (stagger for visibility)
        float yPos = GetYPositionForEventType(evt.type);

        // Create marker
        GameObject marker = Instantiate(eventMarkerPrefab, timelineCanvas);
        RectTransform markerRT = marker.GetComponent<RectTransform>();
        if (markerRT != null)
        {
            markerRT.anchoredPosition = new Vector2(xPos, yPos);
        }

        // Set marker appearance
        var markerImage = marker.GetComponent<Image>();
        var markerText = marker.GetComponentInChildren<TextMeshProUGUI>();

        if (markerImage != null)
        {
            markerImage.color = GetColorForEvent(evt);
        }

        if (markerText != null)
        {
            markerText.text = evt.label;
            markerText.fontSize = 10;
        }

        // Add tooltip
        var tooltip = marker.AddComponent<TimelineTooltip>();
        tooltip.eventData = evt;

        eventMarkers.Add(marker);
    }

    float GetYPositionForEventType(TimelineEventType type)
    {
        switch (type)
        {
            case TimelineEventType.Intercept: return 100f;
            case TimelineEventType.Detection: return 50f;
            case TimelineEventType.Breach: return -50f;
            case TimelineEventType.Predicted: return 150f;
            default: return 0f;
        }
    }

    Color GetColorForEvent(TimelineEvent evt)
    {
        if (evt.time > currentTime)
            return futureEventColor;

        switch (evt.type)
        {
            case TimelineEventType.Intercept:
                return evt.success ? successColor : failColor;
            case TimelineEventType.Detection:
                return Color.yellow;
            case TimelineEventType.Breach:
                return failColor;
            case TimelineEventType.MissionStart:
                return Color.white;
            case TimelineEventType.Predicted:
                return futureEventColor;
            default:
                return pastEventColor;
        }
    }

    // ============================================
    // EVENT MANAGEMENT
    // ============================================
    public void AddEvent(string label, TimelineEventType type, float timeOffset = 0f, bool success = true)
    {
        TimelineEvent evt = new TimelineEvent
        {
            label = label,
            type = type,
            time = currentTime + timeOffset,
            success = success
        };

        events.Add(evt);

        // Limit total events
        if (events.Count > maxEventsDisplayed * 2)
        {
            events.RemoveRange(0, maxEventsDisplayed);
        }
    }

    void CleanOldEvents()
    {
        // Remove events older than 2x time window
        float cutoffTime = currentTime - (timeWindowSeconds * 2f);
        events.RemoveAll(e => e.time < cutoffTime && e.type != TimelineEventType.MissionStart);
    }

    // ============================================
    // PREDICTIVE INTERCEPTS
    // ============================================
    void PredictFutureIntercepts()
    {
        if (predictiveCoordinator == null || droneManager == null) return;

        // Remove old predictions
        events.RemoveAll(e => e.type == TimelineEventType.Predicted && e.time > currentTime);

        // Get active assignments
        var interceptors = droneManager.GetInterceptors();
        if (interceptors == null) return;

        foreach (var ic in interceptors)
        {
            if (ic == null || !ic.IsAssigned()) continue;

            Transform target = ic.GetAssignedTarget();
            if (target == null) continue;

            var intruderMeta = target.GetComponent<IntruderMeta>();
            if (intruderMeta == null) continue;

            // Estimate time to intercept
            float dist = Vector3.Distance(ic.transform.position, target.position);
            float speed = ic.speed;
            float eta = dist / Mathf.Max(speed, 1f);

            if (eta < futureWindowSeconds)
            {
                AddEvent(
                    $"Predicted: {ic.name} → {target.name}",
                    TimelineEventType.Predicted,
                    eta,
                    true
                );
            }
        }
    }

    // ============================================
    // PUBLIC API
    // ============================================
    public void RegisterIntercept(string interceptorName, string intruderName, bool success)
    {
        AddEvent($"{interceptorName} vs {intruderName}", TimelineEventType.Intercept, 0f, success);
    }

    public void RegisterDetection(string intruderName)
    {
        AddEvent($"Detected: {intruderName}", TimelineEventType.Detection, 0f);
    }

    public void RegisterBreach(string intruderName)
    {
        AddEvent($"⚠ BREACH: {intruderName}", TimelineEventType.Breach, 0f, false);
    }

    void OnTimeRangeChanged(float value)
    {
        timeWindowSeconds = Mathf.Lerp(10f, 60f, value);
    }
}

// ============================================
// DATA STRUCTURES
// ============================================
public struct TimelineEvent
{
    public string label;
    public TimelineEventType type;
    public float time; // seconds since mission start
    public bool success;
}

public enum TimelineEventType
{
    MissionStart,
    Detection,
    Intercept,
    Breach,
    WaveStart,
    Predicted
}

// ============================================
// TOOLTIP COMPONENT
// ============================================
public class TimelineTooltip : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    public TimelineEvent eventData;
    private GameObject tooltipPanel;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Show tooltip with event details
        // Implementation depends on your UI setup
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Hide tooltip
    }
}