using UnityEngine;
using System.Linq;

/// <summary>
/// Phase 5: Multi-display camera system.
/// Manages 6 separate camera views for comprehensive mission monitoring.
/// Each camera renders to a different portion of screen or RenderTexture.
/// </summary>
public class MultiDisplayManager : MonoBehaviour
{
    [Header("Display Cameras - Auto-created if null")]
    public Camera interceptorViewCamera;      // Display 1
    public Camera intruderViewCamera;         // Display 2
    public Camera worldOverviewCamera;        // Display 3
    public Camera swarmViewCamera;            // Display 4
    public Camera sensorVisualizerCamera;     // Display 5
    public Camera timelineCamera;             // Display 6

    [Header("Camera Targets")]
    public Transform protectedCore;
    public float coreOrbitRadius = 300f;
    public float coreOrbitSpeed = 5f;

    [Header("Display Layout (Screen Space)")]
    public bool useSingleScreen = true;
    [Range(0.1f, 1f)] public float displayScale = 0.33f;


    [Header("References")]
    public DroneManager droneManager;

    private float orbitAngle = 0f;

    void Start()
    {
        if (droneManager == null) droneManager = DroneManager.Instance;
        if (protectedCore == null && droneManager != null) 
            protectedCore = droneManager.ProtectedCore;

        SetupDisplayCameras();
        ConfigureDisplayLayout();
    }

    void Update()
    {
        UpdateDynamicCameras();
    }

    void LateUpdate()
    {
        // Ensure cameras are properly positioned after all updates
        if (worldOverviewCamera != null && protectedCore != null)
        {
            worldOverviewCamera.transform.position = protectedCore.position + new Vector3(0, 500f, -200f);
            worldOverviewCamera.transform.LookAt(protectedCore.position);
        }
    }

    // ============================================
    // CAMERA SETUP
    // ============================================
    void SetupDisplayCameras()
    {
        // Display 1: Interceptor View (follows interceptors)
        if (interceptorViewCamera == null)
        {
            GameObject camObj = new GameObject("Display1_InterceptorView");
            interceptorViewCamera = camObj.AddComponent<Camera>();
            interceptorViewCamera.depth = 1;
        }
        ConfigureCamera(interceptorViewCamera, "Interceptor View");

        // Display 2: Intruder View (follows intruders)
        if (intruderViewCamera == null)
        {
            GameObject camObj = new GameObject("Display2_IntruderView");
            intruderViewCamera = camObj.AddComponent<Camera>();
            intruderViewCamera.depth = 2;
        }
        ConfigureCamera(intruderViewCamera, "Intruder View");

        // Display 3: World Overview (top-down)
        if (worldOverviewCamera == null)
        {
            GameObject camObj = new GameObject("Display3_WorldOverview");
            worldOverviewCamera = camObj.AddComponent<Camera>();
            worldOverviewCamera.depth = 3;
        }
        ConfigureCamera(worldOverviewCamera, "World Overview");
        SetupWorldOverviewCamera();

        // Display 4: Swarm View (tactical view)
        if (swarmViewCamera == null)
        {
            GameObject camObj = new GameObject("Display4_SwarmView");
            swarmViewCamera = camObj.AddComponent<Camera>();
            swarmViewCamera.depth = 4;
        }
        ConfigureCamera(swarmViewCamera, "Swarm View");

        // Display 5: Sensor Visualizer (radar/lidar view)
        if (sensorVisualizerCamera == null)
        {
            GameObject camObj = new GameObject("Display5_SensorView");
            sensorVisualizerCamera = camObj.AddComponent<Camera>();
            sensorVisualizerCamera.depth = 5;
        }
        ConfigureCamera(sensorVisualizerCamera, "Sensor Visualizer");

        // Display 6: Timeline View (UI camera)
        if (timelineCamera == null)
        {
            GameObject camObj = new GameObject("Display6_TimelineView");
            timelineCamera = camObj.AddComponent<Camera>();
            timelineCamera.depth = 6;
            timelineCamera.clearFlags = CameraClearFlags.Depth;
        }
        ConfigureCamera(timelineCamera, "Timeline View");
    }

    void ConfigureCamera(Camera cam, string displayName)
    {
        if (cam == null) return;

        cam.name = displayName;
        
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    }

    void SetupWorldOverviewCamera()
    {
        if (worldOverviewCamera == null || protectedCore == null) return;

        // Position above core looking down
        worldOverviewCamera.transform.position = protectedCore.position + new Vector3(0, 500f, -200f);
        worldOverviewCamera.transform.LookAt(protectedCore.position);
        worldOverviewCamera.orthographic = true;
        worldOverviewCamera.orthographicSize = 600f;
    }

    // ============================================
    // DISPLAY LAYOUT
    // ============================================
    void ConfigureDisplayLayout()
    {
        if (!useSingleScreen) return;

        // 2x3 grid layout on single screen
        float w = displayScale;
        float h = displayScale;

        // Row 1
        SetCameraViewport(interceptorViewCamera, 0f, 0.66f, w, h);      // Top-left
        SetCameraViewport(intruderViewCamera, 0.33f, 0.66f, w, h);      // Top-middle
        SetCameraViewport(worldOverviewCamera, 0.66f, 0.66f, w, h);     // Top-right

        // Row 2
        SetCameraViewport(swarmViewCamera, 0f, 0.33f, w, h);            // Middle-left
        SetCameraViewport(sensorVisualizerCamera, 0.33f, 0.33f, w, h);  // Middle-middle
        SetCameraViewport(timelineCamera, 0.66f, 0.33f, w, h);          // Middle-right
    }

    void SetCameraViewport(Camera cam, float x, float y, float w, float h)
    {
        if (cam == null) return;
        cam.rect = new Rect(x, y, w, h);
    }

    // ============================================
    // DYNAMIC CAMERA UPDATES
    // ============================================
    void UpdateDynamicCameras()
    {
        UpdateInterceptorCamera();
        UpdateIntruderCamera();
        UpdateSwarmCamera();
        UpdateSensorCamera();
    }

    void UpdateInterceptorCamera()
    {
        if (interceptorViewCamera == null || droneManager == null) return;

        var interceptors = droneManager.GetInterceptors();
        if (interceptors == null || interceptors.Count == 0) return;

        // Find active interceptor (assigned to target)
        DroneController activeInterceptor = null;
        foreach (var ic in interceptors)
        {
            if (ic != null && ic.IsAssigned())
            {
                activeInterceptor = ic;
                break;
            }
        }

        // Fallback to first interceptor
        if (activeInterceptor == null)
            activeInterceptor = interceptors.FirstOrDefault(i => i != null);

        if (activeInterceptor != null)
        {
            // Follow camera behind interceptor
            Vector3 offset = -activeInterceptor.transform.forward * 40f + Vector3.up * 15f;
            Vector3 targetPos = activeInterceptor.transform.position + offset;
            
            interceptorViewCamera.transform.position = Vector3.Lerp(
                interceptorViewCamera.transform.position, 
                targetPos, 
                Time.deltaTime * 3f
            );
            
            interceptorViewCamera.transform.LookAt(activeInterceptor.transform.position);
        }
    }

    void UpdateIntruderCamera()
    {
        if (intruderViewCamera == null) return;

        var intruders = FindObjectsOfType<IntruderMeta>();
        if (intruders.Length == 0) return;

        // Follow closest intruder to core
        IntruderMeta closest = null;
        float minDist = float.MaxValue;

        foreach (var intr in intruders)
        {
            if (intr == null || protectedCore == null) continue;
            float d = Vector3.Distance(intr.transform.position, protectedCore.position);
            if (d < minDist)
            {
                minDist = d;
                closest = intr;
            }
        }

        if (closest != null)
        {
            // Follow camera behind intruder
            Vector3 offset = -closest.transform.forward * 50f + Vector3.up * 20f;
            Vector3 targetPos = closest.transform.position + offset;
            
            intruderViewCamera.transform.position = Vector3.Lerp(
                intruderViewCamera.transform.position,
                targetPos,
                Time.deltaTime * 2f
            );
            
            intruderViewCamera.transform.LookAt(closest.transform.position);
        }
    }

    void UpdateSwarmCamera()
    {
        if (swarmViewCamera == null || protectedCore == null) return;

        // Orbit camera around protected core
        orbitAngle += coreOrbitSpeed * Time.deltaTime;
        float x = Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * coreOrbitRadius;
        float z = Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * coreOrbitRadius;
        
        Vector3 targetPos = protectedCore.position + new Vector3(x, 150f, z);
        swarmViewCamera.transform.position = targetPos;
        swarmViewCamera.transform.LookAt(protectedCore.position);
    }

    void UpdateSensorCamera()
    {
        if (sensorVisualizerCamera == null || protectedCore == null) return;

        // Top-down view centered on core
        sensorVisualizerCamera.transform.position = protectedCore.position + new Vector3(0, 400f, 0);
        sensorVisualizerCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        sensorVisualizerCamera.orthographic = true;
        sensorVisualizerCamera.orthographicSize = 500f;
    }

    // ============================================
    // PUBLIC API
    // ============================================
    public void FocusOnIntruder(Transform intruder)
    {
        if (intruderViewCamera != null && intruder != null)
        {
            intruderViewCamera.transform.position = intruder.position + Vector3.back * 50f + Vector3.up * 20f;
            intruderViewCamera.transform.LookAt(intruder);
        }
    }

    public void FocusOnInterceptor(Transform interceptor)
    {
        if (interceptorViewCamera != null && interceptor != null)
        {
            interceptorViewCamera.transform.position = interceptor.position + Vector3.back * 40f + Vector3.up * 15f;
            interceptorViewCamera.transform.LookAt(interceptor);
        }
    }

    public void ToggleDisplay(int displayIndex, bool enabled)
    {
        Camera cam = GetDisplayCamera(displayIndex);
        if (cam != null) cam.enabled = enabled;
    }

    Camera GetDisplayCamera(int index)
    {
        switch (index)
        {
            case 1: return interceptorViewCamera;
            case 2: return intruderViewCamera;
            case 3: return worldOverviewCamera;
            case 4: return swarmViewCamera;
            case 5: return sensorVisualizerCamera;
            case 6: return timelineCamera;
            default: return null;
        }
    }
}