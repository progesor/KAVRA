using System;
using NUnit.Framework;
using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Lab.Tests
{
    /// <summary>
    /// AssetDatabase roundtrip, not a physics/PlayMode test. Every test creates
    /// a unique disposable Assets/ folder, never overwriting user assets.
    /// Verifies that UPM components, references and internal joints survive
    /// PrefabUtility save/reimport/reload without the optional PhysicsLab host.
    /// </summary>
    public sealed class KavraPrefabRoundtripEditModeTests
    {
        private string folder;
        private GameObject temporary;
        private GameObject loaded;
        private UnityEngine.Object originalSelection;

        [SetUp]
        public void SetUp()
        {
            originalSelection = Selection.activeObject;
            string name = "__KAVRA_Roundtrip_" + Guid.NewGuid().ToString("N");
            folder = "Assets/" + name;
            string folderGuid = AssetDatabase.CreateFolder("Assets", name);
            Assert.IsFalse(string.IsNullOrEmpty(folderGuid));
        }

        [TearDown]
        public void TearDown()
        {
            if (loaded != null)
                PrefabUtility.UnloadPrefabContents(loaded);
            if (temporary != null)
                UnityEngine.Object.DestroyImmediate(temporary);
            Selection.activeObject = originalSelection;
            if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                Assert.IsTrue(AssetDatabase.DeleteAsset(folder),
                    "Only this test's unique disposable asset folder may be deleted.");
        }

        private T Store<T>(T asset, string name) where T : UnityEngine.Object
        {
            AssetDatabase.CreateAsset(asset, folder + "/" + name + ".asset");
            return asset;
        }

        private GameObject SaveAndLoad(string prefabName)
        {
            string prefabPath = folder + "/" + prefabName + ".prefab";
            bool success;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                temporary, prefabPath, out success);
            Assert.IsTrue(success);
            Assert.IsNotNull(saved);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
            loaded = PrefabUtility.LoadPrefabContents(prefabPath);
            Assert.IsNotNull(loaded);
            return loaded;
        }

        [TestCase(VehicleTemplate.PassengerCar4, 4)]
        [TestCase(VehicleTemplate.SingleChassis20WheelStressTest, 20)]
        public void M1PoweredChassisPrefabKeepsWheelIdsAndDefinitionReferences(
            VehicleTemplate template, int expectedWheels)
        {
            var powertrain = Store(
                VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain(),
                "Powertrain");
            var vehicle = VehicleTemplateFactory.Create(template);
            vehicle.Powertrain = powertrain;
            Store(vehicle, "Vehicle");
            AssetDatabase.SaveAssets();

            temporary = VehicleScaffoldBuilder.Create(vehicle, true,
                VehicleDriveSource.SimplePowertrain);
            GameObject prefab = SaveAndLoad("SingleChassis");
            VehicleRoot root = prefab.GetComponent<VehicleRoot>();
            var simulation = prefab.GetComponent<VehicleSimulationRoot>();
            Assert.IsNotNull(root);
            Assert.AreSame(vehicle, root.Definition);
            Assert.IsNotNull(simulation);
            Assert.AreEqual(VehicleDriveSource.SimplePowertrain, simulation.DriveSource);
            Assert.AreSame(powertrain, root.Definition.Powertrain);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<Rigidbody>().Length);
            Assert.AreEqual(0, prefab.GetComponentsInChildren<ConfigurableJoint>().Length);
            var anchors = prefab.GetComponentsInChildren<VehicleWheelAnchor>(true);
            Assert.AreEqual(expectedWheels, anchors.Length);
            foreach (VehicleWheelAnchor anchor in anchors)
            {
                Assert.IsNotNull(anchor);
                Assert.IsTrue(root.Definition.Wheels.Exists(w => w.Id == anchor.WheelId));
            }
        }

        [Test]
        public void M14TwoBodyPrefabReimportsWithIndependentRigidbodyAndOneInternalJoint()
        {
            var powertrain = Store(
                VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain(),
                "Powertrain");
            VehicleDefinition tractor = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            tractor.Powertrain = powertrain;
            Store(tractor, "Tractor");
            VehicleDefinition trailer = Store(
                VehicleAssemblyTemplateFactory.CreateFourWheelTrailer(), "Trailer");
            var asset = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            asset.Tractor = tractor;
            asset.Trailer = trailer;
            Store(asset, "Assembly");
            AssetDatabase.SaveAssets();

            temporary = VehicleAssemblyBuilder.Build(asset);
            GameObject prefab = SaveAndLoad("TwoBody");
            var root = prefab.GetComponent<VehicleAssemblyRoot>();
            Assert.IsNotNull(root);
            Assert.AreSame(asset, root.Definition);
            Assert.AreEqual(2, root.VehicleCount);
            Assert.AreEqual(2, root.ConnectedLeadVehicleCount());
            Assert.AreSame(tractor, root.Tractor.Definition);
            Assert.AreSame(trailer, root.Trailer.Definition);
            Assert.AreEqual(2, prefab.GetComponentsInChildren<Rigidbody>().Length);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<ConfigurableJoint>().Length);
            Assert.AreEqual(10, prefab.GetComponentsInChildren<VehicleWheelAnchor>(true).Length);
            Assert.AreSame(root.Coupling.Joint,
                root.Trailer.GetComponent<ConfigurableJoint>());
            Assert.AreSame(root.Tractor.GetComponent<Rigidbody>(),
                root.Coupling.Joint.connectedBody);
            Assert.IsTrue(root.IsAttached);
        }

        [Test]
        public void M15ThreeBodyPrefabReimportsChainAndBothIndependentJoints()
        {
            var powertrain = Store(
                VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain(),
                "Powertrain");
            VehicleDefinition tractor = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            tractor.Powertrain = powertrain;
            Store(tractor, "Tractor");
            VehicleDefinition trailerA = Store(
                VehicleAssemblyTemplateFactory.CreateFourWheelTrailer(), "TrailerA");
            VehicleDefinition trailerB = Store(
                VehicleAssemblyTemplateFactory.CreateFourWheelTrailer(), "TrailerB");
            var first = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            first.Tractor = tractor;
            first.Trailer = trailerA;
            Store(first, "Hitch0");
            var second = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            second.Tractor = trailerA;
            second.Trailer = trailerB;
            Store(second, "Hitch1");
            var chainAsset = ScriptableObject.CreateInstance<VehicleAssemblyChainDefinition>();
            chainAsset.Links.Add(first);
            chainAsset.Links.Add(second);
            Store(chainAsset, "Chain");
            AssetDatabase.SaveAssets();

            temporary = VehicleAssemblyChainBuilder.Build(chainAsset);
            GameObject prefab = SaveAndLoad("ThreeBody");
            var root = prefab.GetComponent<VehicleAssemblyChainRoot>();
            Assert.IsNotNull(root);
            Assert.AreSame(chainAsset, root.Definition);
            Assert.AreEqual(3, root.VehicleCount);
            Assert.AreEqual(2, root.LinkCount);
            Assert.AreEqual(3, root.ConnectedLeadVehicleCount());
            Assert.AreEqual(3, prefab.GetComponentsInChildren<Rigidbody>().Length);
            Assert.AreEqual(2, prefab.GetComponentsInChildren<ConfigurableJoint>().Length);
            Assert.AreEqual(14, prefab.GetComponentsInChildren<VehicleWheelAnchor>(true).Length);

            Assert.AreSame(tractor, root.Vehicles[0].Definition);
            Assert.AreSame(trailerA, root.Vehicles[1].Definition);
            Assert.AreSame(trailerB, root.Vehicles[2].Definition);
            for (int i = 0; i < root.LinkCount; i++)
            {
                Assert.IsTrue(root.IsLinkAttached(i), "Link " + i);
                Assert.AreSame(root.Couplings[i].Joint,
                    root.Vehicles[i + 1].GetComponent<ConfigurableJoint>());
                Assert.AreSame(root.Vehicles[i].GetComponent<Rigidbody>(),
                    root.Couplings[i].Joint.connectedBody);
                Assert.AreEqual(VehicleHitchState.Attached, root.GetLinkState(i));
            }
        }
    }
}
