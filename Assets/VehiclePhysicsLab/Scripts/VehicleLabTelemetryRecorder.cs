using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// PhysicsLab-only time-series recorder. Never ships in the reusable UPM.
/// Intentionally collects a short sample on Play to investigate roll, bottom
/// out, wheel load and contact-mode differences in CSV.
/// </summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class VehicleLabTelemetryRecorder : MonoBehaviour
{
    public Progesor.Kavra.VehicleSimulationRoot Simulation;
    public bool RecordOnPlay = true;
    [Range(3f, 180f)] public float RecordSeconds = 90f;

    private Rigidbody vehicleBody;
    private VehicleLabManeuverRunner scripted;
    private StringBuilder rows;
    private float elapsed;
    private bool finished;

    private static string F(float value)
    {
        return value.ToString("F5", CultureInfo.InvariantCulture);
    }

    private void Awake()
    {
        if (Simulation == null)
            Simulation = GetComponent<Progesor.Kavra.VehicleSimulationRoot>();
        vehicleBody = GetComponent<Rigidbody>();
        scripted = GetComponent<VehicleLabManeuverRunner>();
        if (!RecordOnPlay || Simulation == null) return;
        rows = new StringBuilder(16 * 1024);
        rows.AppendLine("time_s,wheel_id,grounded,contact_mode,surface,mu_long,mu_lat,rolling_coeff,contact_cost_us_total,static_share,compression_m,bottomout_m,bump_n,antiroll_n,fz_n,fx_n,fy_n,slipx_mps,slipy_mps,wheel_rpm,speed_mps,tilt_deg,com_height_m,steer_deg,slip_ratio,slip_angle_deg,tire_profile,tire_kx,tire_ky,throttle_input,steer_input,brake_input,steer_mode,maneuver,brake_impulse_limited,brake_limiter_enabled");
    }

    private void FixedUpdate()
    {
        if (finished || rows == null || Simulation == null) return;
        elapsed += Time.fixedDeltaTime;

        float speed = vehicleBody != null ? vehicleBody.linearVelocity.magnitude : 0f;
        float tilt = vehicleBody != null ?
            Vector3.Angle(Vector3.up, vehicleBody.transform.up) : 0f;
        float height = vehicleBody != null ? vehicleBody.worldCenterOfMass.y : 0f;

        var wheels = Simulation.Telemetry;
        for (int i = 0; i < wheels.Count; i++)
        {
            var w = wheels[i];
            rows.Append(F(elapsed)).Append(',')
                .Append(w.WheelId).Append(',')
                .Append(w.Grounded ? "1" : "0").Append(',')
                .Append(w.UsedSphereContact ? "sphere" :
                        w.UsedRayFallback ? "ray_fallback" : "ray").Append(',')
                .Append(w.SurfaceName ?? "").Append(',')
                .Append(F(w.EffectiveLongitudinalFriction)).Append(',')
                .Append(F(w.EffectiveLateralFriction)).Append(',')
                .Append(F(w.EffectiveRollingResistance)).Append(',')
                .Append(F(Simulation.ContactQueryMicrosecondsLastStep)).Append(',')
                .Append(F(w.StaticLoadFraction)).Append(',')
                .Append(F(w.CompressionMeters)).Append(',')
                .Append(F(w.BottomOutMeters)).Append(',')
                .Append(F(w.BumpStopForceN)).Append(',')
                .Append(F(w.AntiRollForceN)).Append(',')
                .Append(F(w.NormalLoadN)).Append(',')
                .Append(F(w.LongitudinalForceN)).Append(',')
                .Append(F(w.LateralForceN)).Append(',')
                .Append(F(w.LongitudinalSlipMps)).Append(',')
                .Append(F(w.LateralSlipMps)).Append(',')
                .Append(F(w.WheelRpm)).Append(',')
                .Append(F(speed)).Append(',')
                .Append(F(tilt)).Append(',')
                .Append(F(height)).Append(',')
                .Append(F(w.SteerAngleDegrees)).Append(',')
                .Append(F(w.SlipRatio)).Append(',')
                .Append(F(w.SlipAngleDegrees)).Append(',')
                .Append(w.TireProfileName ?? "Default").Append(',')
                .Append(F(w.EffectiveLongitudinalStiffness)).Append(',')
                .Append(F(w.EffectiveLateralStiffness)).Append(',')
                .Append(F(Simulation.Throttle)).Append(',')
                .Append(F(Simulation.Steering)).Append(',')
                .Append(F(Simulation.ServiceBrake)).Append(',')
                .Append(Simulation.SteeringMode.ToString()).Append(',')
                .Append(scripted != null ? scripted.Maneuver.ToString() : "Manual").Append(',')
                .Append(w.BrakeImpulseLimited ? "1" : "0").Append(',')
                .Append(Simulation.LimitNearStopBrakingImpulse ? "1" : "0")
                .AppendLine();
        }

        if (elapsed >= RecordSeconds) Save();
    }

    private void OnDisable() { Save(); }
    private void OnApplicationQuit() { Save(); }

    private void Save()
    {
        if (finished || rows == null) return;
        finished = true;
        try
        {
            string filename = "kavra_m0_telemetry_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) +
                ".csv";
            string path = Path.Combine(Application.persistentDataPath, filename);
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(path, rows.ToString(), Encoding.UTF8);
            Debug.Log("KAVRA M0: telemetry recorded " + elapsed.ToString("F1") +
                      " s at " + path);
        }
        catch (Exception error)
        {
            Debug.LogWarning("KAVRA M0: telemetry CSV could not be saved: " + error.Message);
        }
    }
}
