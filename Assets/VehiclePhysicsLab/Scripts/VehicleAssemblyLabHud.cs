using Progesor.Kavra;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Host-only articulated vehicle diagnostics; polls state and displays the
/// actual Unity break callback count. Does not auto-connect or apply forces.
/// </summary>
public sealed class VehicleAssemblyLabHud : MonoBehaviour
{
    public VehicleAssemblyRoot Assembly;
    [Range(0.1f, 10f)]
    [Tooltip("LAB ONLY: K applies a real lateral Rigidbody impulse, not a scripted detach. The physics Joint decides whether breakForce/breakTorque are exceeded.")]
    public float StressImpulseMetersPerSecond = 4f;

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.kKey.wasPressedThisFrame ||
            Assembly == null || !Assembly.IsAttached || Assembly.Trailer == null)
            return;

        Rigidbody trailerBody = Assembly.Trailer.GetComponent<Rigidbody>();
        if (trailerBody == null) return;

        // Stress a real constrained hitch. This is a LAB impulse on the
        // trailer body; it NEVER calls Detach() or OnJointBreak() directly.
        // The PhysX solver alone can break the ConfigurableJoint.
        Vector3 impulse = trailerBody.transform.right *
                          (trailerBody.mass * StressImpulseMetersPerSecond);
        Vector3 applicationPoint = trailerBody.worldCenterOfMass +
                                   trailerBody.transform.forward * 1.4f;
        trailerBody.AddForceAtPosition(impulse, applicationPoint, ForceMode.Impulse);
        Debug.Log("KAVRA LAB: lateral trailer impulse applied. Observe Unity hitch " +
                  "breakForce/breakTorque and OnJointBreak. Reconnect manually with H.", this);
#endif
    }

    private void OnGUI()
    {
        if (Assembly == null || Assembly.Tractor == null || Assembly.Trailer == null)
            return;

        Rigidbody tractor = Assembly.Tractor.GetComponent<Rigidbody>();
        Rigidbody trailer = Assembly.Trailer.GetComponent<Rigidbody>();
        VehicleSimulationRoot trailerSimulation =
            Assembly.Trailer.GetComponent<VehicleSimulationRoot>();
        VehicleAssemblyDefinition settings = Assembly.Definition;
        VehicleCoupling hitch = Assembly.Coupling;
        if (tractor == null || trailer == null ||
            trailerSimulation == null || settings == null || hitch == null)
            return;

        int grounded = 0;
        var wheels = trailerSimulation.Telemetry;
        for (int i = 0; i < wheels.Count; i++)
            if (wheels[i].Grounded) grounded++;

        float yaw = Vector3.SignedAngle(tractor.transform.forward,
            trailer.transform.forward, Vector3.up);
        float gap = VehicleAssemblyMath.CalculateHitchErrorMeters(
            tractor.position, tractor.rotation, settings.TractorHitchLocal,
            trailer.position, trailer.rotation, settings.TrailerHitchLocal);
        float forceKiloNewtons = settings.BreakForceNewtons * 0.001f;
        float torqueKiloNm = settings.BreakTorqueNewtonMeters * 0.001f;

        VehicleLabHitchGuide guide = GetComponent<VehicleLabHitchGuide>();
        VehicleHitchAssessment reading = guide != null
            ? guide.SelectedAssessment() : default(VehicleHitchAssessment);
        string guidance = guide != null ? guide.SelectedStatus()
            : "H: manual connect/disconnect";

        string report = "KAVRA M1.5.1 — TRAILER HITCH LAB" +
                        "\nState: " + hitch.State +
                        "   Breaks: " + hitch.PhysicalBreakCount +
                        "   Last break: " + hitch.LastBreakForceNewtons.ToString("F0") + " N" +
                        "\nHitch gap: " + (gap * 100f).ToString("F1") + " cm" +
                        " / " + (settings.MaximumAttachErrorMeters * 100f).ToString("F0") +
                        " cm capture" +
                        "   Relative yaw: " + yaw.ToString("F1") + " deg" +
                        "\n" + guidance +
                        "  | Approach speed: " +
                        reading.RelativeHitchSpeedMps.ToString("F2") + " m/s" +
                        "\nForce break: " +
                        (settings.BreakForceNewtons > 0f
                            ? forceKiloNewtons.ToString("F1") + " kN" : "disabled") +
                        "   Torque break: " +
                        (settings.BreakTorqueNewtonMeters > 0f
                            ? torqueKiloNm.ToString("F1") + " kNm" : "disabled") +
                        "\nTrailer ground contacts: " + grounded + "/" + wheels.Count +
                        "\nTractor: " + (tractor.linearVelocity.magnitude * 3.6f).ToString("F1") +
                        " km/h   Trailer: " +
                        (trailer.linearVelocity.magnitude * 3.6f).ToString("F1") + " km/h" +
                        "\nH = attach/detach when hitch aligned and nearly stopped" +
                        "\nK = LAB hitch overload impulse (physics decides break)" +
                        "\nOverload BREAK is automatic; reconnection requires H" +
                        "\nR = manual BOTH-body recovery while connected";

        float x = Mathf.Max(12f, Screen.width - 465f);
        GUI.Box(new Rect(x, 12f, 455f, 242f), report);
    }
}
