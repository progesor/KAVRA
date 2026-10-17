using Progesor.Kavra;
using UnityEngine;

/// <summary>
/// Pure lab overlay for two independent Rigidbody observations. The
/// presentation only reads joint and telemetry; it never applies forces.
/// </summary>
public sealed class VehicleAssemblyLabHud : MonoBehaviour
{
    public VehicleAssemblyRoot Assembly;

    private void OnGUI()
    {
        if (Assembly == null || Assembly.Tractor == null || Assembly.Trailer == null)
            return;
        Rigidbody tow = Assembly.Tractor.GetComponent<Rigidbody>();
        Rigidbody trailer = Assembly.Trailer.GetComponent<Rigidbody>();
        VehicleSimulationRoot trailerSimulation =
            Assembly.Trailer.GetComponent<VehicleSimulationRoot>();
        if (tow == null || trailer == null || trailerSimulation == null) return;

        int onRoad = 0;
        var wheels = trailerSimulation.Telemetry;
        for (int i = 0; i < wheels.Count; i++)
            if (wheels[i].Grounded) onRoad++;

        float yaw = Vector3.SignedAngle(tow.transform.forward,
            trailer.transform.forward, Vector3.up);
        float gap = Assembly.Definition != null
            ? VehicleAssemblyMath.CalculateHitchErrorMeters(
                tow.position, tow.rotation, Assembly.Definition.TractorHitchLocal,
                trailer.position, trailer.rotation, Assembly.Definition.TrailerHitchLocal)
            : 0f;
        string status = "KAVRA M1.3 — TWO-RIGIDBODY ASSEMBLY" +
                        "\nJoint: " + (Assembly.IsAttached ? "Attached" : "Detached") +
                        "   Relative yaw: " + yaw.ToString("F1") + " deg" +
                        "\nHitch alignment error: " + (gap * 100f).ToString("F1") + " cm" +
                        "\nTrailer wheels grounded: " + onRoad + "/" + wheels.Count +
                        "\nTractor: " + (tow.linearVelocity.magnitude * 3.6f).ToString("F1") +
                        " km/h   Trailer: " +
                        (trailer.linearVelocity.magnitude * 3.6f).ToString("F1") + " km/h" +
                        "\nR = explicit manual BOTH-body recovery, while stopped";
        float x = Mathf.Max(12f, Screen.width - 435f);
        GUI.Box(new Rect(x, 12f, 425f, 130f), status);
    }
}
