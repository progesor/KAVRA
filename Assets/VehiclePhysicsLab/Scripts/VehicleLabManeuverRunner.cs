using UnityEngine;

/// <summary>
/// Repeatable, deterministic-input physics laboratory experiment.
/// Not a vehicle AI, not a runtime package dependency, and NEVER tries to
/// prevent or automatically recover a rollover.
/// </summary>
public enum VehicleLabManeuver
{
    StraightBrake = 0,
    SteeringStep = 1,
    CoastDown = 2
}

public struct VehicleLabDriveCommand
{
    public float Throttle;
    public float Steering;
    public float ServiceBrake;
}

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class VehicleLabManeuverRunner : MonoBehaviour
{
    public Progesor.Kavra.VehicleSimulationRoot Simulation;
    public VehicleLabManeuver Maneuver = VehicleLabManeuver.StraightBrake;
    public bool RunOnPlay = true;
    public float ElapsedSeconds { get; private set; }

    private void Awake()
    {
        if (Simulation == null)
            Simulation = GetComponent<Progesor.Kavra.VehicleSimulationRoot>();
    }

    private void FixedUpdate()
    {
        if (!RunOnPlay || Simulation == null) return;
        ElapsedSeconds += Time.fixedDeltaTime;
        VehicleLabDriveCommand input = Evaluate(Maneuver, ElapsedSeconds);
        Simulation.SetInputs(input.Throttle, input.Steering, input.ServiceBrake, 0f);
    }

    /// <summary>
    /// Input schedules are independent of framerate/rendering.
    /// Actual Rigidbody motion can still vary by platform/physics version.
    /// </summary>
    public static VehicleLabDriveCommand Evaluate(VehicleLabManeuver maneuver, float timeSeconds)
    {
        float t = Mathf.Max(0f, timeSeconds);
        var control = new VehicleLabDriveCommand();
        if (t < 2f) return control; // gravity/suspension settle before the test

        switch (maneuver)
        {
            case VehicleLabManeuver.StraightBrake:
                if (t < 8f) control.Throttle = 0.55f;
                else if (t >= 9f && t < 16f) control.ServiceBrake = 0.95f;
                break;

            case VehicleLabManeuver.SteeringStep:
                if (t < 8f) control.Throttle = 0.45f;
                else if (t < 12f)
                {
                    control.Throttle = 0.25f;
                    control.Steering = t < 10f ? 0.4f : -0.4f;
                }
                else if (t < 19f) control.ServiceBrake = 0.8f;
                break;

            case VehicleLabManeuver.CoastDown:
                if (t < 7f) control.Throttle = 0.55f;
                else if (t >= 21f && t < 27f) control.ServiceBrake = 0.75f;
                break;
        }
        return control;
    }
}
