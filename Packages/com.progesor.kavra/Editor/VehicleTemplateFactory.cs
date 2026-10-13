using UnityEngine;

namespace Progesor.Kavra.Editor
{
    public enum VehicleTemplate
    {
        ThreeWheelDelta,
        PassengerCar4,
        Truck6x6,
        Truck8x8,
        SingleChassis20WheelStressTest
    }

    internal static class VehicleTemplateFactory
    {
        public static VehicleDefinition Create(VehicleTemplate kind)
        {
            var vehicle = ScriptableObject.CreateInstance<VehicleDefinition>();
            switch (kind)
            {
                case VehicleTemplate.ThreeWheelDelta:
                    vehicle.VehicleName = "3W Delta (single front wheel)";
                    vehicle.ChassisMassKg = 650f;
                    AddAxle(vehicle, "front", true, false, 1.1f, 0f, 0.32f, false);
                    AddAxle(vehicle, "rear", false, true, -1.1f, 1.35f, 0.35f, false);
                    break;
                case VehicleTemplate.PassengerCar4:
                    vehicle.VehicleName = "Passenger Car 4W";
                    vehicle.ChassisMassKg = 1450f;
                    AddAxle(vehicle, "front", true, false, 1.35f, 1.58f, 0.36f, false);
                    AddAxle(vehicle, "rear", false, true, -1.35f, 1.58f, 0.36f, false);
                    break;
                case VehicleTemplate.Truck6x6:
                    vehicle.VehicleName = "Truck 6x6";
                    vehicle.ChassisMassKg = 7600f;
                    AddAxle(vehicle, "front", true, true, 2.0f, 2.1f, 0.55f, true);
                    AddAxle(vehicle, "middle", false, true, -0.65f, 2.1f, 0.55f, true);
                    AddAxle(vehicle, "rear", false, true, -2.0f, 2.1f, 0.55f, true);
                    break;
                case VehicleTemplate.Truck8x8:
                    vehicle.VehicleName = "Truck 8x8";
                    vehicle.ChassisMassKg = 11500f;
                    AddAxle(vehicle, "front1", true, true, 3.0f, 2.2f, 0.58f, true);
                    AddAxle(vehicle, "front2", true, true, 1.6f, 2.2f, 0.58f, true);
                    AddAxle(vehicle, "rear1", false, true, -1.4f, 2.2f, 0.58f, true);
                    AddAxle(vehicle, "rear2", false, true, -2.8f, 2.2f, 0.58f, true);
                    break;
                default:
                    vehicle.VehicleName = "20W single-chassis scaffold (NOT a tractor-trailer)";
                    vehicle.ChassisMassKg = 30000f;
                    for (var i = 0; i < 10; i++)
                        AddAxle(vehicle, "axle" + (i + 1).ToString("00"), i < 2, i >= 2,
                            6.0f - i * 1.35f, 2.5f, 0.60f, true);
                    break;
            }
            vehicle.CenterOfMassLocal = new Vector3(0f, -0.25f, 0f);
            return vehicle;
        }

        private static void AddAxle(VehicleDefinition vehicle, string id, bool steer, bool drive,
            float z, float trackWidth, float radius, bool heavy)
        {
            vehicle.Axles.Add(new AxleDefinition
            {
                Id = id, Linkage = heavy ? AxleLinkage.Rigid : AxleLinkage.Independent,
                IsSteered = steer, IsDriven = drive
            });
            var width = heavy ? 0.42f : 0.27f;
            var travel = heavy ? 0.36f : 0.25f;
            var mountY = heavy ? -0.85f : -0.63f;
            if (trackWidth <= 0.01f)
            {
                vehicle.Wheels.Add(new WheelDefinition
                {
                    Id = id + "-C", AxleId = id, Side = WheelSide.Center,
                    MountLocalPosition = new Vector3(0f, mountY, z),
                    RadiusMeters = radius, WidthMeters = width,
                    SuspensionTravelMeters = travel,
                    CanSteer = steer, CanDrive = drive, HasParkingBrake = !steer
                });
                return;
            }
            AddWheel(vehicle, id, "L", WheelSide.Left, -trackWidth * 0.5f, z,
                mountY, radius, width, travel, steer, drive);
            AddWheel(vehicle, id, "R", WheelSide.Right, trackWidth * 0.5f, z,
                mountY, radius, width, travel, steer, drive);
        }

        private static void AddWheel(VehicleDefinition vehicle, string axleId, string suffix,
            WheelSide side, float x, float z, float y, float radius, float width,
            float travel, bool steer, bool drive)
        {
            vehicle.Wheels.Add(new WheelDefinition
            {
                Id = axleId + "-" + suffix, AxleId = axleId, Side = side,
                MountLocalPosition = new Vector3(x, y, z),
                RadiusMeters = radius, WidthMeters = width,
                SuspensionTravelMeters = travel,
                CanSteer = steer, CanDrive = drive, HasParkingBrake = !steer
            });
        }
    }
}
