// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;

// /// <summary>
// /// MissionOrchestrator: spawns waves sequentially. Next wave spawns only when
// /// all intruders from the previous wave are gone. Small inter-wave gap = 0.1s.
// /// Does NOT freeze timeScale on mission end — just stops mission and triggers telemetry.
// /// </summary>
// public class MissionOrchestrator : MonoBehaviour
// {
//     public static MissionOrchestrator Instance;

//     [Header("Managers")]
//     public DroneManager droneManager;
//     public SwarmCoordinator swarm;
//     public MissionTelemetry telemetry;

//     [Header("Wave Settings")]
//     public int initialWave = 4;        // base number per wave
//     public int maxWaves = 5;          // total waves to run
//     public float timeBetweenWaves = 0.1f; // NOTE: minimal gap (0.1s) as requested

//     [Header("Intruder Settings")]
//     public GameObject intruderPrefab;
//     public float spawnRadius = 900f;
//     [Range(0f, 1f)] public float hypersonicChance = 0.25f;

//     [Header("Mission Runtime")]
//     public bool autoStart = true;
//     public bool missionRunning = false;

//     int currentWave = 0;

//     void Awake()
//     {
//         Instance = this;

//         if (!droneManager) droneManager = FindObjectOfType<DroneManager>();
//         if (!swarm) swarm = FindObjectOfType<SwarmCoordinator>();
//         if (!telemetry) telemetry = FindObjectOfType<MissionTelemetry>();
//     }

//     void Start()
//     {
//         if (autoStart) StartMission();
//     }

//     public void StartMission()
//     {
//         if (missionRunning) return;

//         missionRunning = true;
//         currentWave = 0;

//         telemetry?.BeginMission();
//         StartCoroutine(WaveRoutine());
//     }

//     public void StopMission()
//     {
//         if (!missionRunning) return;
//         missionRunning = false;
//         telemetry?.EndMission();
//         // do not set Time.timeScale here — keep editor/play responsive
//     }

//     public void EndMission(bool success)
//     {
//         StopMission();   // reuse existing stop logic
//         MissionTelemetry.Instance?.EndMission(success);
//     }

//     IEnumerator WaveRoutine()
//     {
//         while (missionRunning && currentWave < maxWaves)
//         {
//             currentWave++;
//             int spawnCount = initialWave + (currentWave - 1); // growth per wave optional
//             SpawnWave(spawnCount);

//             telemetry?.RegisterWave(spawnCount);

//             // Wait until *all* intruders spawned in this wave are neutralized/destroyed
//             // Use DroneManager.GetActiveIntruderCount which tracks current living intruders.
//             // Wait in a tight loop but yield each frame — no freeze.
//             while (missionRunning && droneManager != null && droneManager.GetActiveIntruderCount() > 0)
//             {
//                 yield return null;
//             }

//             // short gap before next wave (as requested)
//             yield return new WaitForSeconds(timeBetweenWaves);
//         }

//         // end of waves (either ran out of waves or missionRunning turned false)
//         StopMission();
//     }

//     // spawn helpers
//     void SpawnWave(int count)
//     {
//         for (int i = 0; i < count; i++)
//             SpawnOneIntruder();
//     }

//     void SpawnOneIntruder()
//     {
//         if (intruderPrefab == null || droneManager == null) return;

//         // spawn at a ring around the protected core so intruders start outside interceptor zone
//         Vector3 core = droneManager.ProtectedCore != null ? droneManager.ProtectedCore.position : Vector3.zero;
//         Vector3 dir = Random.onUnitSphere;
//         dir.y = 0;
//         Vector3 pos = core + dir.normalized * spawnRadius;

//         GameObject intr = Instantiate(intruderPrefab, pos, Quaternion.identity);

//         // inject hypersonic behavior (IntruderMeta.SetHypersonic must exist)
//         var meta = intr.GetComponent<IntruderMeta>();
//         if (meta != null)
//         {
//             bool isHyper = Random.value < hypersonicChance;
//             meta.SetHypersonic(isHyper);
//             // Register spawn via telemetry (safe if telemetry null)
//             MissionTelemetry.Instance?.RegisterSpawn(meta);
//         }
//     }

//     // Called when core breached externally
//     public void OnCoreBreached()
//     {
//         if (!missionRunning) return;
//         telemetry?.CoreBreached();
//         StopMission();
//     }
// }


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MissionOrchestrator : MonoBehaviour
{
    public static MissionOrchestrator Instance;

    [Header("Managers")]
    public DroneManager droneManager;
    public SwarmCoordinator swarm;
    public MissionTelemetry telemetry;

    [Header("Wave Settings")]
    public int initialWave = 4;
    public int maxWaves = 5;
    public float timeBetweenWaves = 0.1f; // you asked 0.1s between waves

    [Header("Intruder Settings")]
    public GameObject intruderPrefab;
    public float spawnRadius = 900f;
    [Range(0f, 1f)] public float hypersonicChance = 0.25f;

    [Header("Mission Runtime")]
    public bool autoStart = true;
    public bool missionRunning = false;

    int currentWave = 0;
    int intrudersThisWave = 0;

    void Awake()
    {
        Instance = this;

        if (!droneManager) droneManager = FindObjectOfType<DroneManager>();
        if (!swarm) swarm = FindObjectOfType<SwarmCoordinator>();
        if (!telemetry) telemetry = FindObjectOfType<MissionTelemetry>();
    }

    void Start()
    {
        if (autoStart) StartMission();
    }

    public void StartMission()
    {
        if (missionRunning) return;
        missionRunning = true;
        currentWave = 0;
        telemetry?.BeginMission();
        StartCoroutine(WaveRoutine());
    }

    public void StopMission()
    {
        missionRunning = false;
        telemetry?.EndMission();
    }

    // NEW: Overload used by DroneManager on mission end
    public void EndMission(bool success)
    {
        if (!missionRunning)
        {
            // still call telemetry if needed
            MissionTelemetry.Instance?.SetMissionStatus(success ? "Success" : "Failure");
            MissionTelemetry.Instance?.EndMission();
            return;
        }

        missionRunning = false;
        // notify telemetry
        MissionTelemetry.Instance?.SetMissionStatus(success ? "Success" : "Failure");
        MissionTelemetry.Instance?.EndMission();

        // other cleanup
        StopAllCoroutines();
    }

    IEnumerator WaveRoutine()
    {
        while (missionRunning && currentWave < maxWaves)
        {
            currentWave++;
            int spawnCount = initialWave + currentWave - 1;
            intrudersThisWave = spawnCount;

            SpawnWave(spawnCount);

            telemetry?.RegisterWave(spawnCount);

            // wait until intruders of current wave are cleared
            while (missionRunning && (droneManager != null && droneManager.GetActiveIntruderCount() > 0))
            {
                yield return null;
            }

            // small gap between waves
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        // end mission if still running
        if (missionRunning)
        {
            StopMission();
        }
    }

    void SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
            SpawnOneIntruder();
    }

    void SpawnOneIntruder()
    {
        Vector3 dir = Random.onUnitSphere;
        dir.y = 0;
        Vector3 pos = droneManager != null ? droneManager.ProtectedCore.position + dir.normalized * spawnRadius : dir.normalized * spawnRadius;
        GameObject intr = Instantiate(intruderPrefab, pos, Quaternion.identity);

        bool isHyper = Random.value < hypersonicChance;
        var meta = intr.GetComponent<IntruderMeta>();
        if (meta != null)
            meta.SetHypersonic(isHyper);

        // register spawn with telemetry with actual IntruderMeta if available
        MissionTelemetry.Instance?.RegisterSpawn(meta);
    }

    public void OnCoreBreached()
    {
        if (!missionRunning) return;
        telemetry?.CoreBreached();
        EndMission(false);
    }
}
