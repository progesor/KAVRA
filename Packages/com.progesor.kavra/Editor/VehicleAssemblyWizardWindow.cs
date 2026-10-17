using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>A thin Editor interface for data-driven prefab/scene assembly.</summary>
    public sealed class VehicleAssemblyWizardWindow : EditorWindow
    {
        private VehicleAssemblyDefinition assemblyDefinition;
        private VehicleDefinition tractor;
        private VehicleDefinition trailer;
        private Vector2 scroll;

        [MenuItem("Tools/KAVRA/Vehicle Assembly Wizard")]
        public static void Open()
        {
            GetWindow<VehicleAssemblyWizardWindow>("KAVRA Assembly")
                .minSize = new Vector2(470f, 380f);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("KAVRA Multi-Body Assembly", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "One driven tractor + one independently simulated towed trailer. " +
                "Separate Rigidbody components, connected by a ConfigurableJoint. " +
                "Free yaw, limited pitch/roll, no automatic rollover prevention.",
                MessageType.Info);

            assemblyDefinition = (VehicleAssemblyDefinition)
                EditorGUILayout.ObjectField("Assembly definition", assemblyDefinition,
                    typeof(VehicleAssemblyDefinition), false);

            if (assemblyDefinition == null)
            {
                EditorGUILayout.LabelField("New definition", EditorStyles.boldLabel);
                tractor = (VehicleDefinition)EditorGUILayout.ObjectField(
                    "Tractor vehicle", tractor, typeof(VehicleDefinition), false);
                trailer = (VehicleDefinition)EditorGUILayout.ObjectField(
                    "Towed vehicle", trailer, typeof(VehicleDefinition), false);
                using (new EditorGUI.DisabledScope(tractor == null || trailer == null))
                    if (GUILayout.Button("Create VehicleAssemblyDefinition"))
                        CreateAsset();
                EditorGUILayout.HelpBox("You can also start from the generated " +
                    "Tools > KAVRA > Create M1.3 Tractor Trailer Lab Scene.",
                    MessageType.None);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("References and hitch geometry", EditorStyles.boldLabel);
            Editor editor = UnityEditor.Editor.CreateEditor(assemblyDefinition);
            if (editor != null)
            {
                editor.OnInspectorGUI();
                DestroyImmediate(editor);
            }

            bool valid = true;
            foreach (VehicleValidationIssue issue in
                     VehicleAssemblyValidator.Validate(assemblyDefinition))
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    valid = false;
                    EditorGUILayout.HelpBox(issue.Message, MessageType.Error);
                }
            }
            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button("Build Tractor + Trailer in Scene", GUILayout.Height(30f)))
                    VehicleAssemblyBuilder.Build(assemblyDefinition);
                if (GUILayout.Button("Save Reusable Assembly Prefab", GUILayout.Height(30f)))
                    VehicleAssemblyBuilder.SaveAssemblyPrefab(assemblyDefinition);
            }
            EditorGUILayout.EndScrollView();
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create KAVRA Vehicle Assembly", "KAVRA_VehicleAssembly.asset",
                "asset", "Save assembly configuration under Assets/.");
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA Assembly",
                    "This asset exists. Choose a different path.", "OK");
                return;
            }

            var definition = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            definition.Tractor = tractor;
            definition.Trailer = trailer;
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            assemblyDefinition = definition;
            Selection.activeObject = definition;
        }
    }
}
