using NUnit.Framework;
using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEngine;

namespace Progesor.Kavra.Lab.Tests
{
    /// <summary>
    /// EditMode regression tests for explicit hitch lifecycle.
    /// True PHYSICS threshold breaking needs a separate Unity Play Mode run.
    /// </summary>
    public sealed class KavraHitchLifecycleEditModeTests
    {
        private VehicleAssemblyDefinition definition;
        private VehicleDefinition tractor;
        private VehicleDefinition trailer;
        private SimplePowertrainDefinition engine;
        private GameObject assemblyObject;

        [SetUp]
        public void Setup()
        {
            tractor = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            engine = VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain();
            tractor.Powertrain = engine;
            trailer = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
            definition = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            definition.Tractor = tractor;
            definition.Trailer = trailer;
        }

        [TearDown]
        public void Teardown()
        {
            if (assemblyObject != null) Object.DestroyImmediate(assemblyObject);
            if (definition != null) Object.DestroyImmediate(definition);
            if (tractor != null) Object.DestroyImmediate(tractor);
            if (trailer != null) Object.DestroyImmediate(trailer);
            if (engine != null) Object.DestroyImmediate(engine);
        }

        private VehicleAssemblyRoot MakeRig()
        {
            assemblyObject = VehicleAssemblyBuilder.Build(definition, false);
            return assemblyObject.GetComponent<VehicleAssemblyRoot>();
        }

        [Test]
        public void BreakThresholdsAreAssignedToActualUnityJoint()
        {
            definition.BreakForceNewtons = 9500f;
            definition.BreakTorqueNewtonMeters = 4300f;
            var assembly = MakeRig();
            Assert.IsTrue(assembly.IsAttached);
            Assert.AreEqual(VehicleHitchState.Attached, assembly.HitchState);
            Assert.That(assembly.Coupling.Joint.breakForce, Is.EqualTo(9500f));
            Assert.That(assembly.Coupling.Joint.breakTorque, Is.EqualTo(4300f));
            Assert.AreEqual(0, assembly.Coupling.PhysicalBreakCount);
        }

        [Test]
        public void ZeroBreakThresholdMeansUnbreakableJointNotZeroResistance()
        {
            definition.BreakForceNewtons = 0f;
            definition.BreakTorqueNewtonMeters = 0f;
            var assembly = MakeRig();
            Assert.IsTrue(float.IsPositiveInfinity(assembly.Coupling.Joint.breakForce));
            Assert.IsTrue(float.IsPositiveInfinity(assembly.Coupling.Joint.breakTorque));
        }

        [Test]
        public void ManualDetachAndReattachRequireAnExplicitCommand()
        {
            var assembly = MakeRig();
            Rigidbody a = assembly.Tractor.GetComponent<Rigidbody>();
            Rigidbody b = assembly.Trailer.GetComponent<Rigidbody>();
            a.linearVelocity = Vector3.zero;
            b.linearVelocity = Vector3.zero;
            Assert.IsTrue(assembly.Detach());
            Assert.IsFalse(assembly.IsAttached);
            Assert.AreEqual(VehicleHitchState.Detached, assembly.HitchState);
            Assert.IsNull(assembly.Trailer.GetComponent<ConfigurableJoint>());
            Assert.IsFalse(assembly.IsAttached); // no proximity auto-connect
            Assert.AreEqual(VehicleCouplingResult.Connected, assembly.TryAttach());
            Assert.IsTrue(assembly.IsAttached);
        }

        [Test]
        public void DriverCannotDetachOrReconnectAtHighSpeed()
        {
            var assembly = MakeRig();
            Rigidbody a = assembly.Tractor.GetComponent<Rigidbody>();
            Rigidbody b = assembly.Trailer.GetComponent<Rigidbody>();
            a.linearVelocity = Vector3.forward * 7f;
            b.linearVelocity = Vector3.forward * 7f;
            Assert.IsFalse(assembly.Detach());
            Assert.IsTrue(assembly.IsAttached);

            a.linearVelocity = Vector3.zero;
            b.linearVelocity = Vector3.zero;
            Assert.IsTrue(assembly.Detach());
            a.linearVelocity = Vector3.forward * 7f;
            b.linearVelocity = Vector3.forward * 7f;
            Assert.AreEqual(VehicleCouplingResult.VehicleSpeedTooHigh,
                assembly.TryAttach());
            Assert.IsFalse(assembly.IsAttached);
        }

        [Test]
        public void AttachRejectsDistantYawedOrFastRelativeHitchMotion()
        {
            var assembly = MakeRig();
            var a = assembly.Tractor.GetComponent<Rigidbody>();
            var b = assembly.Trailer.GetComponent<Rigidbody>();
            Assert.IsTrue(assembly.Detach());

            b.position += Vector3.back * 2f;
            Physics.SyncTransforms();
            Assert.AreEqual(VehicleCouplingResult.AttachmentPointsSeparated,
                assembly.TryAttach());

            b.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                a.position, a.rotation, definition.TractorHitchLocal,
                a.rotation, definition.TrailerHitchLocal);
            // Keep hitch world location aligned while changing trailer yaw.
            b.rotation = Quaternion.Euler(0f, 70f, 0f);
            b.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                a.position, a.rotation, definition.TractorHitchLocal,
                b.rotation, definition.TrailerHitchLocal);
            Physics.SyncTransforms();
            Assert.AreEqual(VehicleCouplingResult.AttachmentYawMisaligned,
                assembly.TryAttach());

            b.rotation = a.rotation;
            b.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                a.position, a.rotation, definition.TractorHitchLocal,
                b.rotation, definition.TrailerHitchLocal);
            a.linearVelocity = Vector3.right * 1.4f;
            b.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            Assert.AreEqual(VehicleCouplingResult.RelativeSpeedTooHigh,
                assembly.TryAttach());
        }

        [Test]
        public void SmallHitchGapIsSnappedBeforeManualCoupling()
        {
            var assembly = MakeRig();
            Rigidbody a = assembly.Tractor.GetComponent<Rigidbody>();
            Rigidbody b = assembly.Trailer.GetComponent<Rigidbody>();
            Assert.IsTrue(assembly.Detach());
            b.position += new Vector3(0.09f, 0f, 0f);
            Physics.SyncTransforms();
            Assert.AreEqual(VehicleCouplingResult.Connected, assembly.TryAttach());

            float gap = VehicleAssemblyMath.CalculateHitchErrorMeters(
                a.position, a.rotation, definition.TractorHitchLocal,
                b.position, b.rotation, definition.TrailerHitchLocal);
            Assert.That(gap, Is.LessThan(0.0005f));
        }

        [Test]
        public void PhysicsBreakNotificationSetsBrokenStateAndReleasesTrailerBrakes()
        {
            var assembly = MakeRig();
            var tow = assembly.Tractor.GetComponent<VehicleSimulationRoot>();
            var towed = assembly.Trailer.GetComponent<VehicleSimulationRoot>();
            tow.SetInputs(0f, 0f, 0.7f, 0.6f);
            assembly.PropagateBrakes();
            Assert.That(towed.ServiceBrake, Is.GreaterThan(0f));
            Assert.IsTrue(assembly.IsAttached);

            // The EditMode runner cannot invoke the engine's native
            // MonoBehaviour SendMessage dispatcher safely (ShouldRunBehaviour
            // assertion). Invoke the same private callback through reflection
            // after removing the joint. The real PhysX overload is PlayMode.
            Object.DestroyImmediate(assembly.Coupling.Joint);
            var callback = typeof(VehicleCoupling).GetMethod("OnJointBreak",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(callback, "Unity joint-break callback was renamed.");
            callback.Invoke(assembly.Coupling, new object[] { 12345f });
            Assert.IsFalse(assembly.IsAttached);
            Assert.AreEqual(VehicleHitchState.Broken, assembly.HitchState);
            Assert.AreEqual(1, assembly.Coupling.PhysicalBreakCount);
            Assert.That(assembly.Coupling.LastBreakForceNewtons, Is.EqualTo(12345f));
            assembly.PropagateBrakes();
            Assert.AreEqual(0f, towed.ServiceBrake);
            Assert.AreEqual(0f, towed.ParkingBrake);
            Assert.AreEqual(VehicleCouplingResult.Connected, assembly.TryAttach());
            Assert.AreEqual(VehicleHitchState.Attached, assembly.HitchState);
        }

        [Test]
        public void ValidatorRejectsInvalidHitchSettings()
        {
            definition.MaximumAttachRelativeSpeedMps = float.NaN;
            Assert.IsTrue(VehicleAssemblyValidator.HasErrors(definition));
            definition.MaximumAttachRelativeSpeedMps = 1.25f;
            definition.BreakForceNewtons = -100f;
            Assert.IsTrue(VehicleAssemblyValidator.HasErrors(definition));
            definition.BreakForceNewtons = 150000f;
            definition.MaximumManualCouplingBodySpeedMps = -1f;
            Assert.IsTrue(VehicleAssemblyValidator.HasErrors(definition));
        }
        [Test]
        public void LegacyAssetMigrationSetsSafeDefaultsButPreservesExplicitUnbreakable()
        {
            var field = typeof(VehicleAssemblyDefinition).GetField(
                "hitchSettingsVersion",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            var migrate = typeof(VehicleAssemblyDefinition).GetMethod(
                "UpgradeLegacyHitchSettings",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            Assert.IsNotNull(migrate);

            // Emulate an asset serialized before the M1.4 hitch fields.
            field.SetValue(definition, 0);
            definition.MaximumAttachRelativeSpeedMps = 0f;
            definition.MaximumManualCouplingBodySpeedMps = 0f;
            definition.BreakForceNewtons = 0f;
            definition.BreakTorqueNewtonMeters = 0f;
            migrate.Invoke(definition, null);
            Assert.AreEqual(1.25f, definition.MaximumAttachRelativeSpeedMps);
            Assert.AreEqual(1.75f, definition.MaximumManualCouplingBodySpeedMps);
            Assert.AreEqual(150000f, definition.BreakForceNewtons);
            Assert.AreEqual(100000f, definition.BreakTorqueNewtonMeters);
            Assert.IsFalse(VehicleAssemblyValidator.HasErrors(definition));

            // A user explicitly setting both thresholds to 0 on a NEW
            // schema is still allowed to make their hitch unbreakable.
            definition.BreakForceNewtons = 0f;
            definition.BreakTorqueNewtonMeters = 0f;
            migrate.Invoke(definition, null);
            Assert.AreEqual(0f, definition.BreakForceNewtons);
            Assert.AreEqual(0f, definition.BreakTorqueNewtonMeters);
        }

        [Test]
        public void M151HitchCaptureDefaultsAreUserFriendly()
        {
            Assert.That(definition.MaximumAttachErrorMeters,
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(definition.MaximumAttachYawDegrees,
                Is.EqualTo(55f).Within(0.0001f));
            Assert.IsTrue(definition.AlignHitchesOnManualAttach);
        }

        [Test]
        public void M151OldDefaultCaptureMigratesButCustomSettingsRemainUnchanged()
        {
            var field = typeof(VehicleAssemblyDefinition).GetField(
                "hitchSettingsVersion",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            var migration = typeof(VehicleAssemblyDefinition).GetMethod(
                "UpgradeLegacyHitchSettings",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field);
            Assert.IsNotNull(migration);

            // Old v0.1.4 defaults: convert to the more forgiving settings.
            field.SetValue(definition, 1);
            definition.MaximumAttachErrorMeters = 0.15f;
            definition.MaximumAttachYawDegrees = 35f;
            migration.Invoke(definition, null);
            Assert.That(definition.MaximumAttachErrorMeters,
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(definition.MaximumAttachYawDegrees,
                Is.EqualTo(55f).Within(0.0001f));
            Assert.AreEqual(2, (int)field.GetValue(definition));

            // A user's hand-tuned asset is respected exactly.
            field.SetValue(definition, 1);
            definition.MaximumAttachErrorMeters = 0.28f;
            definition.MaximumAttachYawDegrees = 41f;
            migration.Invoke(definition, null);
            Assert.That(definition.MaximumAttachErrorMeters,
                Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(definition.MaximumAttachYawDegrees,
                Is.EqualTo(41f).Within(0.0001f));
        }

        [Test]
        public void M151SharedAssessmentAndAttachAcceptFortyCentimeterManualCapture()
        {
            var assembly = MakeRig();
            Rigidbody a = assembly.Tractor.GetComponent<Rigidbody>();
            Rigidbody b = assembly.Trailer.GetComponent<Rigidbody>();
            Assert.IsTrue(assembly.Detach());
            a.linearVelocity = Vector3.zero;
            b.linearVelocity = Vector3.zero;
            b.position += Vector3.right * 0.40f;
            Physics.SyncTransforms();

            VehicleHitchAssessment result;
            var status = assembly.Coupling.AssessAttach(
                a, definition, out result);
            Assert.AreEqual(VehicleCouplingResult.Connected, status);
            Assert.IsTrue(result.ReadyToAttach);
            Assert.That(result.HitchGapMeters,
                Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(result.YawErrorDegrees, Is.LessThan(0.01f));

            // Only the explicit attach command performs the snap.
            Assert.AreEqual(VehicleCouplingResult.Connected,
                assembly.TryAttach());
            float aligned = VehicleAssemblyMath.CalculateHitchErrorMeters(
                a.position, a.rotation, definition.TractorHitchLocal,
                b.position, b.rotation, definition.TrailerHitchLocal);
            Assert.That(aligned, Is.LessThan(0.0001f));
            Assert.IsTrue(assembly.IsAttached);
        }

        [Test]
        public void M151AssessmentDistinguishesTooFarAngleAndSpeed()
        {
            var assembly = MakeRig();
            Rigidbody a = assembly.Tractor.GetComponent<Rigidbody>();
            Rigidbody b = assembly.Trailer.GetComponent<Rigidbody>();
            Assert.IsTrue(assembly.Detach());

            b.position += Vector3.right * 0.85f;
            Physics.SyncTransforms();
            VehicleHitchAssessment result;
            Assert.AreEqual(VehicleCouplingResult.AttachmentPointsSeparated,
                assembly.Coupling.AssessAttach(a, definition, out result));
            Assert.Greater(result.HitchGapMeters,
                definition.MaximumAttachErrorMeters);

            b.rotation = Quaternion.Euler(0f, 72f, 0f);
            b.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                a.position, a.rotation, definition.TractorHitchLocal,
                b.rotation, definition.TrailerHitchLocal);
            Physics.SyncTransforms();
            Assert.AreEqual(VehicleCouplingResult.AttachmentYawMisaligned,
                assembly.Coupling.AssessAttach(a, definition, out result));
            Assert.Greater(result.YawErrorDegrees,
                definition.MaximumAttachYawDegrees);

            b.rotation = a.rotation;
            b.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                a.position, a.rotation, definition.TractorHitchLocal,
                b.rotation, definition.TrailerHitchLocal);
            a.linearVelocity = Vector3.right * 1.6f;
            b.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            Assert.AreEqual(VehicleCouplingResult.RelativeSpeedTooHigh,
                assembly.Coupling.AssessAttach(a, definition, out result));

            a.linearVelocity = Vector3.right * 2.0f;
            Assert.AreEqual(VehicleCouplingResult.VehicleSpeedTooHigh,
                assembly.Coupling.AssessAttach(a, definition, out result));
        }

        [Test]
        public void M151EditorHitchGuideSetupWorksBeforeAwakeInitializesMaterialBlock()
        {
            var root = new GameObject("HitchGuide_EditMode_Test");
            GameObject tractorMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject trailerMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                tractorMarker.transform.position = Vector3.zero;
                trailerMarker.transform.position = Vector3.right * 0.4f;
                VehicleLabHitchGuide guide = root.AddComponent<VehicleLabHitchGuide>();

                // Even if AddComponent triggers Awake on some Unity versions,
                // reproduce the precise user's scene-generator ordering:
                // Setup is called with an UNINITIALIZED colorBlock.
                var blockField = typeof(VehicleLabHitchGuide).GetField(
                    "colorBlock",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(blockField);
                blockField.SetValue(guide, null);

                Assert.DoesNotThrow(() => guide.Setup(
                    null, null,
                    new[] { tractorMarker.transform },
                    new[] { trailerMarker.transform },
                    null));

                Assert.AreEqual(1, guide.LinkCount);
                Assert.IsNotNull(blockField.GetValue(guide));
                Assert.IsNotNull(guide.Lines);
                Assert.AreEqual(1, guide.Lines.Length);
                Assert.IsNotNull(guide.Lines[0]);
                Assert.IsNotNull(guide.CaptureRings[0]);
                Assert.That(guide.SelectedHitchGapMeters(),
                    Is.EqualTo(0.4f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(tractorMarker);
                Object.DestroyImmediate(trailerMarker);
            }
        }

    }
}
