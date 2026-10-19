using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Small native Unity Wizard for ordered assembly chain assets.
    /// Links may reuse a vehicle as NEXT Tractor only, never same asset
    /// twice as a chassis node.
    /// </summary>
    public sealed class VehicleAssemblyChainWizardWindow : EditorWindow
    {
        private VehicleAssemblyChainDefinition definition;
        private Vector2 scroll;

        [MenuItem("Tools/KAVRA/Vehicle Chain Wizard")]
        public static void Open()
        {
            GetWindow<VehicleAssemblyChainWizardWindow>("KAVRA Chain")
                .minSize = new Vector2(500f, 400f);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("KAVRA — Multiple Trailer Chain",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Ordered chain of 2..17 independent Rigidbody vehicles. " +
                "Link 0: lead tractor -> trailer A, Link 1: trailer A -> " +
                "trailer B, and so on. No branching/cycles. Each link " +
                "owns a separate breakable ConfigurableJoint.",
                MessageType.Info);

            definition = (VehicleAssemblyChainDefinition)
                EditorGUILayout.ObjectField("Chain definition", definition,
                    typeof(VehicleAssemblyChainDefinition), false);
            if (definition == null)
            {
                if (GUILayout.Button("Create empty ChainDefinition asset"))
                    CreateAsset();
                EditorGUILayout.HelpBox(
                    "Create each VehicleAssemblyDefinition link asset using " +
                    "Assets > Create > KAVRA, or generate the demonstration " +
                    "from Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene.",
                    MessageType.None);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(6f);
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(definition);
            if (editor != null)
            {
                editor.OnInspectorGUI();
                DestroyImmediate(editor);
            }

            bool invalid = false;
            foreach (VehicleValidationIssue problem in
                     VehicleAssemblyChainValidator.Validate(definition))
                if (problem.Severity == ValidationSeverity.Error)
                {
                    invalid = true;
                    EditorGUILayout.HelpBox(problem.Message, MessageType.Error);
                }

            using (new EditorGUI.DisabledScope(invalid))
            {
                if (GUILayout.Button("Build Vehicle Chain in Scene", GUILayout.Height(32f)))
                    VehicleAssemblyChainBuilder.Build(definition);
                if (GUILayout.Button("Save N-Body Prefab", GUILayout.Height(32f)))
                    VehicleAssemblyChainBuilder.SavePrefab(definition);
            }
            EditorGUILayout.EndScrollView();
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create KAVRA Vehicle Chain", "KAVRA_AssemblyChain.asset",
                "asset", "A list of ordered tractor-trailer link assets.");
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA",
                    "Asset already exists. Choose another filename.", "OK");
                return;
            }
            var asset = ScriptableObject.CreateInstance<VehicleAssemblyChainDefinition>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            definition = asset;
            Selection.activeObject = asset;
        }
    }
}
