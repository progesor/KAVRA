using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Sample keyboard and diagnostic HUD, deliberately OUTSIDE the UPM package.
/// Integrating games provide their own input adapter.
/// </summary>
[DisallowMultipleComponent]
public sealed class VehicleLabKeyboardInput : MonoBehaviour
{
    public Progesor.Kavra.VehicleSimulationRoot Simulation;
    private Rigidbody vehicleBody;

    private void Awake()
    {
        if (Simulation == null)
            Simulation = GetComponent<Progesor.Kavra.VehicleSimulationRoot>();
        vehicleBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Simulation == null) return;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            Simulation.SetInputs(0f, 0f, 0f, 0f);
            return;
        }

        bool forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        bool backward = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        float throttle = (forward ? 1f : 0f) - (backward ? 1f : 0f);
        float steering = (right ? 1f : 0f) - (left ? 1f : 0f);

        Simulation.SetInputs(throttle, steering,
            keyboard.spaceKey.isPressed ? 1f : 0f,
            keyboard.leftShiftKey.isPressed ? 1f : 0f);
#endif
    }

    private void OnGUI()
    {
        if (Simulation == null) return;

        float speedKmh = vehicleBody != null
            ? vehicleBody.linearVelocity.magnitude * 3.6f : 0f;
        float totalNormal = 0f;
        float maxBottomOut = 0f;
        int groundedCount = 0;
        int fallbackCount = 0;
        var telemetry = Simulation.Telemetry;
        if (telemetry != null)
        {
            for (int i = 0; i < telemetry.Count; i++)
            {
                var wheel = telemetry[i];
                if (wheel.Grounded) groundedCount++;
                if (wheel.UsedRayFallback) fallbackCount++;
                totalNormal += wheel.NormalLoadN;
                maxBottomOut = Mathf.Max(maxBottomOut, wheel.BottomOutMeters);
            }
        }

        float expected = vehicleBody != null
            ? vehicleBody.mass * Physics.gravity.magnitude : 0f;
        float tilt = vehicleBody != null
            ? Vector3.Angle(Vector3.up, vehicleBody.transform.up) : 0f;
        float coMY = vehicleBody != null
            ? vehicleBody.worldCenterOfMass.y : 0f;

        string message = "KAVRA M0 | experimental | " + speedKmh.ToString("F1") + " km/h" +
                         "\nContact: " + groundedCount + "/" + (telemetry != null ? telemetry.Count : 0) +
                         "  (" + Simulation.ContactMode + ", fallback " + fallbackCount + ")" +
                         "\nSupport Fz: " + (totalNormal * 0.001f).ToString("F2") + " kN / " +
                         (expected * 0.001f).ToString("F2") + " kN static ref" +
                         "\nBottom-out: " + (maxBottomOut * 1000f).ToString("F1") + " mm" +
                         "  Tilt: " + tilt.ToString("F1") + " deg" +
                         "\nCenter of mass Y: " + coMY.ToString("F2") + " m";
#if ENABLE_INPUT_SYSTEM
        message += "\nW/S: torque   A/D: steer   Space: brake   Shift: park";
#else
        message += "\nInput System unavailable: use VehicleSimulationRoot Inspector controls.";
#endif
        GUI.Box(new Rect(12f, 12f, 435f, 150f), message);
    }
}
