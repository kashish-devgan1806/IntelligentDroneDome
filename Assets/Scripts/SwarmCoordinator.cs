// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// [DefaultExecutionOrder(100)]
// public class SwarmCoordinator : MonoBehaviour
// {
//     public DroneManager droneManager;
//     public ThreatPredictor threatPredictor;
//     public SensorFusion sensorFusion;

//     [Header("Squad / assignment")]
//     public int squadSize = 4;
//     public float assignmentInterval = 0.5f;
//     public int maxActiveSquads = 6;

//     [Header("Spacing & repulsion")]
//     public float minSpacing = 6f;
//     public float repulsionStrength = 3.5f;
//     public float avoidFenceDistance = 8f;

//     [Header("Prediction (lead point)")]
//     public float predictionLeadSeconds = 0.6f;

//     [Header("Debug")]
//     public bool debugDraw = true;

//     // runtime
//     float lastAssignTime;
//     List<DroneController> interceptors = new();
//     List<IntruderMeta> intruders = new();
//     List<FusedTrack> tracks = new();
//     Dictionary<int, List<DroneController>> squads = new();
//     int nextSquadId = 1;

//     // NEW: Internal velocity estimation for intruders
//     private Dictionary<int, Vector3> lastPos = new();
//     private Dictionary<int, Vector3> estVel = new();

//     void Awake()
//     {
//         if (droneManager == null && DroneManager.Instance != null)
//             droneManager = DroneManager.Instance;

//         if (threatPredictor == null)
//             threatPredictor = FindObjectOfType<ThreatPredictor>();

//         if (sensorFusion == null)
//             sensorFusion = FindObjectOfType<SensorFusion>();
//     }

//     void Start()
//     {
//         RefreshInterceptorList();
//     }

//     void Update()
//     {
//         if (droneManager == null) return;

//         TrackIntruderVelocities();

//         if (Time.time - lastAssignTime >= assignmentInterval)
//         {
//             lastAssignTime = Time.time;
//             RefreshInterceptorList();
//             RefreshIntruderList();
//             AssignSquadsToIntruders();
//         }

//         ApplyLocalRepulsion();

//         if (debugDraw) DrawDebug();
        
//         if (DroneManager.Instance != null && DroneManager.Instance.SimulationEnded)
//             return;

//         CoordinateAssignments();
//     }

//     // ------------------------------------------------------
// //   PHASE-3 BACKWARD COMPATIBILITY PATCH
// // ------------------------------------------------------
//     private List<DroneController> activeInterceptors = new List<DroneController>();

//     private void CoordinateAssignments()
//     {
//         if (droneManager == null) return;

//         activeInterceptors.Clear();
//         activeInterceptors.AddRange(droneManager.GetAllInterceptors());

//         // Restore Phase-3 assignment logic
//         var intruders = fusion.GetDetectedIntruders();
//         if (intruders == null || intruders.Count == 0) return;

//         foreach (var intr in intruders)
//         {
//             var interceptor = droneManager.GetClosestAvailableInterceptor(intr.transform.position);
//             if (interceptor != null)
//             {
//                 interceptor.AssignTarget(intr.gameObject);
//             }
//         }
//     }


//     // -----------------------------------------
//     void TrackIntruderVelocities()
//     {
//         foreach (var intr in FindObjectsOfType<IntruderMeta>())
//         {
//             int id = intr.GetInstanceID();
//             Vector3 pos = intr.transform.position;

//             if (lastPos.ContainsKey(id))
//             {
//                 Vector3 vel = (pos - lastPos[id]) / Mathf.Max(Time.deltaTime, 0.015f);
//                 estVel[id] = vel;
//             }
//             else estVel[id] = Vector3.zero;

//             lastPos[id] = pos;
//         }
//     }

//     Vector3 GetVelocityFor(IntruderMeta intr)
//     {
//         int id = intr.GetInstanceID();
//         return estVel.ContainsKey(id) ? estVel[id] : Vector3.zero;
//     }

//     // -----------------------------------------
//     void RefreshInterceptorList()
//     {
//         interceptors = droneManager.GetInterceptors()?.Where(d => d != null).ToList() ?? new List<DroneController>();
//     }

//     void RefreshIntruderList()
//     {
//         tracks = sensorFusion != null ? sensorFusion.GetFusedTracks() : new List<FusedTrack>();

//         // map tracks -> intruder metas (by instanceID)
//         intruders = tracks.Select(t => FindIntruderById(t.id)).Where(i => i != null).ToList();
//     }

//     // -----------------------------------------
//     void AssignSquadsToIntruders()
//     {
//         var intrList = intruders.OrderBy(i => Vector3.Distance(i.transform.position, droneManager.ProtectedCore.position)).ToList();

//         foreach (var intr in intrList)
//         {
//             if (intr == null) continue;
//             if (IsIntruderAlreadyTargeted(intr)) continue;

//             var squad = GetOrCreateFreeSquad();
//             if (squad == null) continue;

//             Vector3 predicted = sensorFusion != null ? sensorFusion.GetPredictedFuturePosition(intr, predictionLeadSeconds) : intr.transform.position;

//             bool assignedAny = false;

//             foreach (var dc in squad)
//             {
//                 if (dc == null) continue;
//                 if (dc.IsAssigned()) continue;

//                 bool ok = dc.AssignTarget(intr.transform, OnInterceptorFreed);
//                 if (ok) assignedAny = true;
//             }
//         }
//     }

//     bool IsIntruderAlreadyTargeted(IntruderMeta intr)
//     {
//         foreach (var ic in interceptors)
//         {
//             if (ic == null) continue;
//             if (!ic.IsAssigned()) continue;

//             float d = Vector3.Distance(ic.transform.position, intr.transform.position);
//             if (d < 120f) return true;
//         }
//         return false;
//     }

//     List<DroneController> GetOrCreateFreeSquad()
//     {
//         foreach (var kv in squads)
//         {
//             int free = kv.Value.Count(d => d != null && !d.IsAssigned());
//             if (free >= 1) return kv.Value;
//         }

//         if (squads.Count >= maxActiveSquads) return null;

//         var freeDrones = interceptors.Where(d => d != null && !d.IsAssigned()).OrderBy(d => Vector3.Distance(d.transform.position, droneManager.ProtectedCore.position)).Take(squadSize).ToList();

//         if (freeDrones.Count == 0) return null;

//         int id = nextSquadId++;
//         squads[id] = new List<DroneController>(freeDrones);
//         return squads[id];
//     }

//     // ------------------------------------------------------------
//     Vector3 PredictInterceptPoint(IntruderMeta intr, float leadSeconds)
//     {
//         if (sensorFusion != null) return sensorFusion.GetPredictedFuturePosition(intr, leadSeconds);
//         // fallback: estimate
//         return intr.transform.position + GetVelocityFor(intr) * leadSeconds;
//     }

//     // ------------------------------------------------------------
//     void ApplyLocalRepulsion()
//     {
//         var min = droneManager.interceptorZoneMin;
//         var max = droneManager.interceptorZoneMax;

//         for (int i = 0; i < interceptors.Count; i++)
//         {
//             var a = interceptors[i];
//             if (a == null) continue;

//             Vector3 force = Vector3.zero;

//             // repulsion between drones
//             for (int j = 0; j < interceptors.Count; j++)
//             {
//                 if (i == j) continue;
//                 var b = interceptors[j];
//                 if (b == null) continue;

//                 float dist = Vector3.Distance(a.transform.position, b.transform.position);
//                 if (dist < minSpacing)
//                 {
//                     Vector3 away = (a.transform.position - b.transform.position).normalized;
//                     force += away * repulsionStrength * Time.deltaTime;
//                 }
//             }

//             // fence avoidance
//             float distFence = max.x - a.transform.position.x;
//             if (distFence < avoidFenceDistance)
//             {
//                 force += Vector3.left * repulsionStrength * Time.deltaTime;
//             }

//             if (!a.IsAssigned())
//             {
//                 a.transform.position += force;
//                 Vector3 c = a.transform.position;
//                 c.x = Mathf.Clamp(c.x, min.x, max.x);
//                 c.z = Mathf.Clamp(c.z, min.z, max.z);
//                 a.transform.position = c;
//             }
//         }
//     }

//     void OnInterceptorFreed(DroneController ic)
//     {
//         var keys = squads.Keys.ToList();
//         foreach (var k in keys)
//         {
//             squads[k].RemoveAll(d => d == null);
//             if (squads[k].Count == 0) squads.Remove(k);
//         }
//     }

//     public int GetActiveSquadCount()
//     {
//         return squads.Count;
//     }

//     public Vector3 DebugGetIntruderVelocity(IntruderMeta intr)
//     {
//         int id = intr.GetInstanceID();
//         if (estVel.ContainsKey(id)) return estVel[id];
//         return Vector3.zero;
//     }

//     // helper
//     IntruderMeta FindIntruderById(int id)
//     {
//         foreach (var i in FindObjectsOfType<IntruderMeta>())
//             if (i.GetInstanceID() == id) return i;
//         return null;
//     }

//     void DrawDebug()
//     {
//         if (droneManager == null) return;
//         Vector3 min = droneManager.interceptorZoneMin;
//         Vector3 max = droneManager.interceptorZoneMax;
//         DebugDrawBox((min + max) * 0.5f, max - min, Color.yellow);
//     }

//     void DebugDrawBox(Vector3 center, Vector3 size, Color c)
//     {
//         Vector3 ext = size * 0.5f;
//         Vector3[] pts = new Vector3[8];
//         pts[0] = center + new Vector3(-ext.x, -ext.y, -ext.z);
//         pts[1] = center + new Vector3(ext.x, -ext.y, -ext.z);
//         pts[2] = center + new Vector3(ext.x, -ext.y, ext.z);
//         pts[3] = center + new Vector3(-ext.x, -ext.y, ext.z);
//         pts[4] = center + new Vector3(-ext.x, ext.y, -ext.z);
//         pts[5] = center + new Vector3(ext.x, ext.y, -ext.z);
//         pts[6] = center + new Vector3(ext.x, ext.y, ext.z);
//         pts[7] = center + new Vector3(-ext.x, ext.y, ext.z);
//         for (int i = 0; i < 4; i++)
//         {
//             Debug.DrawLine(pts[i], pts[(i + 1) % 4], c);
//             Debug.DrawLine(pts[i + 4], pts[((i + 1) % 4) + 4], c);
//             Debug.DrawLine(pts[i], pts[i + 4], c);
//         }
//     }
// }


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class SwarmCoordinator : MonoBehaviour
{
    public DroneManager droneManager;
    public ThreatPredictor threatPredictor;
    public SensorFusion sensorFusion;

    [Header("Squad / assignment")]
    public int squadSize = 4;
    public float assignmentInterval = 0.5f;
    public int maxActiveSquads = 6;

    [Header("Spacing & repulsion")]
    public float minSpacing = 6f;
    public float repulsionStrength = 3.5f;
    public float avoidFenceDistance = 8f;

    [Header("Prediction (lead point)")]
    public float predictionLeadSeconds = 0.6f;

    [Header("Debug")]
    public bool debugDraw = true;

    // runtime
    float lastAssignTime;
    List<DroneController> interceptors = new();
    List<FusedTrack> tracks = new();
    Dictionary<int, List<DroneController>> squads = new();
    int nextSquadId = 1;

    // internal velocity estimation map (kept for UI/debug)
    private Dictionary<int, Vector3> estVel = new();

    void Awake()
    {
        if (droneManager == null && DroneManager.Instance != null)
            droneManager = DroneManager.Instance;

        if (threatPredictor == null)
            threatPredictor = FindObjectOfType<ThreatPredictor>();

        if (sensorFusion == null)
            sensorFusion = FindObjectOfType<SensorFusion>();
    }

    void Start()
    {
        RefreshInterceptorList();
    }

    void Update()
    {
        if (droneManager == null) return;

        if (Time.time - lastAssignTime >= assignmentInterval)
        {
            lastAssignTime = Time.time;
            RefreshInterceptorList();
            RefreshIntruderTracks();
            AssignSquadsToIntruders();
        }

        ApplyLocalRepulsion();

        if (debugDraw) DrawDebug();
    }

    void RefreshInterceptorList()
    {
        interceptors = droneManager.GetInterceptors()?.Where(d => d != null).ToList() ?? new List<DroneController>();
    }

    void RefreshIntruderTracks()
    {
        // request fused tracks from SensorFusion
        tracks = sensorFusion != null ? sensorFusion.GetFusedTracks() : new List<FusedTrack>();

        // update estVel map for debug
        estVel.Clear();
        foreach (var t in tracks)
        {
            estVel[t.id] = t.velocity;
        }
    }

    void AssignSquadsToIntruders()
    {
        if (tracks == null || tracks.Count == 0) return;

        // convert fused tracks to priority list by distance to core
        var ordered = tracks.OrderBy(t =>
        {
            var core = droneManager != null ? droneManager.ProtectedCore.position : Vector3.zero;
            return Vector3.Distance(t.worldPosition, core);
        }).ToList();

        foreach (var track in ordered)
        {
            // attempt to find an available intruder GameObject to assign (by ID)
            IntruderMeta intr = FindIntruderById(track.id);
            if (intr == null) continue; // not present physically

            if (IsIntruderAlreadyTargeted(intr)) continue;

            var squad = GetOrCreateFreeSquad();
            if (squad == null) continue;

            Vector3 predicted = sensorFusion != null ? sensorFusion.GetPredictedFuturePosition(intr, predictionLeadSeconds) : intr.transform.position;

            foreach (var dc in squad)
            {
                if (dc == null) continue;
                if (dc.IsAssigned()) continue;

                // assign the intruder transform — DroneController will chase
                dc.AssignTarget(intr.transform, OnInterceptorFreed);
            }
        }
    }

    bool IsIntruderAlreadyTargeted(IntruderMeta intr)
    {
        foreach (var ic in interceptors)
        {
            if (ic == null) continue;
            if (!ic.IsAssigned()) continue;

            if (Vector3.Distance(ic.transform.position, intr.transform.position) < 120f)
                return true;
        }
        return false;
    }

    List<DroneController> GetOrCreateFreeSquad()
    {
        // try existing squads first
        foreach (var kv in squads)
        {
            int free = kv.Value.Count(d => d != null && !d.IsAssigned());
            if (free >= 1) return kv.Value;
        }

        if (squads.Count >= maxActiveSquads)
            return null;

        var freeDrones = interceptors
            .Where(d => d != null && !d.IsAssigned())
            .OrderBy(d => Vector3.Distance(d.transform.position, droneManager.ProtectedCore.position))
            .Take(squadSize)
            .ToList();

        if (freeDrones.Count == 0) return null;

        int id = nextSquadId++;
        squads[id] = new List<DroneController>(freeDrones);
        return squads[id];
    }

    // repulsion forces to avoid merging/stacking near fence
    void ApplyLocalRepulsion()
    {
        if (droneManager == null) return;
        var min = droneManager.interceptorZoneMin;
        var max = droneManager.interceptorZoneMax;

        for (int i = 0; i < interceptors.Count; i++)
        {
            var a = interceptors[i];
            if (a == null) continue;

            Vector3 force = Vector3.zero;

            // neighbor repulsion
            for (int j = 0; j < interceptors.Count; j++)
            {
                if (i == j) continue;
                var b = interceptors[j];
                if (b == null) continue;

                float dist = Vector3.Distance(a.transform.position, b.transform.position);
                if (dist < minSpacing && dist > 0.001f)
                {
                    Vector3 away = (a.transform.position - b.transform.position).normalized;
                    force += away * repulsionStrength * Time.deltaTime;
                }
            }

            // fence avoidance (push inside)
            float distFence = max.x - a.transform.position.x;
            if (distFence < avoidFenceDistance)
                force += Vector3.left * repulsionStrength * Time.deltaTime;

            if (!a.IsAssigned())
            {
                a.transform.position += force;
                // clamp inside interceptor zone
                Vector3 c = a.transform.position;
                c.x = Mathf.Clamp(c.x, min.x, max.x);
                c.z = Mathf.Clamp(c.z, min.z, max.z);
                a.transform.position = c;
            }
        }
    }

    void OnInterceptorFreed(DroneController ic)
    {
        var keys = squads.Keys.ToList();
        foreach (var k in keys)
        {
            squads[k].RemoveAll(d => d == null);
            if (squads[k].Count == 0)
                squads.Remove(k);
        }
    }

    public int GetActiveSquadCount() => squads.Count;

    public Vector3 DebugGetIntruderVelocityById(int id)
    {
        return estVel.ContainsKey(id) ? estVel[id] : Vector3.zero;
    }

    IntruderMeta FindIntruderById(int id)
    {
        foreach (var i in FindObjectsOfType<IntruderMeta>())
        {
            if (i.GetInstanceID() == id) return i;
        }
        return null;
    }

    void DrawDebug()
    {
        if (droneManager == null) return;
        Vector3 min = droneManager.interceptorZoneMin;
        Vector3 max = droneManager.interceptorZoneMax;
        DebugDrawBox((min + max) * 0.5f, max - min, Color.yellow);
    }

    void DebugDrawBox(Vector3 center, Vector3 size, Color c)
    {
        Vector3 ext = size * 0.5f;
        Vector3[] pts = new Vector3[8];
        pts[0] = center + new Vector3(-ext.x, -ext.y, -ext.z);
        pts[1] = center + new Vector3(ext.x, -ext.y, -ext.z);
        pts[2] = center + new Vector3(ext.x, -ext.y, ext.z);
        pts[3] = center + new Vector3(-ext.x, -ext.y, ext.z);
        pts[4] = center + new Vector3(-ext.x, ext.y, -ext.z);
        pts[5] = center + new Vector3(ext.x, ext.y, -ext.z);
        pts[6] = center + new Vector3(ext.x, ext.y, ext.z);
        pts[7] = center + new Vector3(-ext.x, ext.y, ext.z);

        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(pts[i], pts[(i + 1) % 4], c);
            Debug.DrawLine(pts[i + 4], pts[((i + 1) % 4) + 4], c);
            Debug.DrawLine(pts[i], pts[i + 4], c);
        }
    }
}
