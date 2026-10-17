using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// PhysicsLab demo templates. Real imported vehicle geometry is not
    /// needed to prove multi-Rigidbody hitch/wheel behavior.
    /// </summary>
    public static class VehicleAssemblyTemplateFactory
    {
        public static VehicleDefinition CreateFourWheelTrailer()
        {
            VehicleDefinition trailer = ScriptableObject.CreateInstance<VehicleDefinition>();
            trailer.VehicleName = "M1.3 Four-Wheel Towed Trailer";
            trailer.ChassisMassKg = 3400f;
            trailer.CenterOfMassLocal = new Vector3(0f, -0.25f, -1.5f);
            AddAxle(trailer, "bogie-front", -0.75f);
            AddAxle(trailer, "bogie-rear", -2.25f);
            return trailer;
        }

        public static SimplePowertrainDefinition CreateTruckSamplePowertrain()
        {
            var powertrain = ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            powertrain.IdleRpm = 650f;
            powertrain.MaxRpm = 3100f;
            powertrain.EngineRpmResponsePerSecond = 900f;
            powertrain.LaunchFreeRevFraction = 0.22f;
            powertrain.TorqueNmByRpm = new AnimationCurve(
                new Keyframe(650f, 650f),
                new Keyframe(1100f, 950f),
                new Keyframe(1900f, 1150f),
                new Keyframe(3000f, 800f));
            powertrain.ForwardGearRatios = new[] { 8.3f, 4.6f, 2.7f, 1.6f, 1f };
            powertrain.ReverseGearRatio = 7.8f;
            powertrain.FinalDriveRatio = 4.4f;
            powertrain.Efficiency = 0.83f;
            powertrain.AutomaticUpshiftRpm = 2700f;
            powertrain.AutomaticDownshiftRpm = 1200f;
            return powertrain;
        }

        private static void AddAxle(VehicleDefinition trailer, string id, float z)
        {
            trailer.Axles.Add(new AxleDefinition
            {
                Id = id, Linkage = AxleLinkage.Bogie, IsSteered = false,
                IsDriven = false
            });
            for (int i = 0; i < 2; i++)
            {
                bool left = i == 0;
                trailer.Wheels.Add(new WheelDefinition
                {
                    Id = id + (left ? "-L" : "-R"),
                    AxleId = id,
                    Side = left ? WheelSide.Left : WheelSide.Right,
                    MountLocalPosition = new Vector3(left ? -1.05f : 1.05f, -0.85f, z),
                    RadiusMeters = 0.55f,
                    WidthMeters = 0.42f,
                    SuspensionTravelMeters = 0.36f,
                    CanDrive = false,
                    CanSteer = false,
                    HasServiceBrake = true,
                    HasParkingBrake = true
                });
            }
        }
    }
}
