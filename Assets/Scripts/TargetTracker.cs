// using UnityEngine;

// /// <summary>
// /// Small utility to provide a stable reference point for tracking (used by EngagementCoordinator and others).
// /// Attach to interceptor or intruder prefabs if not present.
// /// Provides last-known position and velocity smoothing.
// /// </summary>
// public class TargetTracker : MonoBehaviour
// {
//     public Vector3 SmoothedPosition { get; private set; }
//     public Vector3 SmoothedVelocity { get; private set; }

//     [Range(0.01f, 1f)]
//     public float smoothing = 0.12f;

//     Vector3 lastPos;
//     float lastTime;

//     void Awake()
//     {
//         SmoothedPosition = transform.position;
//         lastPos = transform.position;
//         lastTime = Time.time;
//     }

//     void LateUpdate()
//     {
//         float dt = Mathf.Max(1e-6f, Time.time - lastTime);
//         Vector3 instantVel = (transform.position - lastPos) / dt;
//         SmoothedVelocity = Vector3.Lerp(SmoothedVelocity, instantVel, smoothing);
//         SmoothedPosition = Vector3.Lerp(SmoothedPosition, transform.position, smoothing);

//         lastPos = transform.position;
//         lastTime = Time.time;
//     }
// }


using UnityEngine;

public class TargetTracker : MonoBehaviour
{
    public Transform tracked;
    public Vector3 smoothedPosition;
    public float smoothing = 0.08f;

    private void Update()
    {
        if (tracked == null) return;
        smoothedPosition = Vector3.Lerp(smoothedPosition, tracked.position, Time.deltaTime / Mathf.Max(0.0001f, smoothing));
        transform.position = smoothedPosition;
    }
}

