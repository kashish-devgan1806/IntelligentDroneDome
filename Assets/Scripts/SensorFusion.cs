// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// Phase-3 Sensor Fusion: Radar + Lidar + Noise + fused output.
// /// Exposes fused tracks and convenience methods used by SwarmCoordinator.
// /// </summary>
// public class SensorFusion : MonoBehaviour
// {
//     [Header("Radar Settings")]
//     public float radarRange = 800f;
//     public float radarConeAngle = 55f;

//     [Header("Lidar Settings")]
//     public float lidarRange = 350f;
//     public int lidarPoints = 32;

//     [Header("Noise")]
//     public float positionNoise = 0.35f;
//     public float velocityNoise = 0.20f;

//     [Header("Debug")]
//     public bool showDebug = false;

//     // internal lists
//     private List<FusedTrack> fused = new List<FusedTrack>();
//     private List<IntruderMeta> detectedIntruders = new List<IntruderMeta>();

//     void Update()
//     {
//         if (DroneManager.Instance != null && DroneManager.Instance.SimulationEnded)
//                 return;

//         ScanEnvironment();  
//     }

//     void ScanEnvironment()
//     {
//         fused.Clear();
//         detectedIntruders.Clear();

//         // simple sampling: iterate all intruder metas and filter by range
//         foreach (var intr in FindObjectsOfType<IntruderMeta>())
//         {
//             if (intr == null) continue;

//             Vector3 pos = intr.transform.position;
//             Vector3 vel = intr.GetEstimatedVelocity(); // existing API on IntruderMeta

//             // range check from this sensor rig
//             if (Vector3.Distance(transform.position, pos) > radarRange)
//                 continue;

//             // add noise
//             pos += Random.insideUnitSphere * positionNoise;
//             vel += Random.insideUnitSphere * velocityNoise;

//             // add detection
//             detectedIntruders.Add(intr);

//             // fused track (confidence currently 1, could be improved)
//             fused.Add(new FusedTrack(intr.GetInstanceID(), pos, vel, 1f));
//         }
//     }

//     // API used by SwarmCoordinator / UI
//     public List<FusedTrack> GetFusedTracks()
//     {
//         return new List<FusedTrack>(fused);
//     }

//     public List<IntruderMeta> GetDetectedIntruders()
//     {
//         return new List<IntruderMeta>(detectedIntruders);
//     }

//     public Vector3 GetVelocity(IntruderMeta intr)
//     {
//         if (intr == null) return Vector3.zero;
//         int id = intr.GetInstanceID();
//         foreach (var ft in fused) if (ft.id == id) return ft.velocity;
//         return intr.GetEstimatedVelocity();
//     }

//     public ThreatInfo GetThreatInfo(IntruderMeta intr)
//     {
//         ThreatInfo t = new ThreatInfo();
//         if (intr == null) return t;

//         int id = intr.GetInstanceID();
//         foreach (var ft in fused)
//         {
//             if (ft.id == id)
//             {
//                 t.id = ft.id;
//                 t.position = ft.worldPosition;
//                 t.velocity = ft.velocity;
//                 t.confidence = ft.confidence;
//                 // also fill predictor fields using ThreatPredictor
//                 var predictor = FindObjectOfType<ThreatPredictor>();
//                 if (predictor != null)
//                 {
//                     var info = predictor.EvaluateThreat(ft.worldPosition, ft.velocity, DroneManager.Instance != null && DroneManager.Instance.ProtectedCore != null ? DroneManager.Instance.ProtectedCore.position : Vector3.zero);
//                     t.speed = info.speed;
//                     t.threatLevel = info.threatLevel;
//                     t.timeToCore = info.timeToCore;
//                     t.distanceToCore = info.distanceToCore;
//                     t.predictedPosition = info.predictedPosition;
//                     t.leadTimeUsed = info.leadTimeUsed;
//                 }
//                 return t;
//             }
//         }

//         // fallback: estimate using intr transform
//         var pred = FindObjectOfType<ThreatPredictor>();
//         if (pred != null)
//         {
//             var info = pred.EvaluateThreatFor(intr.transform, intr.GetEstimatedVelocity());
//             t.id = id;
//             t.position = info.predictedPosition;
//             t.velocity = info.velocity;
//             t.confidence = 0.5f;
//             t.speed = info.speed;
//             t.threatLevel = info.threatLevel;
//             t.predictedPosition = info.predictedPosition;
//             t.leadTimeUsed = info.leadTimeUsed;
//         }

//         return t;
//     }

//     public Vector3 GetPredictedFuturePosition(IntruderMeta intr, float seconds = 0.6f)
//     {
//         if (intr == null) return Vector3.zero;
//         int id = intr.GetInstanceID();
//         foreach (var ft in fused)
//         {
//             if (ft.id == id)
//                 return ft.worldPosition + ft.velocity * seconds;
//         }
//         // fallback: use current pos
//         return intr.transform.position + intr.GetEstimatedVelocity() * seconds;
//     }
// }

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase-3 Sensor Fusion: Radar + Lidar + LOS + noise + fused output.
/// Kept intentionally simple: scan all IntruderMeta objects, apply noise,
/// compose fused tracks and expose small API to SwarmCoordinator.
/// </summary>
public class SensorFusion : MonoBehaviour
{
    [Header("Radar Settings")]
    public float radarRange = 800f;
    public float radarConeAngle = 55f;

    [Header("Lidar Settings")]
    public float lidarRange = 350f;
    public int lidarPoints = 32;

    [Header("Noise")]
    public float positionNoise = 0.35f;
    public float velocityNoise = 0.20f;

    [Header("Debug")]
    public bool showDebug = false;

    // internal fused list
    private List<FusedTrack> fused = new List<FusedTrack>();

    // cached intruders
    private List<IntruderMeta> detectedIntruders = new List<IntruderMeta>();

    // PUBLIC API used by SwarmCoordinator / ThreatPredictor
    public List<IntruderMeta> GetDetectedIntruders()
    {
        return new List<IntruderMeta>(detectedIntruders);
    }

    public List<FusedTrack> GetFusedTracks()
    {
        return new List<FusedTrack>(fused);
    }

    public Vector3 GetVelocity(IntruderMeta intr)
    {
        int id = intr.GetInstanceID();
        foreach (var ft in fused)
        {
            if (ft.id == id) return ft.velocity;
        }
        return Vector3.zero;
    }

    public Vector3 GetPredictedFuturePosition(IntruderMeta intr, float seconds = 0.6f)
    {
        int id = intr.GetInstanceID();
        foreach (var ft in fused)
        {
            if (ft.id == id)
                return ft.worldPosition + ft.velocity * seconds;
        }
        // fallback to current transform
        return intr.transform.position;
    }

    void Update()
    {
        ScanEnvironment();
    }

    void ScanEnvironment()
    {
        fused.Clear();
        detectedIntruders.Clear();

        // find all intruders in scene
        foreach (var intr in FindObjectsOfType<IntruderMeta>())
        {
            int id = intr.GetInstanceID();
            Vector3 pos = intr.transform.position;

            // use the correct method on IntruderMeta
            Vector3 vel = intr.GetEstimatedVelocity();

            // add sensor noise (small)
            pos += Random.insideUnitSphere * positionNoise;
            vel += Random.insideUnitSphere * velocityNoise;

            // basic range gating
            if (Vector3.Distance(transform.position, pos) > radarRange)
                continue;

            detectedIntruders.Add(intr);
            fused.Add(new FusedTrack(id, pos, vel, 1.0f));
        }

        // optional debug draw
        if (showDebug)
        {
            foreach (var ft in fused)
            {
                Debug.DrawLine(transform.position, ft.worldPosition, Color.cyan);
            }
        }
    }
}

