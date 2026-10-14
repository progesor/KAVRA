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

        [Test]
        public void MissingSurfaceProfilePreservesOriginalTireCoefficients()
        {
            SurfaceTireParameters result = SurfaceResponse.Evaluate(null, 1.05f, 0.98f, 0.014f);
            Assert.That(result.LongitudinalFriction, Is.EqualTo(1.05f).Within(0.00001f));
            Assert.That(result.LateralFriction, Is.EqualTo(0.98f).Within(0.00001f));
            Assert.That(result.RollingResistanceCoefficient,
                Is.EqualTo(0.014f).Within(0.00001f));
        }

        [Test]
        public void WetAndSoftMetadataCannotApplyUnintendedExtraGripPenalty()
        {
            SurfaceProfile profile = ScriptableObject.CreateInstance<SurfaceProfile>();
            try
            {
                profile.LongitudinalGripMultiplier = 0.4f;
                profile.LateralGripMultiplier = 0.3f;
                profile.RollingResistanceMultiplier = 5f;
                profile.Softness = 0.98f;
                profile.Wetness = 1f;

                SurfaceTireParameters result = SurfaceResponse.Evaluate(
                    profile, 1.05f, 1.1f, 0.014f);
                Assert.That(result.LongitudinalFriction, Is.EqualTo(0.42f).Within(0.00001f));
                Assert.That(result.LateralFriction, Is.EqualTo(0.33f).Within(0.00001f));
                Assert.That(result.RollingResistanceCoefficient,
                    Is.EqualTo(0.07f).Within(0.00001f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SurfaceProfileIsResolvedFromHitColliderParent()
        {
            GameObject road = new GameObject("SurfaceRoad");
            GameObject geometry = new GameObject("ColliderChild");
            SurfaceProfile profile = ScriptableObject.CreateInstance<SurfaceProfile>();
            try
            {
                SurfaceZone zone = road.AddComponent<SurfaceZone>();
                zone.Profile = profile;
                geometry.transform.SetParent(road.transform, false);
                Collider roadCollider = geometry.AddComponent<BoxCollider>();

                Assert.AreSame(profile, SurfaceResponse.Resolve(roadCollider));
                Assert.IsNull(SurfaceResponse.Resolve(null));
            }
            finally
            {
                Object.DestroyImmediate(road);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SurfaceFrictionCapChangesWithGroundType()
        {
            SurfaceProfile slippery = ScriptableObject.CreateInstance<SurfaceProfile>();
            try
            {
                slippery.LongitudinalGripMultiplier = 0.2f;
                slippery.LateralGripMultiplier = 0.2f;
                SurfaceTireParameters dry = SurfaceResponse.Evaluate(null, 1f, 1f, 0.01f);
                SurfaceTireParameters ice = SurfaceResponse.Evaluate(slippery, 1f, 1f, 0.01f);

                var input = new WheelDynamicsInput
                {
                    AngularVelocityRadPerSec = 80f,
                    LongitudinalSpeedMps = 3f,
                    NormalLoadN = 4000f,
                    WheelRadiusMeters = 0.36f,
                    InertiaKgM2 = 2f,
                    DriveTorqueNm = 1000f,
                    LongitudinalStiffnessNsPerMeter = 18000f,
                    LateralStiffnessNsPerMeter = 8000f,
                    LateralFriction = 1f,
                    DeltaTime = 0.02f
                };

                input.LongitudinalFriction = dry.LongitudinalFriction;
                WheelDynamicsResult dryResult = WheelDynamicsSolver.Step(input);
                input.LongitudinalFriction = ice.LongitudinalFriction;
                WheelDynamicsResult iceResult = WheelDynamicsSolver.Step(input);

                Assert.LessOrEqual(Mathf.Abs(iceResult.LongitudinalForceN),
                    ice.LongitudinalFriction * input.NormalLoadN + 0.01f);
                Assert.Greater(Mathf.Abs(dryResult.LongitudinalForceN),
                    Mathf.Abs(iceResult.LongitudinalForceN));
            }
            finally
            {
                Object.DestroyImmediate(slippery);
            }
        }

        [Test]
        public void ManualRecoveryKeepsHeadingButRemovesRollAndPitch()
        {
            Quaternion sideways = Quaternion.Euler(18f, 37f, 165f);
            Vector3 originalForward = sideways * Vector3.forward;
            Quaternion recovered = VehicleManualRecovery.UprightRotation(
                originalForward, sideways * Vector3.right);
            Vector3 heading = Vector3.ProjectOnPlane(originalForward, Vector3.up).normalized;
            Assert.That(Vector3.Dot(recovered * Vector3.up, Vector3.up),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.Greater(Vector3.Dot(recovered * Vector3.forward, heading), 0.999f);
        }

        [Test]
        public void ManualRecoveryRefusesTeleportAtSpeed()
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            GameObject car = new GameObject("TestRecoveryFast");
            try
            {
                Rigidbody rigid = car.AddComponent<Rigidbody>();
                VehicleRoot root = car.AddComponent<VehicleRoot>();
                root.Definition = definition;
                VehicleSimulationRoot simulation = car.AddComponent<VehicleSimulationRoot>();
                car.transform.position = new Vector3(0f, 3f, 0f);
                car.transform.rotation = Quaternion.Euler(0f, 0f, 170f);
                rigid.linearVelocity = new Vector3(6f, 0f, 0f);
                Vector3 beforePosition = rigid.position;
                Quaternion beforeRotation = rigid.rotation;

                VehicleRecoveryResult result = VehicleManualRecovery.TryRecover(simulation);

                Assert.AreEqual(VehicleRecoveryResult.MovingTooFast, result);
                Assert.That((rigid.position - beforePosition).sqrMagnitude,
                    Is.LessThan(0.00001f));
                Assert.That(Quaternion.Angle(rigid.rotation, beforeRotation),
                    Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(car);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ManualRecoveryUprightsSlowOverturnedVehicleAboveGround()
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            GameObject car = new GameObject("TestRecoveryStopped");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(20f, 1f, 20f);
                floor.layer = 30;

                Rigidbody rigid = car.AddComponent<Rigidbody>();
                VehicleRoot root = car.AddComponent<VehicleRoot>();
                root.Definition = definition;
                VehicleSimulationRoot simulation = car.AddComponent<VehicleSimulationRoot>();
                simulation.GroundLayers = 1 << 30;
                car.transform.position = new Vector3(0f, 0.8f, 0f);
                car.transform.rotation = Quaternion.Euler(0f, 0f, 172f);
                rigid.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();

                VehicleRecoveryResult result = VehicleManualRecovery.TryRecover(simulation);

                Assert.AreEqual(VehicleRecoveryResult.Success, result);
                Assert.Greater(rigid.position.y, 1.3f);
                Assert.Greater(Vector3.Dot(rigid.rotation * Vector3.up, Vector3.up), 0.999f);
                Assert.That(rigid.linearVelocity.sqrMagnitude, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(car);
                Object.DestroyImmediate(definition);
            }
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void AckermannInnerFrontWheelTurnsSharperThanOuter(float direction)
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            try
            {
                SteeringGeometryParameters geometry =
                    SteeringGeometrySolver.Analyze(definition.Wheels);
                Assert.IsTrue(geometry.HasGeometry);
                WheelDefinition left = definition.Wheels[0];
                WheelDefinition right = definition.Wheels[1];
                float leftAngle = SteeringGeometrySolver.CalculateAngleDegrees(
                    geometry, left, direction, 28f, SteeringGeometryMode.ApproximateAckermann);
                float rightAngle = SteeringGeometrySolver.CalculateAngleDegrees(
                    geometry, right, direction, 28f, SteeringGeometryMode.ApproximateAckermann);

                Assert.That(Mathf.Sign(leftAngle), Is.EqualTo(direction));
                Assert.That(Mathf.Sign(rightAngle), Is.EqualTo(direction));
                if (direction > 0f)
                    Assert.Greater(Mathf.Abs(rightAngle), Mathf.Abs(leftAngle));
                else
                    Assert.Greater(Mathf.Abs(leftAngle), Mathf.Abs(rightAngle));
                // Parallel mode still reproduces legacy behavior exactly.
                Assert.That(SteeringGeometrySolver.CalculateAngleDegrees(
                    geometry, left, direction, 28f, SteeringGeometryMode.Parallel),
                    Is.EqualTo(direction * 28f).Within(0.0001f));
                Assert.That(SteeringGeometrySolver.CalculateAngleDegrees(
                    geometry, right, direction, 28f, SteeringGeometryMode.Parallel),
                    Is.EqualTo(direction * 28f).Within(0.0001f));
            }
            finally { Object.DestroyImmediate(definition); }
        }

        [TestCase(VehicleTemplate.ThreeWheelDelta, 3)]
        [TestCase(VehicleTemplate.PassengerCar4, 4)]
        [TestCase(VehicleTemplate.Truck6x6, 6)]
        [TestCase(VehicleTemplate.Truck8x8, 8)]
        [TestCase(VehicleTemplate.SingleChassis20WheelStressTest, 20)]
        public void SteeringAnglesStayFiniteForVariableWheelCounts(
            VehicleTemplate kind, int wheelCount)
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(kind);
            try
            {
                SteeringGeometryParameters geometry =
                    SteeringGeometrySolver.Analyze(definition.Wheels);
                Assert.AreEqual(wheelCount, definition.Wheels.Count);
                foreach (WheelDefinition wheel in definition.Wheels)
                {
                    float steer = SteeringGeometrySolver.CalculateAngleDegrees(
                        geometry, wheel, 0.6f, 35f, SteeringGeometryMode.ApproximateAckermann);
                    Assert.IsFalse(float.IsNaN(steer) || float.IsInfinity(steer));
                    Assert.LessOrEqual(Mathf.Abs(steer), 85f);
                    if (!wheel.CanSteer)
                        Assert.That(steer, Is.EqualTo(0f));
                }
            }
            finally { Object.DestroyImmediate(definition); }
        }

        [Test]
        public void ThreeWheeledDeltaSingleFrontSteeringKeepsCenterAngle()
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(VehicleTemplate.ThreeWheelDelta);
            try
            {
                float angle = SteeringGeometrySolver.CalculateAngleDegrees(
                    definition.Wheels, definition.Wheels[0], 1f, 24f,
                    SteeringGeometryMode.ApproximateAckermann);
                Assert.That(angle, Is.EqualTo(24f).Within(0.0001f));
            }
            finally { Object.DestroyImmediate(definition); }
        }

        [Test]
        public void RearOnlySteeringTurnsWheelsOppositeDriverYawRequest()
        {
            VehicleDefinition definition = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            try
            {
                // Simulate a forklift: front axle does not steer, rear axle does.
                definition.Wheels[0].CanSteer = false;
                definition.Wheels[1].CanSteer = false;
                definition.Wheels[2].CanSteer = true;
                definition.Wheels[3].CanSteer = true;
                SteeringGeometryParameters geometry =
                    SteeringGeometrySolver.Analyze(definition.Wheels);
                float rearLeft = SteeringGeometrySolver.CalculateAngleDegrees(
                    geometry, definition.Wheels[2], 1f, 25f,
                    SteeringGeometryMode.ApproximateAckermann);
                float rearRight = SteeringGeometrySolver.CalculateAngleDegrees(
                    geometry, definition.Wheels[3], 1f, 25f,
                    SteeringGeometryMode.ApproximateAckermann);
                Assert.Less(rearLeft, 0f);
                Assert.Less(rearRight, 0f);
                Assert.AreEqual(0f, SteeringGeometrySolver.CalculateAngleDegrees(
                    geometry, definition.Wheels[0], 1f, 25f,
                    SteeringGeometryMode.ApproximateAckermann));
            }
            finally { Object.DestroyImmediate(definition); }
        }

        [Test]
        public void SlipDiagnosticsRemainZeroAtRestAndFiniteAtLowSpeed()
        {
            TireSlipMetrics stopped = TireSlipDiagnostics.Calculate(0f, 0f, 0f, 0.36f);
            Assert.That(stopped.LongitudinalSlipRatio, Is.EqualTo(0f));
            Assert.That(stopped.SlipAngleDegrees, Is.EqualTo(0f));

            TireSlipMetrics launch = TireSlipDiagnostics.Calculate(0.01f, 0.005f, 15f, 0.36f);
            Assert.IsFalse(float.IsNaN(launch.LongitudinalSlipRatio));
            Assert.IsFalse(float.IsInfinity(launch.SlipAngleDegrees));
            Assert.Greater(launch.LongitudinalSlipRatio, 0f);
            Assert.LessOrEqual(Mathf.Abs(launch.LongitudinalSlipRatio), 2f);
            Assert.LessOrEqual(Mathf.Abs(launch.SlipAngleDegrees), 89f);
        }

        [Test]
        public void LockedWheelBrakeReportsMinusOneSlipAndPositiveLateralAngle()
        {
            TireSlipMetrics locked = TireSlipDiagnostics.Calculate(10f, 1f, 0f, 0.36f);
            Assert.That(locked.LongitudinalSlipRatio, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(locked.SlipAngleDegrees, Is.EqualTo(
                Mathf.Atan2(1f, 10f) * Mathf.Rad2Deg).Within(0.001f));
            TireSlipMetrics rolling = TireSlipDiagnostics.Calculate(
                10f, 0f, 10f / 0.36f, 0.36f);
            Assert.That(rolling.LongitudinalSlipRatio, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void TireTuningNullProfileKeepsSurfaceCoefficientsAndStiffness()
        {
            SurfaceTireParameters ground = SurfaceResponse.Evaluate(null, 1.1f, 0.95f, 0.014f);
            TireTuningValues values = TireTuningResolver.Resolve(
                null, ground, 6500f, 8000f, 25f, 0.36f);
            Assert.That(values.LongitudinalStiffnessNsPerMeter, Is.EqualTo(6500f));
            Assert.That(values.LateralStiffnessNsPerMeter, Is.EqualTo(8000f));
            Assert.That(values.LongitudinalFriction, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(values.LateralFriction, Is.EqualTo(0.95f).Within(0.0001f));
            Assert.That(values.RollingResistanceCoefficient, Is.EqualTo(0.014f).Within(0.0001f));
            Assert.That(values.InertiaKgM2, Is.EqualTo(0.5f * 25f * 0.36f * 0.36f).Within(0.0001f));
        }

        [Test]
        public void TireTuningAndSurfaceMultipliersComposeWithoutChangingSharedAsset()
        {
            TireTuningProfile tire = ScriptableObject.CreateInstance<TireTuningProfile>();
            SurfaceProfile surface = ScriptableObject.CreateInstance<SurfaceProfile>();
            try
            {
                tire.LongitudinalStiffnessMultiplier = 1.5f;
                tire.LateralStiffnessMultiplier = 0.9f;
                tire.LongitudinalGripMultiplier = 1.1f;
                tire.LateralGripMultiplier = 0.8f;
                tire.RollingResistanceMultiplier = 1.2f;
                tire.WheelInertiaMultiplier = 2f;
                surface.LongitudinalGripMultiplier = 0.5f;
                surface.LateralGripMultiplier = 0.4f;

                SurfaceTireParameters road = SurfaceResponse.Evaluate(
                    surface, 1f, 1f, 0.02f);
                TireTuningValues effective = TireTuningResolver.Resolve(
                    tire, road, 6500f, 8000f, 25f, 0.36f);

                Assert.That(effective.LongitudinalStiffnessNsPerMeter,
                    Is.EqualTo(9750f).Within(0.0001f));
                Assert.That(effective.LateralStiffnessNsPerMeter,
                    Is.EqualTo(7200f).Within(0.0001f));
                Assert.That(effective.LongitudinalFriction, Is.EqualTo(0.55f).Within(0.0001f));
                Assert.That(effective.LateralFriction, Is.EqualTo(0.32f).Within(0.0001f));
                Assert.That(effective.RollingResistanceCoefficient,
                    Is.EqualTo(0.024f).Within(0.0001f));
                Assert.That(effective.InertiaKgM2, Is.EqualTo(3.24f).Within(0.0001f));
                Assert.That(tire.LongitudinalGripMultiplier, Is.EqualTo(1.1f));
                Assert.That(surface.LongitudinalGripMultiplier, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(tire);
                Object.DestroyImmediate(surface);
            }
        }

        [Test]
        public void LowSpeedBrakingCannotReverseNominalChassisVelocityInOneBodyStep()
        {
            // 1450kg car / 4 equivalent tires at 0.08m/s, exactly the
            // near-stop regime exhibiting +/-14kN telemetry oscillation.
            const float bodyStep = 1f / 60f;
            const float wheelShareKg = 362.5f;
            const float startingSpeed = 0.08f;
            const float radius = 0.36f;

            var p = new WheelDynamicsInput
            {
                AngularVelocityRadPerSec = 0f,
                LongitudinalSpeedMps = startingSpeed,
                LateralSpeedMps = 0f,
                NormalLoadN = 3556.125f,
                WheelRadiusMeters = radius,
                InertiaKgM2 = 1.62f,
                DriveTorqueNm = 0f,
                BrakeTorqueNm = 1710f,
                LongitudinalStiffnessNsPerMeter = 6500f,
                LateralStiffnessNsPerMeter = 8000f,
                LongitudinalFriction = 1.05f,
                LateralFriction = 1.05f,
                RollingResistanceCoefficient = 0.014f,
                BrakeEffectiveMassKg = wheelShareKg,
                BodyFixedDeltaTime = bodyStep,
                LowSpeedBrakeThresholdMps = 0.5f,
                DeltaTime = bodyStep / 4f
            };

            float sumFx = 0f;
            for (int i = 0; i < 4; i++)
            {
                WheelDynamicsResult result = WheelDynamicsSolver.Step(p);
                Assert.LessOrEqual(Mathf.Abs(result.LongitudinalForceN),
                    wheelShareKg * startingSpeed / bodyStep + 0.001f);
                Assert.LessOrEqual(result.LongitudinalForceN, 0f);
                Assert.IsTrue(result.NearStopBrakeImpulseLimited);
                Assert.GreaterOrEqual(result.AngularVelocityRadPerSec, -0.001f);
                sumFx += result.LongitudinalForceN;
                p.AngularVelocityRadPerSec = result.AngularVelocityRadPerSec;
            }

            float averageWheelFx = sumFx / 4f;
            float newBodySpeed = startingSpeed +
                4f * averageWheelFx / (4f * wheelShareKg) * bodyStep;
            Assert.GreaterOrEqual(newBodySpeed, -0.00001f);
        }

        [Test]
        public void FastBrakingPreservesLegacyForceWithLimiterEnabled()
        {
            const float bodyStep = 1f / 60f;
            var input = new WheelDynamicsInput
            {
                AngularVelocityRadPerSec = 10f / 0.36f,
                LongitudinalSpeedMps = 10f,
                NormalLoadN = 3556f,
                WheelRadiusMeters = 0.36f,
                InertiaKgM2 = 1.62f,
                BrakeTorqueNm = 1710f,
                LongitudinalStiffnessNsPerMeter = 6500f,
                LateralStiffnessNsPerMeter = 8000f,
                LongitudinalFriction = 1.05f,
                LateralFriction = 1.05f,
                RollingResistanceCoefficient = 0.014f,
                BodyFixedDeltaTime = bodyStep,
                LowSpeedBrakeThresholdMps = 0.5f,
                DeltaTime = bodyStep / 4f
            };
            input.BrakeEffectiveMassKg = 0f;
            WheelDynamicsResult legacy = WheelDynamicsSolver.Step(input);
            input.BrakeEffectiveMassKg = 362.5f;
            WheelDynamicsResult bounded = WheelDynamicsSolver.Step(input);

            Assert.That(bounded.LongitudinalForceN,
                Is.EqualTo(legacy.LongitudinalForceN).Within(0.0001f));
            Assert.That(bounded.AngularVelocityRadPerSec,
                Is.EqualTo(legacy.AngularVelocityRadPerSec).Within(0.0001f));
            Assert.IsFalse(bounded.NearStopBrakeImpulseLimited);
        }

        [Test]
        public void NoBrakeAndAirborneBehaviorUnaffectedByBrakeLimiter()
        {
            var p = new WheelDynamicsInput
            {
                AngularVelocityRadPerSec = 0f,
                LongitudinalSpeedMps = 0.08f,
                NormalLoadN = 3556f,
                WheelRadiusMeters = 0.36f,
                InertiaKgM2 = 1.62f,
                BrakeTorqueNm = 0f,
                DriveTorqueNm = 500f,
                LongitudinalStiffnessNsPerMeter = 6500f,
                LateralStiffnessNsPerMeter = 8000f,
                LongitudinalFriction = 1.05f,
                LateralFriction = 1.05f,
                BodyFixedDeltaTime = 1f / 60f,
                LowSpeedBrakeThresholdMps = 0.5f,
                DeltaTime = 1f / 240f
            };
            p.BrakeEffectiveMassKg = 0f;
            WheelDynamicsResult legacy = WheelDynamicsSolver.Step(p);
            p.BrakeEffectiveMassKg = 362.5f;
            WheelDynamicsResult bounded = WheelDynamicsSolver.Step(p);

            Assert.That(bounded.LongitudinalForceN,
                Is.EqualTo(legacy.LongitudinalForceN).Within(0.0001f));

            p.NormalLoadN = 0f;
            bounded = WheelDynamicsSolver.Step(p);
            Assert.AreEqual(0f, bounded.LongitudinalForceN);
            Assert.AreEqual(0f, bounded.LateralForceN);
        }

        [TestCase(VehicleTemplate.ThreeWheelDelta, 3, 2, 1)]
        [TestCase(VehicleTemplate.PassengerCar4, 4, 2, 1)]
        [TestCase(VehicleTemplate.Truck6x6, 6, 6, 3)]
        [TestCase(VehicleTemplate.Truck8x8, 8, 8, 4)]
        [TestCase(VehicleTemplate.SingleChassis20WheelStressTest, 20, 16, 8)]
        public void M1TorqueDistributionSupportsEveryStarterLayout(
            VehicleTemplate template, int total, int driven, int drivenAxles)
        {
            VehicleDefinition vehicle = VehicleTemplateFactory.Create(template);
            SimplePowertrainDefinition config =
                ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            try
            {
                vehicle.Powertrain = config;
                foreach (var error in VehicleConfigurationValidator.Validate(vehicle))
                    Assert.AreNotEqual(ValidationSeverity.Error, error.Severity, error.Message);

                var state = new SimplePowertrainRuntime(config, vehicle.Wheels);
                float[] feedback = new float[total];
                float[] torque = new float[total];
                state.Step(1f, 1, 0f, feedback, torque);

                Assert.AreEqual(total, vehicle.Wheels.Count);
                Assert.AreEqual(driven, state.DrivenWheelCount);
                Assert.AreEqual(drivenAxles, state.DrivenAxleCount);
                float sum = 0f;
                for (int i = 0; i < total; i++)
                {
                    if (vehicle.Wheels[i].CanDrive)
                        Assert.Greater(torque[i], 0f);
                    else
                        Assert.AreEqual(0f, torque[i]);
                    sum += torque[i];
                }
                Assert.Greater(sum, 0f);
                Assert.That(sum, Is.EqualTo(state.TotalWheelTorqueNm).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(vehicle);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void M1EqualAxleDistributionDoesNotAssumeSameWheelsPerAxle()
        {
            SimplePowertrainDefinition config =
                ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            try
            {
                // One-wheel steering-drive axle and a separate two-wheel axle.
                var wheels = new System.Collections.Generic.List<WheelDefinition>
                {
                    new WheelDefinition { Id = "front-C", AxleId = "front", CanDrive = true },
                    new WheelDefinition { Id = "rear-L", AxleId = "rear", CanDrive = true },
                    new WheelDefinition { Id = "rear-R", AxleId = "rear", CanDrive = true },
                    new WheelDefinition { Id = "trailing-C", AxleId = "free", CanDrive = false }
                };
                var model = new SimplePowertrainRuntime(config, wheels);
                float[] output = new float[4];
                model.Step(1f, 1, 0f, new float[4], output);
                Assert.Greater(output[0], 0f);
                Assert.That(output[0], Is.EqualTo(output[1] + output[2]).Within(0.01f));
                Assert.That(output[1], Is.EqualTo(output[2]).Within(0.001f));
                Assert.AreEqual(0f, output[3]);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void M1NeutralHasNoTorqueAndReverseChangesTorqueSign()
        {
            VehicleDefinition vehicle = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            SimplePowertrainDefinition config =
                ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            try
            {
                var runtime = new SimplePowertrainRuntime(config, vehicle.Wheels);
                float[] torques = new float[vehicle.Wheels.Count];
                float[] angular = new float[torques.Length];

                runtime.Step(1f, 0, 0f, angular, torques);
                Assert.AreEqual(0f, runtime.TotalWheelTorqueNm);
                foreach (float value in torques) Assert.AreEqual(0f, value);

                runtime.Step(1f, -1, 0f, angular, torques);
                Assert.Less(runtime.TotalWheelTorqueNm, 0f);
                foreach (int i in new[] { 2, 3 })
                    Assert.Less(torques[i], 0f);
                Assert.AreEqual(-1, runtime.CurrentGear);

                runtime.Step(1f, 1, 0f, angular, torques);
                Assert.Greater(runtime.TotalWheelTorqueNm, 0f);
                Assert.Greater(runtime.EngineRpm, 0f);
            }
            finally
            {
                Object.DestroyImmediate(vehicle);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void M1FirstGearDeliversMoreWheelTorqueThanSecondAtSameEngineRpm()
        {
            VehicleDefinition vehicle = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            SimplePowertrainDefinition config =
                ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            try
            {
                var runtime = new SimplePowertrainRuntime(config, vehicle.Wheels);
                float[] feedback = new float[4];
                float[] output = new float[4];
                runtime.Step(0.7f, 1, 0f, feedback, output);
                float first = runtime.TotalWheelTorqueNm;
                runtime.Reset();
                runtime.Step(0.7f, 2, 0f, feedback, output);
                float second = runtime.TotalWheelTorqueNm;
                Assert.Greater(first, second);
                Assert.Greater(second, 0f);
            }
            finally
            {
                Object.DestroyImmediate(vehicle);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void M1RedlineCutStopsPowerToOverspinningWheels()
        {
            VehicleDefinition vehicle = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            SimplePowertrainDefinition config =
                ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            try
            {
                var runtime = new SimplePowertrainRuntime(config, vehicle.Wheels);
                float[] output = new float[4];
                float[] angular = { 0f, 0f, 200f, 200f };
                runtime.Step(1f, 1, 0.02f, angular, output);
                foreach (float torque in output) Assert.AreEqual(0f, torque);
                Assert.AreEqual(0f, runtime.TotalWheelTorqueNm);
                Assert.LessOrEqual(runtime.EngineRpm, config.MaxRpm);
            }
            finally
            {
                Object.DestroyImmediate(vehicle);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void M1InvalidGearsAndMissingDrivenWheelsAreSafe()
        {
            SimplePowertrainDefinition config =
                ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            try
            {
                var wheels = new System.Collections.Generic.List<WheelDefinition>
                {
                    new WheelDefinition { Id = "free", AxleId = "a", CanDrive = false }
                };
                var runtime = new SimplePowertrainRuntime(config, wheels);
                float[] output = { 123f };
                float[] angular = new float[1];
                runtime.Step(1f, 1, .016f, angular, output);
                Assert.AreEqual(0, runtime.DrivenWheelCount);
                Assert.AreEqual(0f, output[0]);

                var driven = new System.Collections.Generic.List<WheelDefinition>
                {
                    new WheelDefinition { Id = "powered", AxleId = "b", CanDrive = true }
                };
                runtime = new SimplePowertrainRuntime(config, driven);
                runtime.Step(1f, 99, .016f, angular, output);
                Assert.AreEqual(0f, output[0]);
                Assert.AreEqual(0f, runtime.TotalWheelTorqueNm);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void M1VehicleDefinitionRemainsValidWithoutPowertrain()
        {
            VehicleDefinition vehicle = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            try
            {
                Assert.IsNull(vehicle.Powertrain);
                foreach (VehicleValidationIssue issue in
                         VehicleConfigurationValidator.Validate(vehicle))
                    Assert.AreNotEqual(ValidationSeverity.Error,
                        issue.Severity, issue.Message);
            }
            finally { Object.DestroyImmediate(vehicle); }
        }

    }
}
