using NUnit.Framework;
using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEngine;

namespace Progesor.Kavra.Lab.Tests
{
    /// <summary>
    /// Source-level regression coverage for the first real multi-body rig.
    /// Unity EditMode + Play Mode acceptance is still required.
    /// </summary>
    public sealed class KavraAssemblyEditModeTests
    {
        private static VehicleAssemblyDefinition MakeAssembly(
            out VehicleDefinition tractor, out VehicleDefinition trailer,
            out SimplePowertrainDefinition motor)
        {
            tractor = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            motor = VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain();
            tractor.Powertrain = motor;
            trailer = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
            var assembly = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            assembly.Tractor = tractor;
            assembly.Trailer = trailer;
            return assembly;
        }

        private static void Cleanup(GameObject instance,
            VehicleAssemblyDefinition assembly,
            VehicleDefinition tractor, VehicleDefinition trailer,
            SimplePowertrainDefinition motor, GameObject floor = null)
        {
            if (instance != null) Object.DestroyImmediate(instance);
            if (floor != null) Object.DestroyImmediate(floor);
            if (assembly != null) Object.DestroyImmediate(assembly);
            if (tractor != null) Object.DestroyImmediate(tractor);
            if (trailer != null) Object.DestroyImmediate(trailer);
            if (motor != null) Object.DestroyImmediate(motor);
        }

        [Test]
        public void M13HitchMathMakesBothLocalAnchorsCoincideAtAnyHeading()
        {
            Vector3 towPosition = new Vector3(11f, 1.6f, -4f);
            Quaternion towRotation = Quaternion.Euler(0f, 52f, 0f);
            Vector3 towAnchor = new Vector3(0f, -0.1f, -2.5f);
            Vector3 trailerAnchor = new Vector3(0f, -0.1f, 2.2f);

            Vector3 trailerPosition = VehicleAssemblyMath.CalculateTrailerRootPosition(
                towPosition, towRotation, towAnchor, towRotation, trailerAnchor);
            float error = VehicleAssemblyMath.CalculateHitchErrorMeters(
                towPosition, towRotation, towAnchor,
                trailerPosition, towRotation, trailerAnchor);
            Assert.That(error, Is.LessThan(0.0001f));
            Assert.Greater((towPosition - trailerPosition).magnitude, 4f);
        }

        [Test]
        public void M13ValidatorRejectsOneSharedBodyAndBadHitchData()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            var config = MakeAssembly(out tractor, out trailer, out motor);
            try
            {
                Assert.IsFalse(VehicleAssemblyValidator.HasErrors(config));
                config.Trailer = config.Tractor;
                Assert.IsTrue(VehicleAssemblyValidator.HasErrors(config));
                config.Trailer = trailer;
                config.TrailerHitchLocal = new Vector3(float.NaN, 0f, 0f);
                Assert.IsTrue(VehicleAssemblyValidator.HasErrors(config));
                config.TrailerHitchLocal = new Vector3(0f, -0.1f, 2.2f);
                config.MaximumPitchDegrees = -5f;
                Assert.IsTrue(VehicleAssemblyValidator.HasErrors(config));
            }
            finally { Cleanup(null, config, tractor, trailer, motor); }
        }

        [Test]
        public void M13TemplateHasIndependentNonPoweredTrailerWheelSet()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            var config = MakeAssembly(out tractor, out trailer, out motor);
            try
            {
                Assert.IsFalse(VehicleAssemblyValidator.HasErrors(config));
                Assert.AreEqual(6, tractor.Wheels.Count);
                Assert.AreEqual(4, trailer.Wheels.Count);
                Assert.AreEqual(2, trailer.Axles.Count);
                foreach (WheelDefinition wheel in trailer.Wheels)
                {
                    Assert.IsFalse(wheel.CanDrive);
                    Assert.IsFalse(wheel.CanSteer);
                    Assert.IsTrue(wheel.HasServiceBrake);
                }
            }
            finally { Cleanup(null, config, tractor, trailer, motor); }
        }

        [Test]
        public void M13BuilderMakesTwoDynamicBodiesAndRealYawJoint()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            var config = MakeAssembly(out tractor, out trailer, out motor);
            GameObject instance = null;
            try
            {
                instance = VehicleAssemblyBuilder.Build(config);
                VehicleAssemblyRoot root = instance.GetComponent<VehicleAssemblyRoot>();
                Assert.IsNotNull(root);
                Assert.AreSame(config, root.Definition);
                Assert.IsTrue(root.IsAttached);

                Rigidbody[] bodies = instance.GetComponentsInChildren<Rigidbody>(true);
                Assert.AreEqual(2, bodies.Length, "Do not fake a trailer with one Rigidbody.");
                Assert.IsFalse(root.Tractor.GetComponent<Rigidbody>().isKinematic);
                Assert.IsFalse(root.Trailer.GetComponent<Rigidbody>().isKinematic);
                Assert.AreNotSame(root.Tractor.GetComponent<Rigidbody>(),
                    root.Trailer.GetComponent<Rigidbody>());

                ConfigurableJoint joint = root.Coupling.Joint;
                Assert.IsNotNull(joint);
                Assert.AreSame(root.Tractor.GetComponent<Rigidbody>(), joint.connectedBody);
                Assert.AreEqual(ConfigurableJointMotion.Locked, joint.xMotion);
                Assert.AreEqual(ConfigurableJointMotion.Locked, joint.yMotion);
                Assert.AreEqual(ConfigurableJointMotion.Locked, joint.zMotion);
                Assert.AreEqual(ConfigurableJointMotion.Limited, joint.angularXMotion);
                Assert.AreEqual(ConfigurableJointMotion.Free, joint.angularYMotion);
                Assert.AreEqual(ConfigurableJointMotion.Limited, joint.angularZMotion);
                Assert.IsFalse(joint.enableCollision);
                Assert.AreEqual(JointProjectionMode.None, joint.projectionMode);

                float hitchError = VehicleAssemblyMath.CalculateHitchErrorMeters(
                    root.Tractor.transform.position, root.Tractor.transform.rotation,
                    config.TractorHitchLocal, root.Trailer.transform.position,
                    root.Trailer.transform.rotation, config.TrailerHitchLocal);
                Assert.That(hitchError, Is.LessThan(0.001f));
                Assert.AreEqual(6, root.Tractor.GetComponentsInChildren<VehicleWheelAnchor>().Length);
                Assert.AreEqual(4, root.Trailer.GetComponentsInChildren<VehicleWheelAnchor>().Length);
                Assert.AreEqual(2, instance.GetComponentsInChildren<VehicleAssemblyMember>().Length);
            }
            finally { Cleanup(instance, config, tractor, trailer, motor); }
        }

        [Test]
        public void M13CouplingRejectsSelfConnectionAndSeparatedPoints()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            var config = MakeAssembly(out tractor, out trailer, out motor);
            GameObject a = new GameObject("Tow");
            GameObject b = new GameObject("Trailer");
            try
            {
                Rigidbody bodyA = a.AddComponent<Rigidbody>();
                Rigidbody bodyB = b.AddComponent<Rigidbody>();
                VehicleCoupling coupling = b.AddComponent<VehicleCoupling>();
                Assert.AreEqual(VehicleCouplingResult.SameBody,
                    coupling.TryAttach(bodyB, config));
                b.transform.position = new Vector3(0f, 0f, -30f);
                Physics.SyncTransforms();
                Assert.AreEqual(VehicleCouplingResult.AttachmentPointsSeparated,
                    coupling.TryAttach(bodyA, config));
                Assert.IsFalse(coupling.IsAttached);

                b.transform.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                    a.transform.position, a.transform.rotation, config.TractorHitchLocal,
                    b.transform.rotation, config.TrailerHitchLocal);
                Physics.SyncTransforms();
                Assert.AreEqual(VehicleCouplingResult.Connected,
                    coupling.TryAttach(bodyA, config));
                Assert.IsTrue(coupling.IsAttached);
                Assert.AreEqual(VehicleCouplingResult.AlreadyAttached,
                    coupling.TryAttach(bodyA, config));
                Assert.IsTrue(coupling.Detach());
                Assert.IsFalse(coupling.IsAttached);
                // Unity ConfigurableJoint is a Component, not a Behaviour.
                // Detach must remove it; clearing connectedBody would
                // instead constrain the trailer to a fixed world point.
                Assert.IsNull(b.GetComponent<ConfigurableJoint>());
                Assert.IsTrue(coupling.TryAttach(bodyA, config) ==
                    VehicleCouplingResult.Connected);
                Assert.IsTrue(coupling.Detach());
                Assert.IsNull(b.GetComponent<ConfigurableJoint>());
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Cleanup(null, config, tractor, trailer, motor);
            }
        }

        [Test]
        public void M13TrailerBrakeCommandsFollowTheTowVehicleOnlyWhenAttached()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            var config = MakeAssembly(out tractor, out trailer, out motor);
            config.TrailerServiceBrakeMultiplier = 0.75f;
            GameObject instance = null;
            try
            {
                instance = VehicleAssemblyBuilder.Build(config);
                VehicleAssemblyRoot assembly = instance.GetComponent<VehicleAssemblyRoot>();
                VehicleSimulationRoot tow = assembly.Tractor.GetComponent<VehicleSimulationRoot>();
                VehicleSimulationRoot towed = assembly.Trailer.GetComponent<VehicleSimulationRoot>();
                tow.SetInputs(0.5f, 0.3f, 0.8f, 0.6f);
                assembly.PropagateBrakes();

                Assert.That(towed.ServiceBrake, Is.EqualTo(0.6f).Within(0.0001f));
                Assert.That(towed.ParkingBrake, Is.EqualTo(0.6f).Within(0.0001f));
                Assert.AreEqual(0f, towed.Throttle);
                Assert.AreEqual(0f, towed.Steering);
                Assert.AreEqual(VehicleDriveSource.LegacyDirectWheelTorque,
                    towed.DriveSource);
                assembly.Coupling.Detach();
                tow.SetInputs(0f, 0f, 0.2f, 0f);
                assembly.PropagateBrakes();
                // Uncoupled trailers should not retain a stale brake input.
                Assert.That(towed.ServiceBrake, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(towed.ParkingBrake, Is.EqualTo(0f).Within(0.0001f));
            }
            finally { Cleanup(instance, config, tractor, trailer, motor); }
        }

        [Test]
        public void M13ManualRRecoversBothBodiesWithoutPullingTheJointApart()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            var config = MakeAssembly(out tractor, out trailer, out motor);
            GameObject instance = null;
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(200f, 1f, 200f);
                instance = VehicleAssemblyBuilder.Build(config);
                VehicleAssemblyRoot assembly = instance.GetComponent<VehicleAssemblyRoot>();
                Rigidbody a = assembly.Tractor.GetComponent<Rigidbody>();
                Rigidbody b = assembly.Trailer.GetComponent<Rigidbody>();
                Quaternion tipped = Quaternion.Euler(0f, 10f, 150f);
                a.transform.position = new Vector3(0f, 1.0f, 0f);
                a.transform.rotation = tipped;
                b.transform.rotation = tipped;
                b.transform.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                    a.transform.position, tipped, config.TractorHitchLocal,
                    tipped, config.TrailerHitchLocal);
                a.linearVelocity = Vector3.zero;
                b.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();

                var result = VehicleManualRecovery.TryRecover(
                    assembly.Tractor.GetComponent<VehicleSimulationRoot>());
                Assert.AreEqual(VehicleRecoveryResult.Success, result);
                // Rigidbody.rotation is the authoritative physics pose in
                // EditMode; Transform.rotation can remain stale until Unity's
                // NEXT simulation tick after a Rigidbody teleport.
                Assert.Greater(Vector3.Dot(a.rotation * Vector3.up, Vector3.up), 0.999f);
                Assert.Greater(Vector3.Dot(b.rotation * Vector3.up, Vector3.up), 0.999f);
                float error = VehicleAssemblyMath.CalculateHitchErrorMeters(
                    a.position, a.rotation, config.TractorHitchLocal,
                    b.position, b.rotation, config.TrailerHitchLocal);
                Assert.Less(error, 0.0005f);
                Assert.IsTrue(assembly.IsAttached);
            }
            finally { Cleanup(instance, config, tractor, trailer, motor, floor); }
        }

        [Test]
        public void M13ManualRecoveryRefusesFastTrainWithoutMovingEitherBody()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            var config = MakeAssembly(out tractor, out trailer, out motor);
            GameObject instance = null;
            try
            {
                instance = VehicleAssemblyBuilder.Build(config);
                var assembly = instance.GetComponent<VehicleAssemblyRoot>();
                Rigidbody tow = assembly.Tractor.GetComponent<Rigidbody>();
                Rigidbody towed = assembly.Trailer.GetComponent<Rigidbody>();
                Vector3 beforeTow = tow.position;
                Vector3 beforeTrailer = towed.position;
                tow.linearVelocity = new Vector3(5f, 0f, 0f);

                VehicleRecoveryResult result = VehicleManualRecovery.TryRecover(
                    assembly.Tractor.GetComponent<VehicleSimulationRoot>());

                Assert.AreEqual(VehicleRecoveryResult.MovingTooFast, result);
                Assert.Less((tow.position - beforeTow).sqrMagnitude, 0.00001f);
                Assert.Less((towed.position - beforeTrailer).sqrMagnitude, 0.00001f);
                Assert.IsTrue(assembly.IsAttached);
            }
            finally { Cleanup(instance, config, tractor, trailer, motor); }
        }
        [Test]
        public void M13CommonControlApiPreservesExplicitHitchCommands()
        {
            VehicleDefinition tractor, trailer;
            SimplePowertrainDefinition motor;
            VehicleAssemblyDefinition config =
                MakeAssembly(out tractor, out trailer, out motor);
            GameObject instance = null;
            try
            {
                instance = VehicleAssemblyBuilder.Build(config);
                var assembly = instance.GetComponent<VehicleAssemblyRoot>();
                IVehicleAssemblyController api = assembly;
                Assert.AreEqual(2, api.VehicleCount);
                Assert.AreEqual(1, api.LinkCount);
                Assert.AreSame(assembly.Tractor, api.GetVehicle(0));
                Assert.AreSame(assembly.Trailer, api.GetVehicle(1));
                Assert.AreSame(assembly.Coupling, api.GetCoupling(0));
                Assert.IsNull(api.GetVehicle(-1));
                Assert.IsNull(api.GetCoupling(1));
                Assert.AreEqual(2, api.ConnectedLeadVehicleCount());
                VehicleHitchAssessment assessment;
                Assert.AreEqual(VehicleCouplingResult.AlreadyAttached,
                    api.AssessAttachLink(0, out assessment));
                Assert.IsTrue(api.DetachLink(0));
                Assert.AreEqual(1, api.ConnectedLeadVehicleCount());
                Assert.AreEqual(VehicleHitchState.Detached, api.GetLinkState(0));
                Assert.AreEqual(VehicleCouplingResult.Connected,
                    api.AssessAttachLink(0, out assessment));
                Assert.IsTrue(assessment.ReadyToAttach);
                Assert.IsFalse(api.IsLinkAttached(0)); // query cannot reattach
                Assert.AreEqual(VehicleCouplingResult.Connected,
                    api.TryAttachLink(0));
                Assert.IsTrue(api.IsLinkAttached(0));
                Assert.AreEqual(VehicleCouplingResult.InvalidConfiguration,
                    api.TryAttachLink(1));
                Assert.AreEqual(VehicleCouplingResult.InvalidConfiguration,
                    api.AssessAttachLink(-1, out assessment));
                Assert.IsFalse(api.DetachLink(1));
            }
            finally { Cleanup(instance, config, tractor, trailer, motor); }
        }

    }
}
