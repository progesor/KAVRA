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
        [Test]
        public void ThreeWheelStaticLoadsFollowCenterOfMass()
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(VehicleTemplate.ThreeWheelDelta);
            try
            {
                float[] loads = new float[definition.Wheels.Count];
                bool balanced = VehicleStaticLoadEstimator.Estimate(
                    definition.Wheels, definition.CenterOfMassLocal, loads);

                Assert.IsTrue(balanced, "The three-wheel footprint encloses the CoM");
                Assert.That(loads[0], Is.EqualTo(0.5f).Within(0.0005f));
                Assert.That(loads[1], Is.EqualTo(0.25f).Within(0.0005f));
                Assert.That(loads[2], Is.EqualTo(0.25f).Within(0.0005f));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [TestCase(VehicleTemplate.PassengerCar4)]
        [TestCase(VehicleTemplate.Truck6x6)]
        [TestCase(VehicleTemplate.Truck8x8)]
        [TestCase(VehicleTemplate.SingleChassis20WheelStressTest)]
        public void NominalLoadsBalanceLateralAndLongitudinalMoments(VehicleTemplate kind)
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(kind);
            try
            {
                float[] fractions = new float[definition.Wheels.Count];
                bool balanced = VehicleStaticLoadEstimator.Estimate(
                    definition.Wheels, definition.CenterOfMassLocal, fractions);
                Assert.IsTrue(balanced, "CoM support for " + kind);

                float total = 0f, x = 0f, z = 0f;
                for (int i = 0; i < fractions.Length; i++)
                {
                    Assert.GreaterOrEqual(fractions[i], 0f);
                    total += fractions[i];
                    x += fractions[i] * definition.Wheels[i].MountLocalPosition.x;
                    z += fractions[i] * definition.Wheels[i].MountLocalPosition.z;
                }
                Assert.That(total, Is.EqualTo(1f).Within(0.0005f));
                Assert.That(x, Is.EqualTo(definition.CenterOfMassLocal.x).Within(0.025f));
                Assert.That(z, Is.EqualTo(definition.CenterOfMassLocal.z).Within(0.025f));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ImpossibleCenterOfMassIsReportedAndLoadsStayNonNegative()
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            try
            {
                float[] fractions = new float[definition.Wheels.Count];
                bool balanced = VehicleStaticLoadEstimator.Estimate(
                    definition.Wheels, new Vector3(20f, 0f, 0f), fractions);
                Assert.IsFalse(balanced);
                float sum = 0f;
                foreach (float f in fractions)
                {
                    Assert.GreaterOrEqual(f, 0f);
                    sum += f;
                }
                Assert.That(sum, Is.EqualTo(1f).Within(0.0005f));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BumpStopAddsProgressiveSupportAfterBottomOut()
        {
            const float radius = 0.36f;
            const float travel = 0.25f;
            const float stiffness = 28000f;
            SuspensionSample mid = SuspensionSolver.Evaluate(
                true, radius, travel, radius + travel - 0.125f,
                stiffness, 0f, 0f, 0.125f, true, 0.016f, 0.85f, 4f);
            SuspensionSample bottomed = SuspensionSolver.Evaluate(
                true, radius, travel, radius - 0.03f,
                stiffness, 0f, 0f, travel + 0.03f, true, 0.016f, 0.85f, 4f);

            Assert.That(mid.BumpStopForceN, Is.EqualTo(0f));
            Assert.That(bottomed.BottomOutMeters, Is.EqualTo(0.03f).Within(0.0001f));
            Assert.Greater(bottomed.BumpStopForceN, 0f);
            Assert.Greater(bottomed.TotalForceN, mid.TotalForceN);
        }

        [Test]
        public void AntiRollTransferConservesSupportAndCannotPullWheelDown()
        {
            const float leftSpring = 1100f;
            const float rightSpring = 300f;
            float transfer = AntiRollSolver.ComputeTransferN(
                0.22f, 0.10f, leftSpring, rightSpring, 9000f);

            float left = leftSpring + transfer;
            float right = rightSpring - transfer;
            Assert.GreaterOrEqual(left, 0f);
            Assert.GreaterOrEqual(right, 0f);
            Assert.That(left + right, Is.EqualTo(leftSpring + rightSpring).Within(0.0001f));
            Assert.That(transfer, Is.EqualTo(rightSpring));
        }

        [TestCase(WheelContactMode.Raycast)]
        [TestCase(WheelContactMode.SphereCastWithRayFallback)]
        public void WheelContactKeepsDistanceAtStaticRideHeight(WheelContactMode mode)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                road.transform.position = new Vector3(0f, -0.5f, 0f);
                road.transform.localScale = new Vector3(10f, 1f, 10f);
                road.layer = 30;
                Physics.SyncTransforms();

                WheelContactSample sample;
                bool found = WheelContactProbe.TrySample(
                    new Vector3(0f, 0.485f, 0f), Vector3.up,
                    0.36f, 0.25f, mode, 1 << 30,
                    null, null, new RaycastHit[16], out sample);
                Assert.IsTrue(found, "Contact for " + mode);
                Assert.That(sample.RayEquivalentDistanceMeters,
                    Is.EqualTo(0.485f).Within(0.004f));
            }
            finally
            {
                Object.DestroyImmediate(road);
            }
        }

        [Test]
        public void RaisedProbeCanDetectContactAfterMountCrossesRoadSurface()
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                road.transform.position = new Vector3(0f, -0.5f, 0f);
                road.transform.localScale = new Vector3(10f, 1f, 10f);
                road.layer = 30;
                Physics.SyncTransforms();

                WheelContactSample sample;
                bool found = WheelContactProbe.TrySample(
                    new Vector3(0f, -0.03f, 0f), Vector3.up,
                    0.36f, 0.25f, WheelContactMode.SphereCastWithRayFallback,
                    1 << 30, null, null, new RaycastHit[16], out sample);
                Assert.IsTrue(found, "Wheel mount has gone 3 cm into ground");
                Assert.Less(sample.RayEquivalentDistanceMeters, 0f);
                SuspensionSample suspension = SuspensionSolver.Evaluate(
                    true, 0.36f, 0.25f, sample.RayEquivalentDistanceMeters,
                    20000f, 0f, 0f, 0.64f, true, 0.016f, 0.85f, 4f);
                Assert.Greater(suspension.BottomOutMeters, 0.36f);
                Assert.Greater(suspension.TotalForceN, 0f);
            }
            finally
            {
                Object.DestroyImmediate(road);
            }
        }

    }
}
