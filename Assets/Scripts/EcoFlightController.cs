using UnityEngine;

/// <summary>
/// Handles eco–aware movement metrics for a drone:
/// energy usage, carbon output, turn-angle estimation,
/// and zone-avoidance repulsion.
/// </summary>
[RequireComponent(typeof(DroneController))]
public class EcoFlightController : MonoBehaviour
{
    [Header("Eco Settings")]
    public float ecoTurnSensitivity = 1.0f;  
    public float ecoSpeedSensitivity = 1.0f;

    // PUBLIC GET (READ-ONLY), INTERNAL SET
    public float energyConsumed { get; private set; }
    public float carbonEmitted { get; private set; }

    // Internal tracking
    private Vector3 lastForward;
    private float lastSpeed;
    private DroneController controller;

    void Awake()
    {
        controller = GetComponent<DroneController>();
        lastForward = transform.forward;
        lastSpeed = controller.speed;
    }

    /// <summary>
    /// Called every frame by the drone controller OR the coordinator.
    /// Computes turn magnitude and energy.
    /// </summary>
    public void EcoUpdate()
    {
        // TURN ANGLE CHANGE
        float turnAngle = Vector3.Angle(lastForward, transform.forward);
        lastForward = transform.forward;

        // SPEED CHANGE
        float speed = controller.speed;
        float speedDelta = Mathf.Abs(speed - lastSpeed);
        lastSpeed = speed;

        // Apply repulsion from eco-zones
        Vector3 repulse = EcoConservationManager.Instance.GetEcoRepulsion(transform.position);
        if (repulse.sqrMagnitude > 0.001f)
            transform.position += repulse * Time.deltaTime;

        // Send calculation to global manager
        EcoConservationManager.Instance.ApplyEnergyCost(
            this,
            speed,
            turnAngle * ecoTurnSensitivity
        );
    }

    // ---- CALLED BY EcoConservationManager ----
    public void AddEnergy(float amount)
    {
        energyConsumed += Mathf.Max(0, amount);
    }

    public void AddCarbon(float amount)
    {
        carbonEmitted += Mathf.Max(0, amount);
    }
}
