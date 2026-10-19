using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Strict but non-mutating preflight for imported model bindings.
    /// Never guesses/saves wheel IDs or changes ModelPrefab on import.
    /// </summary>
    public static class VehicleImportedVisualValidator
    {
        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool Finite(Vector3 value) =>
            Finite(value.x) && Finite(value.y) && Finite(value.z);

        public static Transform FindNode(GameObject modelPrefab, string path)
        {
            if (modelPrefab == null || string.IsNullOrEmpty(path)) return null;
            if (path.StartsWith("/", StringComparison.Ordinal) ||
                path.EndsWith("/", StringComparison.Ordinal))
                return null;
            return modelPrefab.transform.Find(path);
        }

        public static List<VehicleValidationIssue> Validate(
            VehicleVisualBindingDefinition binding)
        {
            var issues = new List<VehicleValidationIssue>();
            if (binding == null)
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Assign or create a VehicleVisualBindingDefinition asset."));
                return issues;
            }
            if (binding.Vehicle == null)
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Visual binding requires an existing VehicleDefinition asset."));
                return issues;
            }
            foreach (VehicleValidationIssue vehicleIssue in
                     VehicleConfigurationValidator.Validate(binding.Vehicle))
                if (vehicleIssue.Severity == ValidationSeverity.Error)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Vehicle: " + vehicleIssue.Message));

            if (!Finite(binding.ModelScale) || binding.ModelScale <= 0f ||
                !Finite(binding.ModelLocalOffset) || !Finite(binding.ModelLocalEuler))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Model scale must be positive and alignment coordinates finite."));

            GameObject source = binding.ModelPrefab;
            if (source == null || !AssetDatabase.Contains(source) ||
                !PrefabUtility.IsPartOfPrefabAsset(source))
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Choose a prefab or imported FBX/GLB GameObject asset from Project, not a scene object."));
                return issues;
            }
            if (source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "P2.0 supports static MeshRenderer/MeshFilter geometry only. " +
                    "Skinned meshes/animations need a future binding adapter."));

            int visibleMeshes = 0;
            foreach (MeshRenderer renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.GetComponent<MeshFilter>() == null ||
                    renderer.GetComponent<MeshFilter>().sharedMesh == null)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "MeshRenderer has no MeshFilter/sharedMesh: " + renderer.name));
                else visibleMeshes++;
            }
            if (visibleMeshes == 0)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Imported model has no MeshFilter/MeshRenderer geometry."));

            var validIds = new HashSet<string>(StringComparer.Ordinal);
            if (binding.Vehicle.Wheels != null)
                foreach (WheelDefinition wheel in binding.Vehicle.Wheels)
                    if (wheel != null) validIds.Add(wheel.Id);

            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            var usedPaths = new List<string>();
            int mappedCount = 0;
            if (binding.WheelVisuals != null)
            {
                foreach (VehicleImportedWheelBinding item in binding.WheelVisuals)
                {
                    if (item == null || !validIds.Contains(item.WheelId) ||
                        !usedIds.Add(item.WheelId))
                    {
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel binding has an unknown or repeated WheelId."));
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(item.ModelNodePath))
                        continue; // User can keep a procedural placeholder wheel.

                    string path = item.ModelNodePath;
                    Transform node = FindNode(source, path);
                    if (node == null || node == source.transform)
                    {
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel " + item.WheelId + " has a missing model node: " + path));
                        continue;
                    }
                    bool nestedOrRepeated = false;
                    foreach (string previous in usedPaths)
                        if (path == previous ||
                            path.StartsWith(previous + "/", StringComparison.Ordinal) ||
                            previous.StartsWith(path + "/", StringComparison.Ordinal))
                        {
                            nestedOrRepeated = true;
                            break;
                        }
                    if (nestedOrRepeated)
                    {
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel visual nodes must be unique and non-overlapping: " + path));
                        continue;
                    }
                    if (node.GetComponentInChildren<MeshRenderer>(true) == null)
                    {
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel node has no mesh renderer: " + path));
                        continue;
                    }
                    usedPaths.Add(path);
                    mappedCount++;
                }
            }
            if (mappedCount == 0)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Warning,
                    "No wheel meshes assigned; KAVRA procedural preview wheels remain. " +
                    "If the imported chassis includes static wheels, map their nodes."));

            return issues;
        }

        public static bool HasErrors(VehicleVisualBindingDefinition binding)
        {
            foreach (VehicleValidationIssue issue in Validate(binding))
                if (issue.Severity == ValidationSeverity.Error) return true;
            return false;
        }
    }
}
