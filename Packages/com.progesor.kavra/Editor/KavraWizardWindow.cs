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
                "M1 foundation: configure variable wheel layouts, create an optional " +
                "basic engine/gearbox asset, and build either the existing M0 " +
                "direct-torque chassis or the new simple powertrain prototype. " +
                "Clutch/differential physics and real-world calibration are later.", MessageType.Info);
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

            GUILayout.Space(7f);
            EditorGUILayout.LabelField("3. Powertrain asset (optional)", EditorStyles.boldLabel);
            SimplePowertrainDefinition powertrain = (SimplePowertrainDefinition)
                EditorGUILayout.ObjectField("Powertrain", configuration.Powertrain,
                    typeof(SimplePowertrainDefinition), false);
            if (powertrain != configuration.Powertrain)
            {
                Undo.RecordObject(configuration, "Assign KAVRA powertrain");
                configuration.Powertrain = powertrain;
                EditorUtility.SetDirty(configuration);
            }
            if (GUILayout.Button("Create Basic Powertrain & assign"))
                CreatePowertrainAsset();

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
                    if (GUILayout.Button("4B. Build dynamic M0 test chassis", GUILayout.Height(32f)))
                    {
                        GameObject vehicle = VehicleScaffoldBuilder.Create(configuration, true);
                        Debug.Log("KAVRA M0: created dynamic prototype " + vehicle.name +
                                  ". Add ground and inputs, or use Tools/KAVRA/Create M0 Physics Lab Scene.");
                    }
                }
                using (new EditorGUI.DisabledScope(!supportsDynamic || configuration.Powertrain == null))
                {
                    if (GUILayout.Button("4C. Build M1 powertrain chassis", GUILayout.Height(32f)))
                    {
                        GameObject vehicle = VehicleScaffoldBuilder.Create(configuration,
                            true, VehicleDriveSource.SimplePowertrain);
                        Debug.Log("KAVRA M1: created powered prototype " + vehicle.name +
                                  ". Use an Input adapter and ground or create the M1 lab scene.");
                    }
                    if (GUILayout.Button("4D. Save M1 powered prefab (reusable)", GUILayout.Height(32f)))
                    {
                        VehiclePrefabBuilder.SaveSimplePowertrainPrefab(configuration);
                    }
                }
                if (!supportsDynamic)
                    EditorGUILayout.HelpBox("Dynamic solver requires positive suspension travel.",
                        MessageType.Warning);
                if (configuration.Powertrain == null)
                    EditorGUILayout.HelpBox("Add a powertrain asset to enable M1 mode. " +
                                            "M0 direct torque is still available.",
                        MessageType.Info);
            }
        }

        private void CreatePowertrainAsset()
        {
            if (configuration == null) return;

            string path = EditorUtility.SaveFilePanelInProject(
                "Create M1 Simple Powertrain", "KAVRA_SimplePowertrain.asset",
                "asset", "Save motor torque curve and gearbox settings.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            AssetDatabase.CreateAsset(asset, path);
            Undo.RecordObject(configuration, "Connect M1 powertrain");
            configuration.Powertrain = asset;
            EditorUtility.SetDirty(configuration);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Debug.Log("KAVRA M1: powertrain assigned to " + configuration.name);
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
