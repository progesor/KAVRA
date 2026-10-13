using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// KAVRA host-project keyboard adapter. Deliberately NOT in the reusable
/// package: other Unity games can provide their own input and camera logic.
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
        Simulation.SetInputs(throttle, steering, keyboard.spaceKey.isPressed ? 1f : 0f,
            keyboard.leftShiftKey.isPressed ? 1f : 0f);
#endif
    }

    private void OnGUI()
    {
        if (Simulation == null) return;
        float kmh = vehicleBody != null ? vehicleBody.linearVelocity.magnitude * 3.6f : 0f;
        string message = "KAVRA M0 | approx physics | " + kmh.ToString("F1") + " km/h";
#if ENABLE_INPUT_SYSTEM
        message += "\nW/S: torque (+/-)    A/D: steer\nSPACE: brake    LSHIFT: parking brake";
#else
        message += "\nEnable the new Input System to drive with keyboard.\nYou may edit controls in VehicleSimulationRoot Inspector.";
#endif
        GUI.Box(new Rect(12f, 12f, 335f, 80f), message);
    }
}
