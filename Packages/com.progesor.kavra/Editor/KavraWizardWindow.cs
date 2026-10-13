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
            GetWindow<KavraWizardWindow>("KAVRA Wizard").minSize = new Vector2(450f, 410f);
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            EditorGUILayout.LabelField("KAVRA Vehicle Framework", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "M0 experimental physics: spring/damper + raycast road contact + simple " +
                "tire slip and wheel torque. No engine, gearbox, advanced terrain or " +
                "calibrated simulation fidelity yet.", MessageType.Info);
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("1. Starter layout", EditorStyles.boldLabel);
            template = (VehicleTemplate)EditorGUILayout.EnumPopup("Template", template);
            if (GUILayout.Button("Create VehicleDefinition Asset")) CreateAsset();

            GUILayout.Space(10f);
            EditorGUILayout.LabelField("2. Validate existing vehicle asset", EditorStyles.boldLabel);
            configuration = (VehicleDefinition)EditorGUILayout.ObjectField("Definition", configuration,
                typeof(VehicleDefinition), false);
            if (configuration == null)
            {
                EditorGUILayout.HelpBox("Create an asset above or drag an existing VehicleDefinition here.",
                    MessageType.None);
                return;
            }

            var issues = VehicleConfigurationValidator.Validate(configuration);
            bool hasErrors = false;
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(145f));
            foreach (VehicleValidationIssue issue in issues)
            {
                MessageType type = issue.Severity == ValidationSeverity.Error ? MessageType.Error :
                    issue.Severity == ValidationSeverity.Warning ? MessageType.Warning : MessageType.Info;
                if (issue.Severity == ValidationSeverity.Error) hasErrors = true;
                EditorGUILayout.HelpBox(issue.Message, type);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(hasErrors))
            {
                if (GUILayout.Button("3A. Build non-drivable preview scaffold", GUILayout.Height(29f)))
                {
                    GameObject preview = VehicleScaffoldBuilder.Create(configuration);
                    Debug.Log("KAVRA: created kinematic preview " + preview.name);
                }

                bool supportsDynamic = configuration.Wheels != null &&
                    configuration.Wheels.TrueForAll(w => w != null && w.SuspensionTravelMeters > 0f);
                using (new EditorGUI.DisabledScope(!supportsDynamic))
                {
                    if (GUILayout.Button("3B. Build dynamic M0 test chassis", GUILayout.Height(32f)))
                    {
                        GameObject vehicle = VehicleScaffoldBuilder.Create(configuration, true);
                        Debug.Log("KAVRA M0: created dynamic prototype " + vehicle.name +
                                  ". Add ground and inputs, or use Tools/KAVRA/Create M0 Physics Lab Scene.");
                    }
                }
                if (!supportsDynamic)
                    EditorGUILayout.HelpBox("Dynamic M0 requires positive suspension travel on every wheel.",
                        MessageType.Warning);
            }
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create KAVRA Definition",
                "KAVRA_" + template + ".asset", "asset",
                "Select a location in Assets/ for this configuration.");
            if (string.IsNullOrEmpty(path)) return;
            VehicleDefinition definition = VehicleTemplateFactory.Create(template);
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            configuration = definition;
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
            Debug.Log("KAVRA: saved vehicle definition at " + path);
        }
    }
}
