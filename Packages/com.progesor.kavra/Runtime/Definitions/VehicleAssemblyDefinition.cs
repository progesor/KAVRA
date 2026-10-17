using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// First KAVRA assembly asset: two INDEPENDENT vehicle rigidbodies
    /// coupled by a hitch. Each vehicle retains its own wheel simulation.
    /// Intended to scale later to a list/graph of arbitrary body connections.
    /// </summary>
    [CreateAssetMenu(fileName = "KAVRA_VehicleAssembly",
        menuName = "KAVRA/Vehicle Assembly Definition")]
    public sealed class VehicleAssemblyDefinition : ScriptableObject
    {
        [Header("Two vehicle definitions (one Rigidbody each)")]
        public VehicleDefinition Tractor;
        public VehicleDefinition Trailer;

        [Header("Coupling points in each vehicle's LOCAL coordinates")]
        public Vector3 TractorHitchLocal = new Vector3(0f, -0.1f, -2.5f);
        public Vector3 TrailerHitchLocal = new Vector3(0f, -0.1f, 2.2f);

        [Header("Hitch rotation")]
        [Range(0f, 60f)] public float MaximumPitchDegrees = 18f;
        [Range(0f, 45f)] public float MaximumRollDegrees = 12f;
        [Tooltip("Manual H capture distance, in meters. Close, safely aligned hitches are snapped together ONCE; this is not automatic proximity coupling.")]
        [Min(0.01f)] public float MaximumAttachErrorMeters = 0.5f;

        [Header("Manual hitch engagement safety")]
        [Range(0f, 180f)] public float MaximumAttachYawDegrees = 55f;
        [Min(0.01f)] public float MaximumAttachRelativeSpeedMps = 1.25f;
        [Min(0.01f)] public float MaximumManualCouplingBodySpeedMps = 1.75f;
        [Tooltip("When H is pressed within the tiny hitch tolerance, place the trailer's local hitch at the tractor hitch BEFORE creating the joint. Never auto-align while driving.")]
        public bool AlignHitchesOnManualAttach = true;

        [Header("Physical hitch break thresholds (0 = unbreakable)")]
        [Tooltip("Maximum ConfigurableJoint solver force in newtons. Zero disables force breaking. Experimental sample value, NOT real hardware data.")]
        [Min(0f)] public float BreakForceNewtons = 150000f;
        [Tooltip("Maximum solver torque in N*m. Zero disables torque breaking. Works only on locked/limited rotation axes.")]
        [Min(0f)] public float BreakTorqueNewtonMeters = 100000f;

        [Header("Basic towed brake command (no pneumatic brake dynamics)")]
        [Range(0f, 2f)] public float TrailerServiceBrakeMultiplier = 1f;
        public bool FollowTractorParkingBrake = true;

        // M1.3 assets written before these fields existed must not silently
        // become non-attachable or unbreakable when imported into M1.4.
        // The version marker differentiates that case from a new asset
        // whose user deliberately set break force/torque to 0 (unbreakable).
        [SerializeField, HideInInspector] private int hitchSettingsVersion = 2;

        private void OnEnable()
        {
            UpgradeLegacyHitchSettings();
        }

        private void OnValidate()
        {
            UpgradeLegacyHitchSettings();
        }

        private void UpgradeLegacyHitchSettings()
        {
            if (hitchSettingsVersion == 0)
            {
                // Original M1.3 assets did not have break/approach fields.
                MaximumAttachYawDegrees = 35f;
                MaximumAttachRelativeSpeedMps = 1.25f;
                MaximumManualCouplingBodySpeedMps = 1.75f;
                AlignHitchesOnManualAttach = true;
                BreakForceNewtons = 150000f;
                BreakTorqueNewtonMeters = 100000f;
                hitchSettingsVersion = 1;
            }

            if (hitchSettingsVersion == 1)
            {
                // Only change EXACT old defaults. A tuned M1.4 asset
                // retains its own capture radius and yaw tolerance.
                if (Mathf.Approximately(MaximumAttachErrorMeters, 0.15f))
                    MaximumAttachErrorMeters = 0.5f;
                if (Mathf.Approximately(MaximumAttachYawDegrees, 35f))
                    MaximumAttachYawDegrees = 55f;
                hitchSettingsVersion = 2;
            }
        }
    }
}
