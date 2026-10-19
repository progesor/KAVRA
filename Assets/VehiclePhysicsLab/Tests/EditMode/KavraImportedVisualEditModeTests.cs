using System;
using NUnit.Framework;
using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Lab.Tests
{
    /// <summary>
    /// P2 imported mesh binding acceptance. All temporary assets live under
    /// unique test-owned folders and all construction uses recordUndo=false
    /// to avoid the native TestRunner Undo rollback crash.
    /// </summary>
    public sealed class KavraImportedVisualEditModeTests
    {
        private string folder;
        private GameObject generated;
        private GameObject reloaded;
        private UnityEngine.Object selectedBefore;

        [SetUp]
        public void Setup()
        {
            selectedBefore = Selection.activeObject;
            string name = "__KAVRA_ImportedVisual_" + Guid.NewGuid().ToString("N");
            folder = "Assets/" + name;
            Assert.IsNotEmpty(AssetDatabase.CreateFolder("Assets", name));
        }

        [TearDown]
        public void Cleanup()
        {
            if (reloaded != null) PrefabUtility.UnloadPrefabContents(reloaded);
            if (generated != null) UnityEngine.Object.DestroyImmediate(generated);
            Selection.activeObject = selectedBefore;
            if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                Assert.IsTrue(AssetDatabase.DeleteAsset(folder),
                    "Delete only this test's unique temporary asset folder.");
        }

        private T Store<T>(T obj, string file) where T : UnityEngine.Object
        {
            AssetDatabase.CreateAsset(obj, folder + "/" + file + ".asset");
            return obj;
        }

        private VehicleVisualBindingDefinition MakeProfile(
            VehicleTemplate template, bool powered, int mapped)
        {
            var vehicle = VehicleTemplateFactory.Create(template);
            if (powered)
                vehicle.Powertrain = Store(
                    VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain(),
                    "Powertrain");
            Store(vehicle, "Vehicle");

            // Synthetic imported-like model; built-in primitive mesh/material
            // references are shared with source, not copied into generated
            // physics hierarchy. Original source colliders remain on the
            // asset but MUST NOT be copied by KAVRA's visual-only adapter.
            var staging = new GameObject("SyntheticImportedFBX");
            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "body_mesh";
            chassis.transform.SetParent(staging.transform, false);
            chassis.transform.localScale = new Vector3(1.7f, 0.9f, 3f);
            for (int i = 0; i < vehicle.Wheels.Count; i++)
            {
                WheelDefinition wheel = vehicle.Wheels[i];
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = wheel.Id;
                visual.transform.SetParent(staging.transform, false);
                visual.transform.localPosition = wheel.MountLocalPosition;
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                visual.transform.localScale = new Vector3(
                    wheel.RadiusMeters * 2f, wheel.WidthMeters * 0.5f,
                    wheel.RadiusMeters * 2f);
            }

            bool saved;
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(
                staging, folder + "/ImportedSource.prefab", out saved);
            Assert.IsTrue(saved);
            Assert.IsNotNull(asset);
            UnityEngine.Object.DestroyImmediate(staging);

            var profile = ScriptableObject.CreateInstance<VehicleVisualBindingDefinition>();
            profile.Vehicle = vehicle;
            profile.ModelPrefab = asset;
            for (int i = 0; i < mapped && i < vehicle.Wheels.Count; i++)
            {
                string id = vehicle.Wheels[i].Id;
                profile.WheelVisuals.Add(new VehicleImportedWheelBinding
                {
                    WheelId = id, ModelNodePath = id
                });
            }
            Store(profile, "VisualBinding");
            AssetDatabase.SaveAssets();
            return profile;
        }

        [TestCase(VehicleTemplate.PassengerCar4, 4, false)]
        [TestCase(VehicleTemplate.Truck6x6, 6, true)]
        [TestCase(VehicleTemplate.SingleChassis20WheelStressTest, 20, false)]
        public void ImportedMeshBindsEachWheelWithoutExtraPhysics(
            VehicleTemplate template, int wheelCount, bool powered)
        {
            var binding = MakeProfile(template, powered, wheelCount);
            Assert.IsFalse(VehicleImportedVisualValidator.HasErrors(binding));

            generated = VehicleImportedVisualBuilder.Build(binding, false);
            var info = generated.GetComponent<VehicleImportedVisualRoot>();
            Assert.IsNotNull(info);
            Assert.AreSame(binding, info.Binding);
            Assert.IsNotNull(info.ImportedBodyVisual);
            Assert.AreEqual(1, generated.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.AreEqual(1, generated.GetComponentsInChildren<Collider>(true).Length,
                "Imported FBX source colliders must never be duplicated.");
            Assert.AreEqual(wheelCount,
                generated.GetComponentsInChildren<VehicleWheelAnchor>(true).Length);

            var sim = generated.GetComponent<VehicleSimulationRoot>();
            Assert.IsNotNull(sim);
            Assert.AreEqual(powered
                    ? VehicleDriveSource.SimplePowertrain
                    : VehicleDriveSource.LegacyDirectWheelTorque, sim.DriveSource);

            var sourceModel = binding.ModelPrefab;
            Assert.AreEqual(wheelCount + 1,
                sourceModel.GetComponentsInChildren<MeshRenderer>(true).Length);
            int boundWheels = 0;
            foreach (VehicleWheelAnchor anchor in
                     generated.GetComponentsInChildren<VehicleWheelAnchor>(true))
            {
                Transform wheel = anchor.transform.Find(
                    "TravelPivot/SteeringPivot/SpinPivot/__KAVRA_IMPORTED_WHEEL_" +
                    anchor.WheelId);
                Assert.IsNotNull(wheel, "Missing imported wheel " + anchor.WheelId);
                Assert.AreSame(wheel, anchor.VisualTransform);
                Assert.IsNotNull(anchor.GetComponent<VehicleWheelVisualRig>());
                Assert.IsNotNull(wheel.GetComponent<MeshFilter>());
                Assert.AreSame(
                    sourceModel.transform.Find(anchor.WheelId).GetComponent<MeshFilter>().sharedMesh,
                    wheel.GetComponent<MeshFilter>().sharedMesh);
                Assert.That(wheel.localPosition, Is.EqualTo(Vector3.zero));
                Renderer preview = anchor.transform.Find(
                    "TravelPivot/SteeringPivot/SpinPivot/__WHEEL_PREVIEW")
                    .GetComponent<Renderer>();
                Assert.IsFalse(preview.enabled);
                boundWheels++;
            }
            Assert.AreEqual(wheelCount, boundWheels);
            Assert.IsFalse(generated.transform.Find(
                "__CHASSIS_TEST_COLLIDER").GetComponent<MeshRenderer>().enabled);
            Assert.IsTrue(generated.transform.Find(
                "__CHASSIS_TEST_COLLIDER").GetComponent<BoxCollider>().enabled);

            // Reimport the generated prefab with its external visual-profile,
            // mesh/material and WheelAnchor references intact.
            bool success;
            string path = folder + "/ImportedVehicle.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(generated,
                path, out success);
            Assert.IsTrue(success);
            Assert.IsNotNull(saved);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            reloaded = PrefabUtility.LoadPrefabContents(path);
            Assert.IsNotNull(reloaded);
            Assert.AreSame(binding,
                reloaded.GetComponent<VehicleImportedVisualRoot>().Binding);
            Assert.AreEqual(1, reloaded.GetComponentsInChildren<Rigidbody>(true).Length);
            Assert.AreEqual(wheelCount, reloaded.GetComponentsInChildren<
                VehicleWheelVisualRig>(true).Length);
            foreach (VehicleWheelAnchor anchor in reloaded.GetComponentsInChildren<
                     VehicleWheelAnchor>(true))
                Assert.IsNotNull(anchor.VisualTransform);
        }

        [Test]
        public void InvalidWheelNodePathsAreRejectedWithoutMutatingModelAsset()
        {
            var binding = MakeProfile(VehicleTemplate.PassengerCar4, false, 2);
            int unchangedChildren = binding.ModelPrefab.transform.childCount;
            string oldId = binding.Vehicle.Wheels[0].Id;
            binding.WheelVisuals[0].ModelNodePath = "not/a/wheel";
            Assert.IsTrue(VehicleImportedVisualValidator.HasErrors(binding));
            Assert.Throws<InvalidOperationException>(
                () => VehicleImportedVisualBuilder.Build(binding, false));
            binding.WheelVisuals[0].ModelNodePath = oldId;

            binding.WheelVisuals[1].ModelNodePath = oldId;
            Assert.IsTrue(VehicleImportedVisualValidator.HasErrors(binding),
                "Two physical wheels cannot share one imported wheel subtree.");
            Assert.AreEqual(unchangedChildren, binding.ModelPrefab.transform.childCount);
            Assert.IsNull(binding.ModelPrefab.GetComponent<VehicleRoot>());
        }

        [Test]
        public void UnmappedWheelsKeepProceduralPreviewsAndSourceRemainsIntact()
        {
            var binding = MakeProfile(VehicleTemplate.PassengerCar4, false, 1);
            binding.ModelScale = 1f;
            generated = VehicleImportedVisualBuilder.Build(binding, false);
            int active = 0;
            foreach (VehicleWheelAnchor anchor in
                     generated.GetComponentsInChildren<VehicleWheelAnchor>(true))
            {
                bool mapped = anchor.WheelId == binding.Vehicle.Wheels[0].Id;
                Renderer preview = anchor.transform.Find(
                    "TravelPivot/SteeringPivot/SpinPivot/__WHEEL_PREVIEW")
                    .GetComponent<Renderer>();
                if (preview.enabled) active++;
                Assert.AreEqual(!mapped, preview.enabled);
            }
            Assert.AreEqual(3, active);
            Assert.IsTrue(binding.ModelPrefab.transform.Find(
                binding.Vehicle.Wheels[0].Id).GetComponent<Renderer>().enabled,
                "Imported model asset must never be modified.");
        }

        [Test]
        public void InvalidVisualScaleIsCaughtBeforeAnySceneConstruction()
        {
            var binding = MakeProfile(VehicleTemplate.PassengerCar4, false, 0);
            binding.ModelScale = float.NaN;
            Assert.IsTrue(VehicleImportedVisualValidator.HasErrors(binding));
            Assert.Throws<InvalidOperationException>(
                () => VehicleImportedVisualBuilder.Build(binding, false));
            binding.ModelScale = -1f;
            Assert.IsTrue(VehicleImportedVisualValidator.HasErrors(binding));
        }
    }
}
