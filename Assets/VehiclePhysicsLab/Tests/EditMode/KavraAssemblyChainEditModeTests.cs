using NUnit.Framework;
using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEngine;

namespace Progesor.Kavra.Lab.Tests
{
    /// <summary>
    /// EditMode structural/command tests for the first ordered multi-body
    /// chain. Joint physics convergence and real break events are PlayMode.
    /// </summary>
    public sealed class KavraAssemblyChainEditModeTests
    {
        private VehicleDefinition tractor;
        private VehicleDefinition trailerA;
        private VehicleDefinition trailerB;
        private SimplePowertrainDefinition engine;
        private VehicleAssemblyDefinition first;
        private VehicleAssemblyDefinition second;
        private VehicleAssemblyChainDefinition chainDef;
        private GameObject sceneRoot;
        private GameObject floor;

        [SetUp]
        public void Setup()
        {
            tractor = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            engine = VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain();
            tractor.Powertrain = engine;
            trailerA = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
            trailerB = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
            trailerA.VehicleName = "M1.5 Trailer A";
            trailerB.VehicleName = "M1.5 Trailer B";

            first = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            first.Tractor = tractor;
            first.Trailer = trailerA;
            first.TractorHitchLocal = new Vector3(0f, -0.1f, -3f);

            second = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            second.Tractor = trailerA;
            second.Trailer = trailerB;
            second.TractorHitchLocal = new Vector3(0f, -0.1f, -3.3f);

            chainDef = ScriptableObject.CreateInstance<VehicleAssemblyChainDefinition>();
            chainDef.Links.Add(first);
            chainDef.Links.Add(second);
        }

        [TearDown]
        public void Cleanup()
        {
            if (sceneRoot != null) Object.DestroyImmediate(sceneRoot);
            if (floor != null) Object.DestroyImmediate(floor);
            if (chainDef != null) Object.DestroyImmediate(chainDef);
            if (first != null) Object.DestroyImmediate(first);
            if (second != null) Object.DestroyImmediate(second);
            if (tractor != null) Object.DestroyImmediate(tractor);
            if (trailerA != null) Object.DestroyImmediate(trailerA);
            if (trailerB != null) Object.DestroyImmediate(trailerB);
            if (engine != null) Object.DestroyImmediate(engine);
        }

        private VehicleAssemblyChainRoot Build()
        {
            sceneRoot = VehicleAssemblyChainBuilder.Build(chainDef);
            return sceneRoot.GetComponent<VehicleAssemblyChainRoot>();
        }

        [Test]
        public void M15ValidatorAcceptsThreeBodiesAndRejectsCycles()
        {
            Assert.IsFalse(VehicleAssemblyChainValidator.HasErrors(chainDef));
            Assert.AreEqual(3, chainDef.VehicleCount);
            second.Trailer = tractor;
            Assert.IsTrue(VehicleAssemblyChainValidator.HasErrors(chainDef));
            second.Trailer = trailerB;
            second.Tractor = tractor;
            Assert.IsTrue(VehicleAssemblyChainValidator.HasErrors(chainDef));
            second.Tractor = trailerA;
            chainDef.Links.Add(first);
            Assert.IsTrue(VehicleAssemblyChainValidator.HasErrors(chainDef));
        }

        [Test]
        public void M15BuilderCreatesThreeIndependentRigidbodiesAndTwoJoints()
        {
            var chain = Build();
            Assert.AreEqual(3, chain.Vehicles.Length);
            Assert.AreEqual(2, chain.Couplings.Length);
            Assert.AreEqual(3, sceneRoot.GetComponentsInChildren<Rigidbody>().Length);
            Assert.AreEqual(2, sceneRoot.GetComponentsInChildren<ConfigurableJoint>().Length);
            Assert.AreEqual(3,
                sceneRoot.GetComponentsInChildren<VehicleAssemblyChainMember>().Length);
            Assert.AreEqual(14,
                sceneRoot.GetComponentsInChildren<VehicleWheelAnchor>().Length);
            Assert.AreEqual(3, chain.ConnectedLeadVehicleCount());

            for (int i = 0; i < 2; i++)
            {
                ConfigurableJoint joint = chain.Couplings[i].Joint;
                Assert.IsNotNull(joint);
                Assert.AreSame(chain.Vehicles[i].GetComponent<Rigidbody>(), joint.connectedBody);
                Assert.AreEqual(ConfigurableJointMotion.Free, joint.angularYMotion);
                Assert.AreEqual(ConfigurableJointMotion.Locked, joint.xMotion);
                float gap = VehicleAssemblyMath.CalculateHitchErrorMeters(
                    chain.Vehicles[i].transform.position,
                    chain.Vehicles[i].transform.rotation,
                    chainDef.Links[i].TractorHitchLocal,
                    chain.Vehicles[i + 1].transform.position,
                    chain.Vehicles[i + 1].transform.rotation,
                    chainDef.Links[i].TrailerHitchLocal);
                Assert.Less(gap, 0.001f);
            }
        }

        [Test]
        public void M15SelectedLinkWrapsForwardAndBackward()
        {
            var chain = Build();
            Assert.AreEqual(0, chain.SelectedLinkIndex);
            chain.SelectRelativeLink(-1);
            Assert.AreEqual(1, chain.SelectedLinkIndex);
            chain.SelectRelativeLink(1);
            Assert.AreEqual(0, chain.SelectedLinkIndex);
            chain.SelectLink(99);
            Assert.AreEqual(1, chain.SelectedLinkIndex);
        }

        [Test]
        public void M15BrakingPassesThroughTwoLinksWithTheirOwnMultipliers()
        {
            first.TrailerServiceBrakeMultiplier = 0.75f;
            second.TrailerServiceBrakeMultiplier = 0.5f;
            second.FollowTractorParkingBrake = false;

            var chain = Build();
            chain.Vehicles[0].GetComponent<VehicleSimulationRoot>()
                .SetInputs(0.6f, 0.3f, 0.8f, 0.7f);
            chain.PropagateBrakes();

            var a = chain.Vehicles[1].GetComponent<VehicleSimulationRoot>();
            var b = chain.Vehicles[2].GetComponent<VehicleSimulationRoot>();
            Assert.That(a.ServiceBrake, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(b.ServiceBrake, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(a.ParkingBrake, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(b.ParkingBrake, Is.EqualTo(0f));
            Assert.AreEqual(0f, a.Throttle);
            Assert.AreEqual(0f, b.Throttle);
        }

        [Test]
        public void M15DisconnectingSecondLinkKeepsFirstJointAndClearsSecondBrakes()
        {
            var chain = Build();
            chain.Vehicles[0].GetComponent<VehicleSimulationRoot>()
                .SetInputs(0f, 0f, 0.8f, 0.6f);
            chain.PropagateBrakes();

            chain.SelectLink(1);
            Assert.IsTrue(chain.DetachSelected());
            Assert.IsTrue(chain.IsLinkAttached(0));
            Assert.IsFalse(chain.IsLinkAttached(1));
            Assert.AreEqual(2, chain.ConnectedLeadVehicleCount());

            var a = chain.Vehicles[1].GetComponent<VehicleSimulationRoot>();
            var b = chain.Vehicles[2].GetComponent<VehicleSimulationRoot>();
            Assert.Greater(a.ServiceBrake, 0f);
            Assert.AreEqual(0f, b.ServiceBrake);
            Assert.AreEqual(0f, b.ParkingBrake);
            Assert.AreEqual(VehicleCouplingResult.Connected,
                chain.TryAttachSelected());
            Assert.AreEqual(3, chain.ConnectedLeadVehicleCount());
        }

        [Test]
        public void M15FrontJointBreakLeavesRearPairButCutsTractorBrakes()
        {
            var chain = Build();
            chain.Vehicles[0].GetComponent<VehicleSimulationRoot>()
                .SetInputs(0f, 0f, 0.7f, 0.5f);
            chain.PropagateBrakes();

            Object.DestroyImmediate(chain.Couplings[0].Joint);
            chain.Couplings[0].SendMessage("OnJointBreak", 160000f);
            Assert.IsFalse(chain.IsLinkAttached(0));
            Assert.IsTrue(chain.IsLinkAttached(1));
            Assert.AreEqual(1, chain.ConnectedLeadVehicleCount());
            chain.PropagateBrakes();

            Assert.AreEqual(0f,
                chain.Vehicles[1].GetComponent<VehicleSimulationRoot>().ServiceBrake);
            Assert.AreEqual(0f,
                chain.Vehicles[2].GetComponent<VehicleSimulationRoot>().ServiceBrake);
            Assert.AreEqual(VehicleHitchState.Broken, chain.Couplings[0].State);
            Assert.AreEqual(1, chain.Couplings[0].PhysicalBreakCount);
        }

        [Test]
        public void M15ChainRecoveryRefusesMovingTrainBeforeAnyTeleport()
        {
            var chain = Build();
            Rigidbody a = chain.Vehicles[0].GetComponent<Rigidbody>();
            Rigidbody b = chain.Vehicles[1].GetComponent<Rigidbody>();
            Rigidbody c = chain.Vehicles[2].GetComponent<Rigidbody>();
            Vector3 beforeA = a.position, beforeB = b.position, beforeC = c.position;
            c.linearVelocity = new Vector3(5f, 0f, 0f);

            VehicleRecoveryResult result = VehicleManualRecovery.TryRecover(
                chain.Vehicles[0].GetComponent<VehicleSimulationRoot>());
            Assert.AreEqual(VehicleRecoveryResult.MovingTooFast, result);
            Assert.That(a.position, Is.EqualTo(beforeA));
            Assert.That(b.position, Is.EqualTo(beforeB));
            Assert.That(c.position, Is.EqualTo(beforeC));
        }

        [Test]
        public void M15RecoverDetachedPrefixDoesNotMoveIndependentRearPair()
        {
            var chain = Build();
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(200f, 1f, 200f);
            chain.SelectLink(0);
            Assert.IsTrue(chain.DetachSelected());

            Rigidbody tow = chain.Vehicles[0].GetComponent<Rigidbody>();
            Rigidbody a = chain.Vehicles[1].GetComponent<Rigidbody>();
            Rigidbody b = chain.Vehicles[2].GetComponent<Rigidbody>();
            Vector3 aBefore = a.position;
            Vector3 bBefore = b.position;
            Quaternion tip = Quaternion.Euler(0f, 0f, 145f);
            tow.rotation = tip;
            tow.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();

            VehicleRecoveryResult recovery = VehicleManualRecovery.TryRecover(
                chain.Vehicles[0].GetComponent<VehicleSimulationRoot>());
            Assert.AreEqual(VehicleRecoveryResult.Success, recovery);
            Assert.Greater(Vector3.Dot(tow.transform.up, Vector3.up), 0.999f);
            Assert.That(a.position, Is.EqualTo(aBefore));
            Assert.That(b.position, Is.EqualTo(bBefore));
            Assert.IsTrue(chain.IsLinkAttached(1));
        }

        [Test]
        public void M15FullConnectedTrainRecoveryKeepsBothHitchPoints()
        {
            var chain = Build();
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(200f, 1f, 200f);

            Quaternion tip = Quaternion.Euler(0f, 20f, 150f);
            Rigidbody head = chain.Vehicles[0].GetComponent<Rigidbody>();
            head.rotation = tip;
            for (int i = 1; i < chain.Vehicles.Length; i++)
            {
                Rigidbody front = chain.Vehicles[i - 1].GetComponent<Rigidbody>();
                Rigidbody rear = chain.Vehicles[i].GetComponent<Rigidbody>();
                rear.rotation = tip;
                rear.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                    front.position, front.rotation, chainDef.Links[i - 1].TractorHitchLocal,
                    rear.rotation, chainDef.Links[i - 1].TrailerHitchLocal);
            }
            Physics.SyncTransforms();

            VehicleRecoveryResult result = VehicleManualRecovery.TryRecover(
                chain.Vehicles[0].GetComponent<VehicleSimulationRoot>());
            Assert.AreEqual(VehicleRecoveryResult.Success, result);
            for (int i = 0; i < chain.Vehicles.Length; i++)
                Assert.Greater(Vector3.Dot(chain.Vehicles[i].transform.up, Vector3.up), 0.999f);
            for (int i = 0; i < chain.Couplings.Length; i++)
            {
                float gap = VehicleAssemblyMath.CalculateHitchErrorMeters(
                    chain.Vehicles[i].transform.position,
                    chain.Vehicles[i].transform.rotation, chainDef.Links[i].TractorHitchLocal,
                    chain.Vehicles[i + 1].transform.position,
                    chain.Vehicles[i + 1].transform.rotation, chainDef.Links[i].TrailerHitchLocal);
                Assert.Less(gap, 0.0005f);
            }
        }
    }
}
