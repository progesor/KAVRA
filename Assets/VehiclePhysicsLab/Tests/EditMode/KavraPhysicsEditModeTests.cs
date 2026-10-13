using NUnit.Framework;
using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEngine;

namespace Progesor.Kavra.Lab.Tests
{
    public sealed class KavraPhysicsEditModeTests
    {
        [TestCase(VehicleTemplate.ThreeWheelDelta, 3)]
        [TestCase(VehicleTemplate.PassengerCar4, 4)]
        [TestCase(VehicleTemplate.Truck6x6, 6)]
        [TestCase(VehicleTemplate.Truck8x8, 8)]
        [TestCase(VehicleTemplate.SingleChassis20WheelStressTest, 20)]
        public void TemplatesHaveNoFixedFourWheelAssumption(VehicleTemplate kind, int expectedWheels)
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(kind);
            try
            {
                Assert.AreEqual(expectedWheels, definition.Wheels.Count);
                foreach (VehicleValidationIssue issue in VehicleConfigurationValidator.Validate(definition))
                    Assert.AreNotEqual(ValidationSeverity.Error, issue.Severity, issue.Message);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SuspensionStaticReferenceLoadMatchesMassTimesGravity()
        {
            const float massKg = 1450f;
            const float gravity = 9.81f;
            const float travel = 0.25f;
            const float radius = 0.36f;
            const float compression = 0.125f;
            float stiffness = massKg * gravity / (4f * compression);

            SuspensionSample sample = SuspensionSolver.Evaluate(
                true, radius, travel, radius + travel - compression,
                stiffness, 0f, 0f, compression, true, 0.02f);

            Assert.That(sample.CompressionMeters, Is.EqualTo(compression).Within(1e-5f));
            Assert.That(4f * sample.TotalForceN, Is.EqualTo(massKg * gravity).Within(0.02f));
        }

        [Test]
        public void SuspensionCanOnlyPushAndAirborneWheelHasNoForce()
        {
            SuspensionSample rebound = SuspensionSolver.Evaluate(
                true, 0.4f, 0.3f, 0.69f, 1000f, 2000f, 2000f,
                0.3f, true, 0.02f);
            Assert.That(rebound.TotalForceN, Is.EqualTo(0f));

            SuspensionSample airborne = SuspensionSolver.Evaluate(
                false, 0.4f, 0.3f, 0f, 1000f, 1000f, 1000f,
                0.15f, true, 0.02f);
            Assert.That(airborne.TotalForceN, Is.EqualTo(0f));
        }

        [Test]
        public void AirborneTireHasNoRoadForceButCanSpin()
        {
            var p = new WheelDynamicsInput
            {
                AngularVelocityRadPerSec = 0f,
                LongitudinalSpeedMps = 0f,
                LateralSpeedMps = 4f,
                NormalLoadN = 0f,
                WheelRadiusMeters = 0.4f,
                InertiaKgM2 = 2f,
                DriveTorqueNm = 100f,
                BrakeTorqueNm = 0f,
                LongitudinalStiffnessNsPerMeter = 6000f,
                LateralStiffnessNsPerMeter = 7000f,
                LongitudinalFriction = 1f,
                LateralFriction = 1f,
                DeltaTime = 0.02f
            };
            WheelDynamicsResult result = WheelDynamicsSolver.Step(p);
            Assert.AreEqual(0f, result.LongitudinalForceN);
            Assert.AreEqual(0f, result.LateralForceN);
            Assert.Greater(result.AngularVelocityRadPerSec, 0f);
        }

        [Test]
        public void TireForcesRespectCombinedFrictionEllipse()
        {
            var p = new WheelDynamicsInput
            {
                AngularVelocityRadPerSec = 80f,
                LongitudinalSpeedMps = 4f,
                LateralSpeedMps = 15f,
                NormalLoadN = 4000f,
                WheelRadiusMeters = 0.36f,
                InertiaKgM2 = 1.6f,
                DriveTorqueNm = 500f,
                LongitudinalStiffnessNsPerMeter = 9000f,
                LateralStiffnessNsPerMeter = 12000f,
                LongitudinalFriction = 0.8f,
                LateralFriction = 1.1f,
                DeltaTime = 0.016f
            };
            WheelDynamicsResult result = WheelDynamicsSolver.Step(p);
            float x = result.LongitudinalForceN / (p.LongitudinalFriction * p.NormalLoadN);
            float y = result.LateralForceN / (p.LateralFriction * p.NormalLoadN);
            Assert.LessOrEqual(x * x + y * y, 1.0001f);
            Assert.IsFalse(float.IsNaN(result.AngularVelocityRadPerSec));
        }

        [Test]
        public void BrakeAtStandstillCannotCreateReverseSpin()
        {
            var p = new WheelDynamicsInput
            {
                WheelRadiusMeters = 0.4f,
                InertiaKgM2 = 2f,
                BrakeTorqueNm = 800f,
                NormalLoadN = 3200f,
                LongitudinalStiffnessNsPerMeter = 6500f,
                LongitudinalFriction = 1f,
                LateralFriction = 1f,
                DeltaTime = 0.016f
            };
            WheelDynamicsResult result = WheelDynamicsSolver.Step(p);
            Assert.That(result.AngularVelocityRadPerSec, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void RaycastExcludesOwnBodyAndFindsGround()
        {
            GameObject vehicle = new GameObject("test-vehicle");
            GameObject ownCollider = null;
            GameObject ground = null;
            try
            {
                Rigidbody body = vehicle.AddComponent<Rigidbody>();
                body.isKinematic = true;

                ownCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ownCollider.transform.SetParent(vehicle.transform);
                ownCollider.transform.position = new Vector3(0f, 1.25f, 0f);
                ownCollider.transform.localScale = new Vector3(2f, 0.3f, 2f);
                ownCollider.layer = 30;

                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.transform.position = new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale = new Vector3(5f, 1f, 5f);
                ground.layer = 30;
                Physics.SyncTransforms();

                var hits = new RaycastHit[16];
                RaycastHit nearest;
                bool found = WheelContactProbe.TryRaycast(
                    new Vector3(0f, 2f, 0f), Vector3.down, 3f, 1 << 30,
                    vehicle.transform, body, hits, out nearest);
                Assert.IsTrue(found);
                Assert.AreEqual(ground.GetComponent<Collider>(), nearest.collider);
            }
            finally
            {
                if (ground != null) Object.DestroyImmediate(ground);
                if (ownCollider != null) Object.DestroyImmediate(ownCollider);
                Object.DestroyImmediate(vehicle);
            }
        }
    }
}
