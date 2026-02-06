using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EXACT RED ZONE VERSION:
/// - Interceptors spawn and patrol INSIDE red cylinder
/// - Intruders spawn OUTSIDE red zone
/// - Dynamic neutralize distance based on intruder count
/// </summary>
public class DroneManager : MonoBehaviour
{
    public static DroneManager Instance { get; private set; }

    [Header("Core & Spawn")]
    public Transform protectedCore;
    public Transform redZoneCylinder; // Assign your red cylinder here

    [Header("Prefabs")]
    public GameObject interceptorPrefab;
    public GameObject intruderPrefab;

    [Header("Counts")]
    [Range(1, 50)] public int interceptorCount = 30;
    [Range(1, 20)] public int minIntruders = 1;
    [Range(1, 50)] public int maxIntruders = 30;
    [Range(0f,1f)] public float hypersonicChance = 0.5f; // set in inspector


    [Header("Red Zone (Auto-calculated from cylinder)")]
    public Vector3 redZoneMin;
    public Vector3 redZoneMax;

    [Header("Spawn Zone")]
    // Interceptors (TOP 2 terrains)
    public Vector3 interceptorZoneMin = new Vector3(0f, 0f, 200f);
    public Vector3 interceptorZoneMax = new Vector3(2000f, 6f, 2500f);

    // Intruders (BOTTOM 2 terrains)
    public Vector3 intruderSpawnZoneMin = new Vector3(0f, 0f, -2500f);
    public Vector3 intruderSpawnZoneMax = new Vector3(2000f, 6f, -800f);


    [Header("Dynamic Neutralize Distance")]
    public float normalNeutralizeDistance = 40f;
    public float highThreatNeutralizeDistance = 80f;
    public int highThreatThreshold = 15;

    // Runtime
    private List<DroneController> interceptors = new List<DroneController>();
    private List<IntruderMeta> intruders = new List<IntruderMeta>();
    public float currentNeutralizeDistance { get; private set; }
    public bool simulationEndedFlag = false;

    public int totalInterceptions = 0;
    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Auto-find core
        if (protectedCore == null)
        {
            var go = GameObject.FindWithTag("ProtectedCore");
            if (go != null) protectedCore = go.transform;
        }

        // Auto-find red zone cylinder
        if (redZoneCylinder == null)
        {
            var go = GameObject.Find("ProtectedZone");
            if (go != null) redZoneCylinder = go.transform;
        }

        // Calculate red zone boundaries from cylinder
        CalculateRedZoneBounds();

        // Spawn drones
        SpawnInterceptors();
        SpawnRandomIntruders();

        // Update neutralize distance
        UpdateNeutralizeDistance();

        Debug.Log($"[LOG] patrol initialised: {interceptors.Count} interceptor spawned");
        Debug.Log($"[LOG] intrusion initialised: {intruders.Count} intrusion spawned");
        Debug.Log($"[LOG] Red Zone: {redZoneMin} to {redZoneMax}");
        Debug.Log($"[LOG] Neutralize Distance: {currentNeutralizeDistance}m");
    }

    void Update()
    {
        // Dynamically adjust neutralize distance
        UpdateNeutralizeDistance();
    }

    // ============================================
    // RED ZONE CALCULATION
    // ============================================
    void CalculateRedZoneBounds()
    {
        if (redZoneCylinder != null)
        {
            Vector3 pos = redZoneCylinder.position;
            Vector3 scale = redZoneCylinder.localScale;

            // Cylinder bounds (XZ plane)
            float radiusX = scale.x * 0.5f;
            float radiusZ = scale.z * 0.5f;

            redZoneMin = new Vector3(
                pos.x - radiusX,
                0f,
                pos.z - radiusZ
            );

            redZoneMax = new Vector3(
                pos.x + radiusX,
                6f,
                pos.z + radiusZ
            );

            Debug.Log($"[DroneManager] Red Zone calculated: Min={redZoneMin}, Max={redZoneMax}");
        }
        else
        {
            Debug.LogError("[DroneManager] Red Zone Cylinder not found! Using default bounds.");
            redZoneMin = new Vector3(36f, 0f, -1f);
            redZoneMax = new Vector3(1736f, 6f, 999f);
        }
    }

    // ============================================
    // DYNAMIC NEUTRALIZE DISTANCE
    // ============================================
    void UpdateNeutralizeDistance()
    {
        int activeCount = GetActiveIntruderCount();

        if (activeCount > highThreatThreshold)
        {
            currentNeutralizeDistance = highThreatNeutralizeDistance;
        }
        else
        {
            currentNeutralizeDistance = normalNeutralizeDistance;
        }

        // Update all interceptors
        foreach (var ic in interceptors)
        {
            if (ic != null)
            {
                ic.neutralizeDistance = currentNeutralizeDistance;
            }
        }
    }

    // ============================================
    // INTERCEPTOR SPAWNING (Inside Red Zone)
    // ============================================
    void SpawnInterceptors()
    {
        if (interceptorPrefab == null)
        {
            Debug.LogError("[DroneManager] interceptorPrefab not assigned!");
            return;
        }

        interceptors.Clear();
        for (int i = 0; i < interceptorCount; i++)
        {
            // Spawn randomly INSIDE red zone
            Vector3 pos = RandomPointInRedZone();

            GameObject go = Instantiate(interceptorPrefab, pos, Quaternion.identity);
            go.name = $"Interceptor_{i}";

            DroneController dc = go.GetComponent<DroneController>();
            if (dc == null)
            {
                Debug.LogError("[DroneManager] Interceptor prefab missing DroneController!");
                Destroy(go);
                continue;
            }

            dc.ConfigureAsInterceptor(redZoneMin, redZoneMax);
            dc.neutralizeDistance = currentNeutralizeDistance;
            interceptors.Add(dc);

            Debug.Log($"[DroneManager] Spawned {go.name} at {pos} (inside red zone)");
        }
    }

    Vector3 RandomPointInRedZone()
    {
        return new Vector3(
            Random.Range(redZoneMin.x + 50f, redZoneMax.x - 50f), // 50 unit margin
            Random.Range(1f, 4f),
            Random.Range(redZoneMin.z + 50f, redZoneMax.z - 50f)
        );
    }

    // ============================================
    // INTRUDER SPAWNING (Outside Red Zone)
    // ============================================
    void SpawnRandomIntruders()
    {
        if (intruderPrefab == null)
        {
            Debug.LogError("[DroneManager] intruderPrefab not assigned!");
            return;
        }

        intruders.Clear();
        int count = Random.Range(minIntruders, maxIntruders + 1);
        Debug.Log($"[DroneManager] Spawning {count} intruders");

        for (int i = 0; i < count; i++)
        {
            IntruderMeta meta = SpawnSingleIntruder(i);
            if (meta != null) intruders.Add(meta);
        }
    }

    IntruderMeta SpawnSingleIntruder(int index)
    {
        // Spawn OUTSIDE red zone (in intruder spawn zone)
        Vector3 pos = RandomPointInBox(intruderSpawnZoneMin, intruderSpawnZoneMax);


        // Ensure it's NOT inside red zone
        while (IsInsideRedZone(pos))
        {
            pos = RandomPointInBox(intruderSpawnZoneMin, intruderSpawnZoneMax);
        }

        GameObject go = Instantiate(intruderPrefab, pos, Quaternion.identity);
        go.name = $"Intruder_{index}";

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dirToCore = (protectedCore.position - pos).normalized;
            rb.velocity = dirToCore * Random.Range(8f, 18f); // tweak this range for difficulty
        }
        
        DroneController dc = go.GetComponent<DroneController>();
        IntruderMeta meta = go.GetComponent<IntruderMeta>();

        if (Random.value < hypersonicChance)
        {
            meta.SetHypersonic(true);
            // optional: also mark / notify telemetry
        }

        if (dc == null || meta == null)
        {
            Debug.LogError("[DroneManager] Intruder prefab missing components!");
            Destroy(go);
            return null;
        }

        // Intruders can move anywhere (no boundary clamp)
        Vector3 worldMin = new Vector3(-100f, 0f, -1100f);
        Vector3 worldMax = new Vector3(2100f, 6f, 1100f);
        dc.ConfigureAsIntruder(intruderSpawnZoneMin, intruderSpawnZoneMax, protectedCore);
        meta.Initialize(protectedCore.position);

        // NEW: register spawn with telemetry if available
        MissionTelemetry.Instance?.RegisterSpawn(meta);

        Debug.Log($"[DroneManager] Spawned {go.name} at {pos} (outside red zone)");
        return meta;
    }

    bool IsInsideRedZone(Vector3 pos)
    {
        return pos.x >= redZoneMin.x && pos.x <= redZoneMax.x &&
               pos.z >= redZoneMin.z && pos.z <= redZoneMax.z;
    }


    // ============================================
    // API
    // ============================================
    public Transform ProtectedCore => protectedCore;

    public int GetActiveIntruderCount()
    {
        intruders.RemoveAll(i => i == null);
        return intruders.Count;
    }

    public void NotifyIntruderNeutralized(IntruderMeta meta)
    {
        intruders.Remove(meta);
        Debug.Log($"[LOG] neutralized at {meta.lastKnownPosition}");
        CheckMissionEnd();
    }

    public void NotifyIntruderDestroyed(IntruderMeta meta)
    {
        intruders.Remove(meta);
        Debug.Log($"[LOG] destroyed at {meta.lastKnownPosition}");
        CheckMissionEnd();
    }

    // void CheckMissionEnd()
    // {
    //     if (GetActiveIntruderCount() == 0)
    //     {
    //         Debug.Log("[RESULT] BREACH UNSUCCESSFUL — ALL ENEMY DESTROYED!");
    //         EndSimulation(true);
    //     }
    // }

    // private void EndSimulation(bool success)
    // {
    //     foreach (var ic in interceptors) if (ic != null) ic.ForceStop();
    //     Debug.Log("[Simulation] Ending simulation. Success: " + success);
    //     // DO NOT modify Time.timeScale here (keeps editor responsive)
    //     MissionTelemetry.Instance?.SetMissionStatus(success ? "Success" : "Failure");
    //     MissionTelemetry.Instance?.EndMission();
    // }

    // public bool SimulationEnded = false;

    // public void EndSimulationFailure()
    // {
    //     if (SimulationEnded) return;
    //     SimulationEnded = true;

    //     MissionOrchestrator.Instance?.EndMission();

    //     MissionOrchestrator.Instance?.StopMission();
    // }

    private void CheckMissionEnd()
    {
        if (GetActiveIntruderCount() == 0)
        {
            Debug.Log("[RESULT] BREACH UNSUCCESSFUL — ALL ENEMY DESTROYED / NEUTRALISED.");
            EndMission(success: true);
        }
    }

    private void EndMission(bool success)
    {
        if (simulationEndedFlag) return;

        simulationEndedFlag = true;

        foreach (var ic in interceptors) 
            if (ic != null) ic.ForceStop();

        Debug.Log("[Simulation] Ending simulation. Success: " + success);

        MissionOrchestrator.Instance?.EndMission(success);
    }

    public void EndSimulationFailure()
    {
        if (simulationEndedFlag) return;

        simulationEndedFlag = true;

        Debug.Log("[RESULT] BREACH SUCCESSFUL!");

        foreach (var ic in interceptors) 
            if (ic != null) ic.ForceStop();

        MissionOrchestrator.Instance?.EndMission(false);
    }


    public void SimulationEnded(bool success)
    {
        EndMission(success);
    }

    // helper used elsewhere
    public List<DroneController> GetInterceptors() => interceptors;


    public int GetIntruderCount()
    {
        return FindObjectsOfType<IntruderMeta>().Length;
    }

    public void RegisterInterception()
    {
        totalInterceptions++;
    }

    // Add this method inside DroneManager class
    public Vector3 RandomPointInBox(Vector3 min, Vector3 max)
    {
        return new Vector3(
            UnityEngine.Random.Range(min.x, max.x),
            UnityEngine.Random.Range(min.y, max.y),
            UnityEngine.Random.Range(min.z, max.z)
        );
    }

    public Vector3 RandomPointInIntruderZone()
    {
        // clamp spawn inside intruder zone and keep small margin from fence
        float margin = 4f;
        Vector3 min = intruderSpawnZoneMin + new Vector3(margin, 0, margin);
        Vector3 max = intruderSpawnZoneMax - new Vector3(margin, 0, margin);
        return RandomPointInBox(min, max);
    }

}