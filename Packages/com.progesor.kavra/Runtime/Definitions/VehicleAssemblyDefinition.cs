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
        [Min(0.01f)] public float MaximumAttachErrorMeters = 0.15f;

        [Header("Basic towed brake command (no pneumatic brake dynamics)")]
        [Range(0f, 2f)] public float TrailerServiceBrakeMultiplier = 1f;
        public bool FollowTractorParkingBrake = true;
    }
}
