using NUnit.Framework;
using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEngine;

namespace Progesor.Kavra.Lab.Tests
{
    /// <summary>
    /// P1 data safety: malformed imported/edited ScriptableObjects must fail
    /// before a Rigidbody/prefab is constructed. No assets are saved here.
    /// </summary>
    public sealed class KavraDefinitionValidationEditModeTests
    {
        private static bool HasErrors(VehicleDefinition definition)
        {
            foreach (VehicleValidationIssue issue in
                VehicleConfigurationValidator.Validate(definition))
                if (issue.Severity == ValidationSeverity.Error) return true;
            return false;
        }

        [TestCase(VehicleTemplate.ThreeWheelDelta)]
        [TestCase(VehicleTemplate.PassengerCar4)]
        [TestCase(VehicleTemplate.Truck6x6)]
        [TestCase(VehicleTemplate.Truck8x8)]
        [TestCase(VehicleTemplate.SingleChassis20WheelStressTest)]
        public void ExistingTemplatesStillPassStrictSourceValidation(VehicleTemplate preset)
        {
            VehicleDefinition config = VehicleTemplateFactory.Create(preset);
            try
            {
                Assert.IsFalse(HasErrors(config), preset.ToString());
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void NonfiniteMassOrCenterOfMassIsRejectedBeforeBuilderRuns()
        {
            var config = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            try
            {
                Assert.IsFalse(HasErrors(config));
                config.ChassisMassKg = float.NaN;
                Assert.IsTrue(HasErrors(config));
                Assert.Throws<System.InvalidOperationException>(
                    () => VehicleScaffoldBuilder.Create(config, true,
                        VehicleDriveSource.LegacyDirectWheelTorque, false));
                config.ChassisMassKg = float.PositiveInfinity;
                Assert.IsTrue(HasErrors(config));

                config.ChassisMassKg = 1200f;
                config.CenterOfMassLocal = new Vector3(0f, float.NaN, 0f);
                Assert.IsTrue(HasErrors(config));
                config.CenterOfMassLocal = new Vector3(float.NegativeInfinity, 0f, 0f);
                Assert.IsTrue(HasErrors(config));
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void NonfiniteWheelMeasurementsNeverReachPhysicsSolver()
        {
            var config = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            try
            {
                WheelDefinition wheel = config.Wheels[0];
                float radius = wheel.RadiusMeters;
                float width = wheel.WidthMeters;
                float travel = wheel.SuspensionTravelMeters;
                Vector3 mount = wheel.MountLocalPosition;
                Assert.IsFalse(HasErrors(config));
                wheel.RadiusMeters = float.NaN;
                Assert.IsTrue(HasErrors(config));
                wheel.RadiusMeters = radius;
                wheel.WidthMeters = float.PositiveInfinity;
                Assert.IsTrue(HasErrors(config));
                wheel.WidthMeters = width;
                wheel.SuspensionTravelMeters = float.NaN;
                Assert.IsTrue(HasErrors(config));
                wheel.SuspensionTravelMeters = travel;
                wheel.MountLocalPosition = new Vector3(float.NegativeInfinity, 0f, 0f);
                Assert.IsTrue(HasErrors(config));
                wheel.MountLocalPosition = mount;
                Assert.IsFalse(HasErrors(config));
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void StableIdsAndUnsupportedEnumValuesAreRejectedWithoutRewritingData()
        {
            var config = VehicleTemplateFactory.Create(VehicleTemplate.Truck8x8);
            try
            {
                AxleDefinition axle = config.Axles[0];
                WheelDefinition wheel = config.Wheels[0];
                string originalAxleId = axle.Id;
                string originalWheelId = wheel.Id;
                Assert.IsFalse(HasErrors(config));

                axle.Id = originalAxleId + " ";
                Assert.IsTrue(HasErrors(config));
                Assert.AreEqual(originalAxleId + " ", axle.Id,
                    "Validator must not auto-migrate user-authored IDs.");
                axle.Id = originalAxleId;

                wheel.Id = " " + originalWheelId;
                Assert.IsTrue(HasErrors(config));
                wheel.Id = originalWheelId;

                AxleDifferentialMode originalMode = axle.DifferentialMode;
                axle.DifferentialMode = (AxleDifferentialMode)777;
                Assert.IsTrue(HasErrors(config));
                axle.DifferentialMode = originalMode;

                WheelSide originalSide = wheel.Side;
                wheel.Side = (WheelSide)777;
                Assert.IsTrue(HasErrors(config));
                wheel.Side = originalSide;

                Assert.IsFalse(HasErrors(config));
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void NonfinitePowertrainValuesAreRejectedAndLegacyNullRemainsValid()
        {
            var config = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            var powertrain = VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain();
            try
            {
                Assert.IsFalse(HasErrors(config),
                    "M0 vehicle without Powertrain must remain valid.");
                config.Powertrain = powertrain;
                Assert.IsFalse(HasErrors(config));

                float originalRatio = powertrain.ForwardGearRatios[0];
                powertrain.ForwardGearRatios[0] = float.NaN;
                Assert.IsTrue(HasErrors(config));
                powertrain.ForwardGearRatios[0] = originalRatio;

                float oldEfficiency = powertrain.Efficiency;
                powertrain.Efficiency = float.PositiveInfinity;
                Assert.IsTrue(HasErrors(config));
                powertrain.Efficiency = oldEfficiency;

                float oldIdle = powertrain.IdleRpm;
                powertrain.IdleRpm = float.NaN;
                Assert.IsTrue(HasErrors(config));
                powertrain.IdleRpm = oldIdle;

                float oldResponse = powertrain.EngineRpmResponsePerSecond;
                powertrain.EngineRpmResponsePerSecond = float.NaN;
                Assert.IsTrue(HasErrors(config));
                powertrain.EngineRpmResponsePerSecond = oldResponse;

                float oldShiftTime = powertrain.ShiftDurationSeconds;
                powertrain.ShiftDurationSeconds = float.NegativeInfinity;
                Assert.IsTrue(HasErrors(config));
                powertrain.ShiftDurationSeconds = oldShiftTime;

                float oldAutoRpm = powertrain.AutomaticDownshiftRpm;
                powertrain.AutomaticDownshiftRpm = float.PositiveInfinity;
                Assert.IsTrue(HasErrors(config));
                powertrain.AutomaticDownshiftRpm = oldAutoRpm;

                Assert.IsFalse(HasErrors(config));
                config.Powertrain = null;
                Assert.IsFalse(HasErrors(config));
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(powertrain);
            }
        }

        [Test]
        public void AssemblyAndChainValidationPropagateInvalidChassisData()
        {
            VehicleDefinition tractor = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            VehicleDefinition a = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
            VehicleDefinition b = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
            var first = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            var second = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            var chain = ScriptableObject.CreateInstance<VehicleAssemblyChainDefinition>();
            try
            {
                first.Tractor = tractor;
                first.Trailer = a;
                second.Tractor = a;
                second.Trailer = b;
                chain.Links.Add(first);
                chain.Links.Add(second);
                Assert.IsFalse(VehicleAssemblyValidator.HasErrors(first));
                Assert.IsFalse(VehicleAssemblyChainValidator.HasErrors(chain));

                a.ChassisMassKg = float.NaN;
                Assert.IsTrue(VehicleAssemblyValidator.HasErrors(first));
                Assert.IsTrue(VehicleAssemblyValidator.HasErrors(second));
                Assert.IsTrue(VehicleAssemblyChainValidator.HasErrors(chain));
            }
            finally
            {
                Object.DestroyImmediate(chain);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(tractor);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
        [Test]
        public void EditorBuilderOriginalMethodSignaturesRemainDelegateCompatible()
        {
            // Optional parameters alone change the method group signature.
            // Keep the original overloads for external Unity Editor clients.
            System.Func<VehicleDefinition, bool, VehicleDriveSource, GameObject>
                previousScaffold = VehicleScaffoldBuilder.Create;
            System.Func<VehicleDefinition, bool, VehicleDriveSource, bool, GameObject>
                temporaryScaffold = VehicleScaffoldBuilder.Create;
            System.Func<VehicleAssemblyDefinition, GameObject>
                previousTwoBody = VehicleAssemblyBuilder.Build;
            System.Func<VehicleAssemblyDefinition, bool, GameObject>
                temporaryTwoBody = VehicleAssemblyBuilder.Build;
            System.Func<VehicleAssemblyChainDefinition, GameObject>
                previousChain = VehicleAssemblyChainBuilder.Build;
            System.Func<VehicleAssemblyChainDefinition, bool, GameObject>
                temporaryChain = VehicleAssemblyChainBuilder.Build;
            System.Func<GameObject, bool, int>
                previousRig = VehicleWheelVisualRigBuilder.Bind;
            System.Func<GameObject, bool, bool, int>
                temporaryRig = VehicleWheelVisualRigBuilder.Bind;

            Assert.IsNotNull(previousScaffold);
            Assert.IsNotNull(temporaryScaffold);
            Assert.IsNotNull(previousTwoBody);
            Assert.IsNotNull(temporaryTwoBody);
            Assert.IsNotNull(previousChain);
            Assert.IsNotNull(temporaryChain);
            Assert.IsNotNull(previousRig);
            Assert.IsNotNull(temporaryRig);
            // This is compile-time API coverage, not a scene mutation.
        }

    }
}
