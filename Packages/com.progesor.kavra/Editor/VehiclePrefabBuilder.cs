using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Save a functional but unskinned/placeholder M1 chassis as a prefab.
    /// The prefab references VehicleDefinition + SimplePowertrainDefinition
    /// assets from the host project and requires external scene ground/input.
    /// </summary>
    internal static class VehiclePrefabBuilder
    {
        public static bool SaveSimplePowertrainPrefab(VehicleDefinition configuration)
        {
            if (configuration == null || configuration.Powertrain == null)
            {
                EditorUtility.DisplayDialog("KAVRA M1",
                    "Assign a VehicleDefinition and powertrain first.", "OK");
                return false;
            }

            foreach (VehicleValidationIssue issue in
                     VehicleConfigurationValidator.Validate(configuration))
            {
                if (issue.Severity != ValidationSeverity.Error) continue;
                EditorUtility.DisplayDialog("KAVRA M1",
                    "Invalid vehicle: " + issue.Message, "OK");
                return false;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save KAVRA M1 Prefab", "KAVRA_M1_Vehicle.prefab",
                "prefab", "Choose a destination under Assets/.");
            if (string.IsNullOrEmpty(path)) return false;

            // Never overwrite an existing user-authored prefab silently.
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA M1",
                    "Destination already exists. Choose another filename.", "OK");
                return false;
            }

            GameObject temporary = null;
            try
            {
                temporary = VehicleScaffoldBuilder.Create(configuration, true,
                    VehicleDriveSource.SimplePowertrain);
                bool success;
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    temporary, path, out success);
                if (!success || saved == null)
                {
                    Debug.LogError("KAVRA M1: PrefabUtility did not save a prefab at " + path);
                    return false;
                }

                Selection.activeObject = saved;
                EditorGUIUtility.PingObject(saved);
                Debug.Log("KAVRA M1: saved reusable prototype prefab at " +
                          path + " (placeholder body and wheel meshes).");
                return true;
            }
            catch (System.Exception error)
            {
                Debug.LogError("KAVRA M1: failed to save prototype prefab: " + error);
                return false;
            }
            finally
            {
                if (temporary != null)
                    Undo.DestroyObjectImmediate(temporary);
            }
        }
    }
}
