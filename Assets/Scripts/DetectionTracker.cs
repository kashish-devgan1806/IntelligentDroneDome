using UnityEngine;

/// <summary>
/// Small data type & filter used by SensorFusion.
/// Simple exponential smoothing / alpha-beta filter for position + velocity.
/// </summary>
public class DetectionTracker
{
    public int id;
    public Vector3 position;
    public Vector3 velocity;
    public float confidence;
    public float age;       // seconds since creation
    public float lastUpdateTime;

    // smoothing params
    private float alpha = 0.6f; // position smoothing
    private float beta = 0.4f;  // velocity smoothing

    public DetectionTracker(int id, Vector3 pos, Vector3 vel, float conf)
    {
        this.id = id;
        this.position = pos;
        this.velocity = vel;
        this.confidence = conf;
        this.age = 0f;
        this.lastUpdateTime = Time.time;
    }

    public void UpdateWithMeasurement(Vector3 measuredPos, float measuredConf)
    {
        float dt = Mathf.Max(0.0001f, Time.time - lastUpdateTime);
        Vector3 measVel = (measuredPos - position) / dt;

        // alpha-beta smoothing
        position = Vector3.Lerp(position, measuredPos, alpha);
        velocity = Vector3.Lerp(velocity, measVel, beta);

        confidence = Mathf.Clamp01(Mathf.Lerp(confidence, measuredConf, 0.5f));
        lastUpdateTime = Time.time;
        age = 0f;
    }

    public void Predict(float dt)
    {
        position += velocity * dt;
        age += dt;
    }
}
