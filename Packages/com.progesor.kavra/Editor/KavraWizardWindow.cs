using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    public sealed class KavraWizardWindow : EditorWindow
    {
        private VehicleTemplate template = VehicleTemplate.PassengerCar4;
        private VehicleDefinition configuration;
        private Vector2 scroll;

        [MenuItem("Tools/KAVRA/Vehicle Setup Wizard")]
        public static void Open()
        {
            GetWindow<KavraWizardWindow>("KAVRA Wizard").minSize = new Vector2(425f, 345f);
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            EditorGUILayout.LabelField("KAVRA Vehicle Framework", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "M0 foundation only: generates a configuration asset and visible, kinematic " +
                "vehicle scaffold. This is NOT a driving vehicle. Tire, suspension and drivetrain " +
                "solvers will be implemented and tested in later milestones.", MessageType.Info);
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("1. Choose a starter vehicle", EditorStyles.boldLabel);
            template = (VehicleTemplate)EditorGUILayout.EnumPopup("Template", template);
            if (GUILayout.Button("Create VehicleDefinition Asset")) CreateAsset();
            GUILayout.Space(10f);
            EditorGUILayout.LabelField("2. Validate or edit an existing asset", EditorStyles.boldLabel);
            configuration = (VehicleDefinition)EditorGUILayout.ObjectField("Definition", configuration,
                typeof(VehicleDefinition), false);
            if (configuration == null)
            {
                EditorGUILayout.HelpBox("Create an asset above or drag a VehicleDefinition here.", MessageType.None);
                return;
            }
            var issues = VehicleConfigurationValidator.Validate(configuration);
            var containsErrors = false;
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(160f));
            foreach (var issue in issues)
            {
                var type = issue.Severity == ValidationSeverity.Error ? MessageType.Error :
                    issue.Severity == ValidationSeverity.Warning ? MessageType.Warning : MessageType.Info;
                if (issue.Severity == ValidationSeverity.Error) containsErrors = true;
                EditorGUILayout.HelpBox(issue.Message, type);
            }
            EditorGUILayout.EndScrollView();
            using (new EditorGUI.DisabledScope(containsErrors))
            {
                if (GUILayout.Button("3. Build non-drivable scene scaffold", GUILayout.Height(30f)))
                {
                    var root = VehicleScaffoldBuilder.Create(configuration);
                    Debug.Log("KAVRA M0: created " + root.name +
                              " with " + configuration.Wheels.Count + " wheel anchors (kinematic preview only).");
                }
            }
        }

        private void CreateAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create KAVRA Definition",
                "KAVRA_" + template + ".asset", "asset",
                "Select a location in Assets/ for this configuration.");
            if (string.IsNullOrEmpty(path)) return;
            var definition = VehicleTemplateFactory.Create(template);
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            configuration = definition;
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
            Debug.Log("KAVRA M0: saved vehicle definition at " + path);
        }
    }
}
