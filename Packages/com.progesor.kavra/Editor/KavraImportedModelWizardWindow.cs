using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Import-agnostic visual binder for FBX and already imported model
    /// prefabs (including GLB when the host project has a GLB importer).
    /// This window only edits a separate profile, never the model source.
    /// </summary>
    public sealed class KavraImportedModelWizardWindow : EditorWindow
    {
        private VehicleDefinition vehicle;
        private VehicleVisualBindingDefinition binding;
        private Vector2 scroll;

        [MenuItem("Tools/KAVRA/Imported Model Binding Wizard")]
        public static void Open()
        {
            var window = GetWindow<KavraImportedModelWizardWindow>("KAVRA Model Binder");
            window.minSize = new Vector2(540f, 510f);
        }

        public static void OpenFor(VehicleDefinition definition)
        {
            Open();
            var window = GetWindow<KavraImportedModelWizardWindow>();
            window.vehicle = definition;
            window.Repaint();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("KAVRA P2 — Imported Model Binding",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose a prefab / imported FBX GameObject from Project. " +
                "This copies visual mesh references only; the existing " +
                "Rigidbody, wheel physics and KAVRA pivots stay in control. " +
                "GLB requires a separate importer in the Unity host.",
                MessageType.Info);
            vehicle = (VehicleDefinition)EditorGUILayout.ObjectField(
                "VehicleDefinition", vehicle, typeof(VehicleDefinition), false);
            binding = (VehicleVisualBindingDefinition)EditorGUILayout.ObjectField(
                "Binding profile", binding, typeof(VehicleVisualBindingDefinition), false);
            if (vehicle == null)
            {
                EditorGUILayout.HelpBox("Select an existing VehicleDefinition asset.",
                    MessageType.None);
                EditorGUILayout.EndScrollView();
                return;
            }
            if (binding == null)
            {
                if (GUILayout.Button("Create separate visual binding asset"))
                    CreateBinding();
                EditorGUILayout.EndScrollView();
                return;
            }

            if (binding.Vehicle != vehicle)
            {
                EditorGUILayout.HelpBox(
                    "Selected vehicle differs from the profile VehicleDefinition.",
                    MessageType.Warning);
                if (GUILayout.Button("Assign selected vehicle to profile"))
                {
                    Undo.RecordObject(binding, "Change KAVRA visual VehicleDefinition");
                    binding.Vehicle = vehicle;
                    EditorUtility.SetDirty(binding);
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("1. Model alignment", EditorStyles.boldLabel);
            GameObject prefab = (GameObject)EditorGUILayout.ObjectField(
                "Imported model", binding.ModelPrefab, typeof(GameObject), false);
            Vector3 offset = EditorGUILayout.Vector3Field(
                "Local offset (meters)", binding.ModelLocalOffset);
            Vector3 euler = EditorGUILayout.Vector3Field(
                "Local orientation", binding.ModelLocalEuler);
            float scale = EditorGUILayout.FloatField(
                "Uniform scale", binding.ModelScale);
            bool hideBody = EditorGUILayout.Toggle(
                "Hide prototype body mesh", binding.HideScaffoldChassisRenderer);
            bool hideWheels = EditorGUILayout.Toggle(
                "Hide mapped wheel previews", binding.HideMatchedWheelPreviews);
            if (prefab != binding.ModelPrefab || offset != binding.ModelLocalOffset ||
                euler != binding.ModelLocalEuler ||
                !Mathf.Approximately(scale, binding.ModelScale) ||
                hideBody != binding.HideScaffoldChassisRenderer ||
                hideWheels != binding.HideMatchedWheelPreviews)
            {
                Undo.RecordObject(binding, "Update KAVRA visual alignment");
                binding.ModelPrefab = prefab;
                binding.ModelLocalOffset = offset;
                binding.ModelLocalEuler = euler;
                binding.ModelScale = scale;
                binding.HideScaffoldChassisRenderer = hideBody;
                binding.HideMatchedWheelPreviews = hideWheels;
                EditorUtility.SetDirty(binding);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. Wheel visual nodes", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Each wheel mesh pivot should be at its own wheel center. " +
                "Paths are relative to the imported model root. " +
                "Unassigned wheels keep their cylinder preview; no automatic " +
                "scale or physics asset migration is performed.",
                MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add missing wheel rows")) AddMissingRows();
                using (new EditorGUI.DisabledScope(binding.ModelPrefab == null))
                    if (GUILayout.Button("Suggest matches from names"))
                        SuggestNames();
            }

            List<string> candidatePaths = CandidatePaths(binding.ModelPrefab);
            var names = new string[candidatePaths.Count + 1];
            names[0] = "(unassigned)";
            for (int i = 0; i < candidatePaths.Count; i++)
                names[i + 1] = candidatePaths[i];

            if (vehicle.Wheels != null)
                foreach (WheelDefinition wheel in vehicle.Wheels)
                {
                    if (wheel == null) continue;
                    VehicleImportedWheelBinding item = FindSlot(wheel.Id);
                    if (item == null)
                    {
                        EditorGUILayout.LabelField(wheel.Id +
                            " — select Add missing wheel rows");
                        continue;
                    }
                    EditorGUILayout.LabelField(wheel.Id, EditorStyles.miniBoldLabel);
                    string current = item.ModelNodePath ?? "";
                    string text = EditorGUILayout.TextField("  Node path", current);
                    if (text != current) SetPath(item, text);
                    int index = candidatePaths.IndexOf(current) + 1;
                    int selected = EditorGUILayout.Popup(
                        "  Browse hierarchy", index, names);
                    if (selected != index)
                        SetPath(item, selected == 0 ? "" : candidatePaths[selected - 1]);
                }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3. Validate & create", EditorStyles.boldLabel);
            var issues = VehicleImportedVisualValidator.Validate(binding);
            bool invalid = binding.Vehicle != vehicle;
            foreach (VehicleValidationIssue issue in issues)
            {
                if (issue.Severity == ValidationSeverity.Error) invalid = true;
                EditorGUILayout.HelpBox(issue.Message,
                    issue.Severity == ValidationSeverity.Error ? MessageType.Error :
                    issue.Severity == ValidationSeverity.Warning ? MessageType.Warning :
                    MessageType.Info);
            }
            EditorGUILayout.HelpBox(
                "P2.0 retains the physics box collider behind the imported " +
                "body renderers. Mesh-based chassis collision and rigged " +
                "suspension parts are later phases.", MessageType.None);
            using (new EditorGUI.DisabledScope(invalid))
            {
                if (GUILayout.Button("Build model-bound vehicle in scene",
                    GUILayout.Height(32f)))
                    VehicleImportedVisualBuilder.Build(binding, true);
                if (GUILayout.Button("Save reusable model-bound prefab",
                    GUILayout.Height(32f)))
                {
                    AssetDatabase.SaveAssets();
                    VehicleImportedVisualBuilder.SavePrefab(binding);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void CreateBinding()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save separate visual binding", "KAVRA_VisualBinding.asset",
                "asset", "Choose a new mapping asset (existing files stay untouched).");
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA",
                    "Asset already exists; choose another filename.", "OK");
                return;
            }
            var asset = ScriptableObject.CreateInstance<VehicleVisualBindingDefinition>();
            asset.Vehicle = vehicle;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            binding = asset;
            AddMissingRows();
            EditorGUIUtility.PingObject(binding);
        }

        private VehicleImportedWheelBinding FindSlot(string id)
        {
            if (binding == null || binding.WheelVisuals == null) return null;
            foreach (VehicleImportedWheelBinding item in binding.WheelVisuals)
                if (item != null && item.WheelId == id) return item;
            return null;
        }

        private void AddMissingRows()
        {
            if (vehicle == null || vehicle.Wheels == null || binding == null)
                return;
            Undo.RecordObject(binding, "Add imported wheel binding rows");
            if (binding.WheelVisuals == null)
                binding.WheelVisuals = new List<VehicleImportedWheelBinding>();
            foreach (WheelDefinition wheel in vehicle.Wheels)
                if (wheel != null && FindSlot(wheel.Id) == null)
                    binding.WheelVisuals.Add(new VehicleImportedWheelBinding
                    { WheelId = wheel.Id, ModelNodePath = "" });
            EditorUtility.SetDirty(binding);
        }

        private void SetPath(VehicleImportedWheelBinding item, string value)
        {
            Undo.RecordObject(binding, "Edit imported wheel mesh node");
            item.ModelNodePath = value;
            EditorUtility.SetDirty(binding);
        }

        private void SuggestNames()
        {
            AddMissingRows();
            List<string> candidates = CandidatePaths(binding.ModelPrefab);
            var reserved = new HashSet<string>(StringComparer.Ordinal);
            foreach (VehicleImportedWheelBinding item in binding.WheelVisuals)
                if (item != null && !string.IsNullOrWhiteSpace(item.ModelNodePath))
                    reserved.Add(item.ModelNodePath);
            foreach (WheelDefinition wheel in vehicle.Wheels)
            {
                if (wheel == null) continue;
                var item = FindSlot(wheel.Id);
                if (item == null || !string.IsNullOrWhiteSpace(item.ModelNodePath))
                    continue;
                string wheelName = Normalize(wheel.Id);
                string best = "";
                int bestScore = 0;
                foreach (string candidate in candidates)
                {
                    if (reserved.Contains(candidate)) continue;
                    string simpleName = candidate.Substring(candidate.LastIndexOf('/') + 1);
                    string key = Normalize(simpleName);
                    int score = key == wheelName ? 100 :
                        key.EndsWith(wheelName, StringComparison.Ordinal) ? 80 :
                        wheelName.Length > 2 && key.Contains(wheelName) ? 50 : 0;
                    if (score > bestScore) { bestScore = score; best = candidate; }
                }
                if (bestScore >= 50)
                {
                    SetPath(item, best);
                    reserved.Add(best);
                }
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var chars = new System.Text.StringBuilder(value.Length);
            foreach (char ch in value)
                if (char.IsLetterOrDigit(ch))
                    chars.Append(char.ToLowerInvariant(ch));
            return chars.ToString();
        }

        private static List<string> CandidatePaths(GameObject source)
        {
            var result = new List<string>();
            if (source != null)
                Collect(source.transform, "", result);
            return result;
        }

        private static void Collect(Transform node, string prefix, List<string> result)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                string path = prefix.Length == 0 ? child.name : prefix + "/" + child.name;
                if (child.GetComponentInChildren<MeshRenderer>(true) != null)
                    result.Add(path);
                Collect(child, path, result);
            }
        }
    }
}
