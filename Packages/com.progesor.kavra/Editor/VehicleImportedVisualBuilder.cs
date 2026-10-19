using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// P2.0 imported mesh -> KAVRA prefab bridge. Creates mesh-only clones,
    /// preserving shared Mesh/Material asset references but never copying
    /// imported physics, colliders, scripts, animation or Undo into temporary
    /// prefab stages. Imported source prefab/FBX/GLB is NEVER edited.
    /// </summary>
    public static class VehicleImportedVisualBuilder
    {
        public static GameObject Build(VehicleVisualBindingDefinition binding)
        {
            return Build(binding, true);
        }

        public static GameObject Build(VehicleVisualBindingDefinition binding,
            bool recordUndo)
        {
            foreach (VehicleValidationIssue issue in VehicleImportedVisualValidator.Validate(binding))
                if (issue.Severity == ValidationSeverity.Error)
                    throw new InvalidOperationException(
                        "KAVRA imported model: " + issue.Message);

            VehicleDefinition config = binding.Vehicle;
            VehicleDriveSource drive = config.Powertrain != null
                ? VehicleDriveSource.SimplePowertrain
                : VehicleDriveSource.LegacyDirectWheelTorque;
            GameObject vehicle = VehicleScaffoldBuilder.Create(
                config, true, drive, recordUndo);

            try
            {
                var info = recordUndo
                    ? Undo.AddComponent<VehicleImportedVisualRoot>(vehicle)
                    : vehicle.AddComponent<VehicleImportedVisualRoot>();
                info.Binding = binding;

                // Existing M1 powered scaffolds already have the travel/steer/
                // spin rigs. Other vehicles gain the same rig without altering
                // their original WheelAnchor / simulation configuration.
                VehicleWheelVisualRigBuilder.Bind(vehicle, false, recordUndo);

                var excluded = new HashSet<Transform>();
                if (binding.WheelVisuals != null)
                    foreach (VehicleImportedWheelBinding item in binding.WheelVisuals)
                        if (item != null && !string.IsNullOrWhiteSpace(item.ModelNodePath))
                        {
                            Transform node = VehicleImportedVisualValidator.FindNode(
                                binding.ModelPrefab, item.ModelNodePath);
                            if (node != null) excluded.Add(node);
                        }

                Transform holder = CreateObject("__KAVRA_IMPORTED_BODY", vehicle.transform,
                    recordUndo).transform;
                holder.localPosition = binding.ModelLocalOffset;
                holder.localRotation = Quaternion.Euler(binding.ModelLocalEuler);
                holder.localScale = Vector3.one * binding.ModelScale;
                info.ImportedBodyVisual = holder;

                // Extract renderer-only body hierarchy. Wheel subtrees are
                // deliberately excluded so they cannot remain static while
                // an animated clone is added to the corresponding wheel rig.
                CloneVisualTree(binding.ModelPrefab.transform, holder,
                    excluded, recordUndo);

                var anchorById = new Dictionary<string, VehicleWheelAnchor>(
                    StringComparer.Ordinal);
                foreach (VehicleWheelAnchor anchor in
                         vehicle.GetComponentsInChildren<VehicleWheelAnchor>(true))
                    if (anchor != null && !anchorById.ContainsKey(anchor.WheelId))
                        anchorById.Add(anchor.WheelId, anchor);

                if (binding.WheelVisuals != null)
                    foreach (VehicleImportedWheelBinding wheel in binding.WheelVisuals)
                    {
                        if (wheel == null || string.IsNullOrWhiteSpace(wheel.ModelNodePath))
                            continue;
                        VehicleWheelAnchor anchor;
                        if (!anchorById.TryGetValue(wheel.WheelId, out anchor))
                            continue; // Already rejected by validator.

                        Transform sourceWheel = VehicleImportedVisualValidator.FindNode(
                            binding.ModelPrefab, wheel.ModelNodePath);
                        var rig = anchor.GetComponent<VehicleWheelVisualRig>();
                        if (sourceWheel == null || rig == null || rig.SpinPivot == null)
                            throw new InvalidOperationException(
                                "Missing wheel pivot rig for " + wheel.WheelId);

                        // Root pivot of imported mesh is assumed to be at the
                        // center of its wheel. Its original chassis-relative
                        // translation is replaced by KAVRA WheelAnchor.
                        // Root rotation/scale retain the model import pose.
                        Transform copy = CloneVisualTree(sourceWheel,
                            rig.SpinPivot, null, recordUndo);
                        copy.name = "__KAVRA_IMPORTED_WHEEL_" + wheel.WheelId;
                        copy.localPosition = Vector3.zero;
                        copy.localRotation =
                            Quaternion.Euler(binding.ModelLocalEuler) * sourceWheel.rotation;
                        Vector3 modelScale = sourceWheel.lossyScale;
                        copy.localScale = modelScale * binding.ModelScale;
                        anchor.VisualTransform = copy;

                        if (binding.HideMatchedWheelPreviews)
                            foreach (Renderer preview in
                                     anchor.GetComponentsInChildren<Renderer>(true))
                                if (preview.gameObject.name == "__WHEEL_PREVIEW")
                                    preview.enabled = false;

                        rig.CaptureInitialPose();
                    }

                if (binding.HideScaffoldChassisRenderer)
                    foreach (Renderer renderer in
                             vehicle.GetComponentsInChildren<Renderer>(true))
                        if (renderer.gameObject.name == "__CHASSIS_TEST_COLLIDER")
                            renderer.enabled = false; // Keep BoxCollider + Rigidbody intact.

                if (recordUndo)
                {
                    Selection.activeGameObject = vehicle;
                    EditorGUIUtility.PingObject(vehicle);
                }
                return vehicle;
            }
            catch
            {
                if (recordUndo) Undo.DestroyObjectImmediate(vehicle);
                else UnityEngine.Object.DestroyImmediate(vehicle);
                throw;
            }
        }

        private static GameObject CreateObject(string name, Transform parent,
            bool recordUndo)
        {
            var go = new GameObject(name);
            if (recordUndo)
                Undo.RegisterCreatedObjectUndo(go, "Create KAVRA imported visual");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Transform CloneVisualTree(Transform source, Transform parent,
            HashSet<Transform> excluded, bool recordUndo)
        {
            if (source == null) return null;
            GameObject copy = CreateObject(source.name, parent, recordUndo);
            copy.transform.localPosition = source.localPosition;
            copy.transform.localRotation = source.localRotation;
            copy.transform.localScale = source.localScale;

            MeshFilter filter = source.GetComponent<MeshFilter>();
            MeshRenderer originalRenderer = source.GetComponent<MeshRenderer>();
            if (filter != null && filter.sharedMesh != null && originalRenderer != null)
            {
                MeshFilter mesh = recordUndo
                    ? Undo.AddComponent<MeshFilter>(copy)
                    : copy.AddComponent<MeshFilter>();
                mesh.sharedMesh = filter.sharedMesh;
                MeshRenderer renderer = recordUndo
                    ? Undo.AddComponent<MeshRenderer>(copy)
                    : copy.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = originalRenderer.sharedMaterials;
                renderer.enabled = originalRenderer.enabled;
                renderer.shadowCastingMode = originalRenderer.shadowCastingMode;
                renderer.receiveShadows = originalRenderer.receiveShadows;
            }

            for (int i = 0; i < source.childCount; i++)
            {
                Transform child = source.GetChild(i);
                if (excluded == null || !excluded.Contains(child))
                    CloneVisualTree(child, copy.transform, excluded, recordUndo);
            }
            return copy.transform;
        }

        public static bool SavePrefab(VehicleVisualBindingDefinition binding)
        {
            if (VehicleImportedVisualValidator.HasErrors(binding))
            {
                EditorUtility.DisplayDialog("KAVRA Imported Model",
                    "Fix the visual binding configuration errors first.", "OK");
                return false;
            }
            string path = EditorUtility.SaveFilePanelInProject(
                "Save KAVRA Imported Vehicle", "KAVRA_ImportedVehicle.prefab",
                "prefab", "Save a reusable vehicle with imported mesh references.");
            if (string.IsNullOrEmpty(path)) return false;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA Imported Model",
                    "Destination exists. Choose a new prefab filename.", "OK");
                return false;
            }

            GameObject temporary = null;
            try
            {
                temporary = Build(binding, false);
                bool savedOk;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporary, path, out savedOk);
                if (!savedOk || prefab == null) return false;
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("KAVRA P2: imported visual vehicle prefab saved at " + path);
                return true;
            }
            finally
            {
                if (temporary != null)
                    UnityEngine.Object.DestroyImmediate(temporary);
            }
        }
    }
}
