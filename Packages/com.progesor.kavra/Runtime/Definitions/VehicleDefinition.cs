using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    public enum WheelSide { Left, Center, Right }
    public enum AxleLinkage { Independent, Rigid, Bogie, Virtual }

    [Serializable]
    public sealed class AxleDefinition
    {
        public string Id = "axle-01";
        public AxleLinkage Linkage = AxleLinkage.Independent;
        public bool IsSteered;
        public bool IsDriven;
        public string Notes = "";
    }

    [Serializable]
    public sealed class WheelDefinition
    {
        public string Id = "wheel-01";
        public string AxleId = "axle-01";
        public WheelSide Side;
        // Vehicle convention: +X right, +Y up, +Z forward; metres.
        public Vector3 MountLocalPosition;
        [Min(0.01f)] public float RadiusMeters = 0.38f;
        [Min(0.01f)] public float WidthMeters = 0.28f;
        [Min(0f)] public float SuspensionTravelMeters = 0.25f;
        [Tooltip("Optional per-wheel tuning asset. Null keeps vehicle-wide tire defaults.")]
        public TireTuningProfile TireProfile;
        public bool CanSteer;
        public bool CanDrive;
        public bool HasServiceBrake = true;
        public bool HasParkingBrake;
    }

    /// <summary>
    /// Asset for one rigid chassis. Articulated combinations will use multiple
    /// chassis assets in a future VehicleAssembly; do not flatten a tractor-trailer
    /// into one Rigidbody just because its wheel count is high.
    /// </summary>
    [CreateAssetMenu(fileName = "VehicleDefinition", menuName = "KAVRA/Vehicle Definition")]
    public sealed class VehicleDefinition : ScriptableObject
    {
        public string VehicleName = "Untitled Vehicle";
        [Min(1f)] public float ChassisMassKg = 1200f;
        public Vector3 CenterOfMassLocal = new Vector3(0f, -0.25f, 0f);
        public List<AxleDefinition> Axles = new List<AxleDefinition>();
        public List<WheelDefinition> Wheels = new List<WheelDefinition>();
    }
}
