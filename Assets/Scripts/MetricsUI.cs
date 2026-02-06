using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight UI metrics updater. Hook up references in inspector.
/// </summary>
public class MetricsUI : MonoBehaviour
{
    public static MetricsUI Instance { get; private set; }

    public Text intruderCountText;
    public Text neutralizedText;
    public Text breachText;
    public GameObject missionResultPanel;
    public Text missionResultText;

    int intruderCount = 0;
    int neutralized = 0;
    int breaches = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (missionResultPanel != null)
            missionResultPanel.SetActive(false);

        RefreshUI();
    }

    void RefreshUI()
    {
        if (intruderCountText != null)
            intruderCountText.text = $"Intruders: {intruderCount}";

        if (neutralizedText != null)
            neutralizedText.text = $"Neutralized: {neutralized}";

        if (breachText != null)
            breachText.text = $"Breaches: {breaches}";
    }

    public void SetMissionActive()
    {
        if (missionResultPanel != null)
            missionResultPanel.SetActive(false);
        RefreshUI();
    }

    public void SetInterceptorCount(int count)
    {
        // optional extension if you want interceptor UI later
    }

    public void OnIntruderSpawned(int totalActive)
    {
        intruderCount = totalActive;
        RefreshUI();
    }

    public void OnIntruderDespawned(bool wasNeutralized, int totalActive)
    {
        intruderCount = totalActive;
        if (wasNeutralized) neutralized++;
        RefreshUI();
    }

    public void OnBreach()
    {
        breaches++;
        RefreshUI();

        if (missionResultPanel == null) return;
        missionResultPanel.SetActive(true);
        missionResultText.text = $"BREACH DETECTED\nNeutralized: {neutralized}\nBreaches: {breaches}";
    }

    public void OnMissionSuccess()
    {
        if (missionResultPanel == null) return;
        missionResultPanel.SetActive(true);
        missionResultText.text = $"MISSION SUCCESS\nNeutralized: {neutralized}\nBreaches: {breaches}";
    }

    public void OnMissionFailed()
    {
        if (missionResultPanel == null) return;
        missionResultPanel.SetActive(true);
        missionResultText.text = $"MISSION FAILED\nNeutralized: {neutralized}\nBreaches: {breaches}";
    }
}


