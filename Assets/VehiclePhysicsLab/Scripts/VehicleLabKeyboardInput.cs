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
    [Tooltip("Disable manual driving keys during an automated calibration run. R recovery remains manual.")]
    public bool AcceptDrivingKeys = true;
    private Rigidbody vehicleBody;
    private string recoveryStatus = "";
    private float recoveryStatusUntil;

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

        // Never prevent a rollover automatically. Recovery occurs ONLY
        // after a deliberate R key-down, at sufficiently low speed.
        if (keyboard.rKey.wasPressedThisFrame)
        {
            Progesor.Kavra.VehicleRecoveryResult outcome =
                Progesor.Kavra.VehicleManualRecovery.TryRecover(Simulation);
            recoveryStatus = outcome == Progesor.Kavra.VehicleRecoveryResult.Success
                ? "Recovered upright (manual)"
                : "Recovery unavailable: " + outcome;
            recoveryStatusUntil = Time.unscaledTime + 4f;
            if (outcome == Progesor.Kavra.VehicleRecoveryResult.Success)
            {
                Debug.Log("KAVRA: manual vehicle recovery succeeded (R).");
                return; // Do not reapply throttle/steering in this frame.
            }
        }

        if (!AcceptDrivingKeys) return; // Scripted runner owns input during calibration.

        // H is a conscious hitch command: never auto-connect in proximity.
        // Attached H detaches; detached/broken H retries only when position,
        // yaw and relative hitch speed pass VehicleCoupling.TryAttach.
        if (keyboard.hKey.wasPressedThisFrame)
        {
            Progesor.Kavra.VehicleAssemblyMember member =
                GetComponent<Progesor.Kavra.VehicleAssemblyMember>();
            if (member != null && member.IsTractor && member.Assembly != null)
            {
                Progesor.Kavra.VehicleAssemblyRoot assembly = member.Assembly;
                if (assembly.IsAttached)
                {
                    recoveryStatus = assembly.Detach()
                        ? "Trailer uncoupled manually."
                        : "Trailer could not be disconnected.";
                }
                else
                {
                    Progesor.Kavra.VehicleCouplingResult result = assembly.TryAttach();
                    recoveryStatus = result == Progesor.Kavra.VehicleCouplingResult.Connected
                        ? "Trailer coupled."
                        : "Coupling refused: " + result;
                }
                recoveryStatusUntil = Time.unscaledTime + 4f;
            }
        }

        bool forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        bool backward = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        float throttle = (forward ? 1f : 0f) - (backward ? 1f : 0f);
        float steering = (right ? 1f : 0f) - (left ? 1f : 0f);
        float braking = keyboard.spaceKey.isPressed ? 1f : 0f;

        if (Simulation.DriveSource == Progesor.Kavra.VehicleDriveSource.SimplePowertrain)
        {
            // The simplified gearbox uses 0..1 accelerator and a separate
            // gear sign. W/S automatically selects forward/reverse at low
            // speed. Pressing the opposite direction while driving first
            // brakes instead of instantly reversing drivetrain torque.
            if (keyboard.tKey.wasPressedThisFrame)
                Simulation.GearboxMode =
                    Simulation.GearboxMode == Progesor.Kavra.GearboxControlMode.Automatic
                        ? Progesor.Kavra.GearboxControlMode.Manual
                        : Progesor.Kavra.GearboxControlMode.Automatic;

            int selectedGear = Simulation.SelectedGear;
            if (Simulation.GearboxMode == Progesor.Kavra.GearboxControlMode.Manual)
            {
                if (keyboard.eKey.wasPressedThisFrame && selectedGear >= 1)
                    Simulation.TrySetGear(selectedGear + 1);
                else if (keyboard.qKey.wasPressedThisFrame && selectedGear > 1)
                    Simulation.TrySetGear(selectedGear - 1);
                else if (keyboard.nKey.wasPressedThisFrame)
                    Simulation.TrySetGear(0);
            }

            float longitudinal = vehicleBody != null
                ? Vector3.Dot(vehicleBody.linearVelocity, transform.forward) : 0f;
            if (forward == backward)
            {
                throttle = 0f;
            }
            else if (forward)
            {
                if (longitudinal < -0.75f)
                {
                    braking = 1f;
                    throttle = 0f;
                }
                else
                {
                    if (Simulation.SelectedGear < 1) Simulation.TrySetGear(1);
                    throttle = 1f;
                }
            }
            else
            {
                if (longitudinal > 0.75f)
                {
                    braking = 1f;
                    throttle = 0f;
                }
                else
                {
                    Simulation.TrySetGear(-1);
                    throttle = 1f;
                }
            }
        }

        Simulation.SetInputs(throttle, steering, braking,
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
        float maxAbsSlipRatio = 0f;
        float maxAbsSlipAngle = 0f;
        float maxActualSteer = 0f;
        int groundedCount = 0;
        int fallbackCount = 0;
        string activeSurface = "";
        bool mixedSurface = false;
        var telemetry = Simulation.Telemetry;
        if (telemetry != null)
        {
            for (int i = 0; i < telemetry.Count; i++)
            {
                var wheel = telemetry[i];
                if (wheel.Grounded) groundedCount++;
                if (wheel.UsedRayFallback) fallbackCount++;
                if (wheel.Grounded)
                {
                    if (activeSurface.Length == 0) activeSurface = wheel.SurfaceName;
                    else if (wheel.SurfaceName != activeSurface) mixedSurface = true;
                }
                totalNormal += wheel.NormalLoadN;
                maxBottomOut = Mathf.Max(maxBottomOut, wheel.BottomOutMeters);
                if (wheel.Grounded)
                {
                    maxAbsSlipRatio = Mathf.Max(maxAbsSlipRatio, Mathf.Abs(wheel.SlipRatio));
                    maxAbsSlipAngle = Mathf.Max(maxAbsSlipAngle, Mathf.Abs(wheel.SlipAngleDegrees));
                    maxActualSteer = Mathf.Max(maxActualSteer, Mathf.Abs(wheel.SteerAngleDegrees));
                }
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
                         "\nCenter of mass Y: " + coMY.ToString("F2") + " m" +
                         "\nSurface: " +
                         (mixedSurface ? "Mixed (transition)" :
                          activeSurface.Length != 0 ? activeSurface : "Air") +
                         "\nCast CPU cost: " +
                         (Simulation.MeasureContactQueryCost
                             ? Simulation.ContactQueryMicrosecondsLastStep.ToString("F1") + " us/step"
                             : "off (enable in Inspector)") +
                         "\nSteer geometry: " + Simulation.SteeringMode +
                         "  |max steer|: " + maxActualSteer.ToString("F1") + " deg" +
                         "\nMax |slip ratio|: " + maxAbsSlipRatio.ToString("F2") +
                         "  |slip angle|: " + maxAbsSlipAngle.ToString("F1") + " deg";
#if ENABLE_INPUT_SYSTEM
        if (Simulation.DriveSource == Progesor.Kavra.VehicleDriveSource.SimplePowertrain)
        {
            message += "\nPOWERTRAIN: " + Simulation.EngineRpm.ToString("F0") +
                " RPM   Gear: " + Simulation.EngagedGear +
                " (target " + Simulation.SelectedGear + ")" +
                "   Mode: " + Simulation.GearboxMode +
                (Simulation.IsGearShifting ? "  SHIFTING" : "") +
                "   Engine Nm: " + Simulation.EngineTorqueNm.ToString("F0") +
                "   Wheel torque Nm: " + Simulation.TotalAppliedDriveTorqueNm.ToString("F0");
        }

        if (AcceptDrivingKeys)
            message += Simulation.DriveSource ==
                       Progesor.Kavra.VehicleDriveSource.SimplePowertrain
                ? "\nW/S: fwd/rev   Q/E: gears (manual)   T: auto/manual   N: neutral   A/D: steer"
                : "\nW/S: torque   A/D: steer   Space: brake   Shift: park";
        else
            message += "\nSCRIPTED test: set maneuver in VehicleLabManeuverRunner";
        message += "\nR: MANUAL upright recovery (at low speed)";
        Progesor.Kavra.VehicleAssemblyMember assemblyMember =
            GetComponent<Progesor.Kavra.VehicleAssemblyMember>();
        if (assemblyMember != null && assemblyMember.IsTractor &&
            assemblyMember.Assembly != null)
            message += "\nH: manually connect/disconnect trailer";
#else
        message += "\nInput System unavailable: use VehicleSimulationRoot Inspector controls.";
#endif
        if (Time.unscaledTime < recoveryStatusUntil)
            message += "\n" + recoveryStatus;
        GUI.Box(new Rect(12f, 12f, 690f, 340f), message);
    }
}
