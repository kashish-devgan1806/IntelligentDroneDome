using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase 5: Real-time radar/lidar visualization for Display 5.
/// Renders sensor cones, detection points, and tracking confidence.
/// </summary>
public class SensorRadarVisualizer : MonoBehaviour
{
    [Header("References")]
    public SensorFusion sensorFusion;
    public RadarSensor radarSensor;
    public LidarSensor lidarSensor;
    public Transform sensorOrigin;

    [Header("Visualization Settings")]
    public bool showRadarCone = true;
    public bool showLidarRange = true;
    public bool showDetectionPoints = true;
    public bool showTrackingLines = true;
    public bool showConfidenceRings = true;

    [Header("Colors")]
    public Color radarColor = new Color(1f, 1f, 0f, 0.3f);
    public Color lidarColor = new Color(0f, 1f, 1f, 0.3f);
    public Color detectionColor = Color.green;
    public Color trackColor = Color.magenta;
    public Color lowConfidenceColor = Color.red;
    public Color highConfidenceColor = Color.green;

    [Header("Radar Sweep")]
    public bool animateSweep = true;
    public float sweepSpeed = 45f; // degrees per second
    private float currentSweepAngle = 0f;

    [Header("Display Materials")]
    public Material radarConeMaterial;
    public Material lidarSphereMaterial;

    private List<GameObject> visualMarkers = new List<GameObject>();
    private GameObject radarConeObject;
    private GameObject lidarSphereObject;

    void Start()
    {
        if (sensorFusion == null) sensorFusion = FindObjectOfType<SensorFusion>();
        if (radarSensor == null) radarSensor = FindObjectOfType<RadarSensor>();
        if (lidarSensor == null) lidarSensor = FindObjectOfType<LidarSensor>();
        
        if (sensorOrigin == null && radarSensor != null)
            sensorOrigin = radarSensor.transform;

        CreateVisualizationObjects();
    }

    void Update()
    {
        if (animateSweep)
        {
            currentSweepAngle += sweepSpeed * Time.deltaTime;
            if (currentSweepAngle >= 360f) currentSweepAngle -= 360f;
        }

        DrawRadarVisualization();
        DrawLidarVisualization();
        DrawDetections();
        DrawTracks();
    }

    void OnRenderObject()
    {
        // Additional rendering can be done here for custom geometry
    }

    // ============================================
    // VISUALIZATION OBJECTS
    // ============================================
    void CreateVisualizationObjects()
    {
        // Create radar cone mesh
        if (showRadarCone && radarSensor != null)
        {
            radarConeObject = CreateRadarCone();
        }

        // Create lidar sphere
        if (showLidarRange && lidarSensor != null)
        {
            lidarSphereObject = CreateLidarSphere();
        }
    }

    GameObject CreateRadarCone()
    {
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = "RadarCone_Visual";
        cone.transform.SetParent(transform);
        
        if (radarSensor != null)
        {
            cone.transform.position = radarSensor.transform.position;
            cone.transform.rotation = radarSensor.transform.rotation;
        }

        // Make it a cone shape (scale)
        float height = radarSensor != null ? radarSensor.range : 1000f;
        cone.transform.localScale = new Vector3(height * 0.5f, height * 0.5f, height);

        var renderer = cone.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (radarConeMaterial != null)
                renderer.material = radarConeMaterial;
            else
                renderer.material.color = radarColor;
        }

        // Remove collider
        Destroy(cone.GetComponent<Collider>());

        return cone;
    }

    GameObject CreateLidarSphere()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "LidarSphere_Visual";
        sphere.transform.SetParent(transform);
        
        if (lidarSensor != null)
        {
            sphere.transform.position = lidarSensor.transform.position;
            float range = lidarSensor.maxRange;
            sphere.transform.localScale = Vector3.one * range * 2f;
        }

        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (lidarSphereMaterial != null)
                renderer.material = lidarSphereMaterial;
            else
            {
                renderer.material.color = lidarColor;
                renderer.material.SetFloat("_Mode", 2); // Fade mode
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.renderQueue = 3000;
            }
        }

        // Remove collider
        Destroy(sphere.GetComponent<Collider>());

        return sphere;
    }

    // ============================================
    // RADAR VISUALIZATION
    // ============================================
    void DrawRadarVisualization()
    {
        if (!showRadarCone || radarSensor == null || sensorOrigin == null) return;

        float range = radarSensor.range;
        float fov = radarSensor.fovDegrees;
        Vector3 origin = sensorOrigin.position;
        Vector3 forward = sensorOrigin.forward;

        // Draw FOV cone edges
        float halfFOV = fov * 0.5f;
        Vector3 leftEdge = Quaternion.Euler(0, -halfFOV, 0) * forward * range;
        Vector3 rightEdge = Quaternion.Euler(0, halfFOV, 0) * forward * range;

        Debug.DrawLine(origin, origin + leftEdge, radarColor);
        Debug.DrawLine(origin, origin + rightEdge, radarColor);

        // Draw sweep line
        if (animateSweep)
        {
            Vector3 sweepDir = Quaternion.Euler(0, currentSweepAngle - halfFOV, 0) * forward * range;
            Debug.DrawLine(origin, origin + sweepDir, Color.yellow);
        }

        // Draw range circles
        int segments = 32;
        for (int ring = 1; ring <= 3; ring++)
        {
            float r = range * (ring / 3f);
            DrawArc(origin, r, -halfFOV, halfFOV, segments, radarColor);
        }
    }

    void DrawArc(Vector3 center, float radius, float startAngle, float endAngle, int segments, Color color)
    {
        Vector3 prevPoint = center + Quaternion.Euler(0, startAngle, 0) * Vector3.forward * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)segments);
            Vector3 point = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Debug.DrawLine(prevPoint, point, color);
            prevPoint = point;
        }
    }

    // ============================================
    // LIDAR VISUALIZATION
    // ============================================
    void DrawLidarVisualization()
    {
        if (!showLidarRange || lidarSensor == null || sensorOrigin == null) return;

        Vector3 origin = sensorOrigin.position;
        float range = lidarSensor.maxRange;

        // Draw range sphere (wireframe)
        int latSegments = 16;
        int lonSegments = 32;

        for (int lat = 0; lat < latSegments; lat++)
        {
            float theta1 = (lat / (float)latSegments) * Mathf.PI;
            float theta2 = ((lat + 1) / (float)latSegments) * Mathf.PI;

            for (int lon = 0; lon < lonSegments; lon++)
            {
                float phi1 = (lon / (float)lonSegments) * 2f * Mathf.PI;
                float phi2 = ((lon + 1) / (float)lonSegments) * 2f * Mathf.PI;

                Vector3 p1 = origin + SphericalToCartesian(range, theta1, phi1);
                Vector3 p2 = origin + SphericalToCartesian(range, theta1, phi2);
                Vector3 p3 = origin + SphericalToCartesian(range, theta2, phi1);

                Debug.DrawLine(p1, p2, lidarColor);
                Debug.DrawLine(p1, p3, lidarColor);
            }
        }
    }

    Vector3 SphericalToCartesian(float radius, float theta, float phi)
    {
        float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
        float y = radius * Mathf.Cos(theta);
        float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
        return new Vector3(x, y, z);
    }

    // ============================================
    // DETECTION POINTS
    // ============================================
    void DrawDetections()
    {
        if (!showDetectionPoints || sensorFusion == null) return;

        var intruders = sensorFusion.GetDetectedIntruders();
        if (intruders == null) return;

        foreach (var intr in intruders)
        {
            if (intr == null) continue;

            Vector3 pos = intr.transform.position;
            
            // Draw detection marker
            DrawCrosshair(pos, 5f, detectionColor);
            
            // Draw line to sensor origin
            if (sensorOrigin != null)
            {
                Debug.DrawLine(sensorOrigin.position, pos, detectionColor * 0.5f);
            }
        }
    }

    // ============================================
    // TRACKING VISUALIZATION
    // ============================================
    void DrawTracks()
    {
        if (!showTrackingLines || sensorFusion == null) return;

        var tracks = sensorFusion.GetFusedTracks();
        if (tracks == null) return;

        foreach (var track in tracks)
        {
            Vector3 pos = track.worldPosition;
            Vector3 vel = track.velocity;
            float conf = track.confidence;

            // Draw track point with confidence coloring
            Color trackColorWithConf = Color.Lerp(lowConfidenceColor, highConfidenceColor, conf);
            DrawCrosshair(pos, 7f, trackColorWithConf);

            // Draw velocity vector
            if (vel.sqrMagnitude > 0.1f)
            {
                Debug.DrawLine(pos, pos + vel.normalized * 20f, trackColorWithConf);
            }

            // Draw confidence ring
            if (showConfidenceRings)
            {
                float ringSize = Mathf.Lerp(15f, 5f, conf);
                DrawCircle(pos, ringSize, 16, trackColorWithConf);
            }
        }
    }

    // ============================================
    // DRAWING UTILITIES
    // ============================================
    void DrawCrosshair(Vector3 center, float size, Color color)
    {
        Debug.DrawLine(center - Vector3.right * size, center + Vector3.right * size, color);
        Debug.DrawLine(center - Vector3.up * size, center + Vector3.up * size, color);
        Debug.DrawLine(center - Vector3.forward * size, center + Vector3.forward * size, color);
    }

    void DrawCircle(Vector3 center, float radius, int segments, Color color)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Quaternion.Euler(0, 0, 0) * Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep;
            Vector3 point = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Debug.DrawLine(prevPoint, point, color);
            prevPoint = point;
        }
    }

    // ============================================
    // PUBLIC API
    // ============================================
    public void SetVisualizationMode(bool radar, bool lidar, bool detections, bool tracks)
    {
        showRadarCone = radar;
        showLidarRange = lidar;
        showDetectionPoints = detections;
        showTrackingLines = tracks;
    }

    public void ToggleSweepAnimation(bool enabled)
    {
        animateSweep = enabled;
    }
}