using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SensorVisualization : MonoBehaviour
{
    public RadarSensor radar;
    public LidarSensor lidar;
    public SensorFusion fusion;

    public Color radarColor = Color.yellow;
    public Color lidarColor = Color.cyan;
    public Color trackColor = Color.green;

    void OnDrawGizmos()
    {
        if (radar != null)
        {
            // draw FOV arc
            Gizmos.color = radarColor;
            Vector3 pos = radar.transform.position;
            Vector3 fwd = radar.transform.forward;
            float half = radar.fovDegrees * 0.5f;
            int seg = 24;
            for (int i=0;i<seg;i++)
            {
                float a1 = (-half + (i/(float)seg)*radar.fovDegrees) * Mathf.Deg2Rad;
                float a2 = (-half + ((i+1)/(float)seg)*radar.fovDegrees) * Mathf.Deg2Rad;
                Vector3 p1 = pos + Quaternion.Euler(0f, Mathf.Rad2Deg*a1, 0f) * fwd * radar.range;
                Vector3 p2 = pos + Quaternion.Euler(0f, Mathf.Rad2Deg*a2, 0f) * fwd * radar.range;
                Gizmos.DrawLine(pos, p1);
                Gizmos.DrawLine(p1, p2);
            }
        }

        if (lidar != null)
        {
            Gizmos.color = lidarColor;
            Gizmos.DrawWireSphere(lidar.transform.position, lidar.maxRange);
        }

        if (fusion != null)
        {
            Gizmos.color = trackColor;
            var tracks = fusion.GetFusedTracks();
            foreach (var t in tracks)
            {
                Gizmos.DrawSphere(t.worldPosition, 2f);
                Gizmos.DrawLine(t.worldPosition, t.worldPosition + t.velocity.normalized * 6f);
            }
        }
    }
}
