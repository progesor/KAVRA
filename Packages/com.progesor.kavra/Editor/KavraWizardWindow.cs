using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    public sealed class KavraWizardWindow : EditorWindow
    {
        private VehicleTemplate template = VehicleTemplate.PassengerCar4;
        private VehicleDefinition configuration;
        private Vector2 scroll;
        private Vector2 wholeWindowScroll;

        [MenuItem("Tools/KAVRA/Vehicle Setup Wizard")]
        public static void Open()
        {
            GetWindow<KavraWizardWindow>("KAVRA Wizard").minSize = new Vector2(450f, 410f);
        }

        private void OnGUI()
        {
            // The M1 asset and prefab workflow is taller than the old M0
            // window. Keep all controls reachable without resizing Unity.
            wholeWindowScroll = EditorGUILayout.BeginScrollView(wholeWindowScroll);
            DrawWizardContents();
            EditorGUILayout.EndScrollView();
        }

        private void DrawWizardContents()
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

            if (configuration.Powertrain != null)
                DrawAxleDriveSettings();

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
                if (GUILayout.Button("4A. Build non-drivable preview scaffold", GUILayout.Height(29f)))
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

        private void DrawAxleDriveSettings()
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField("4. Driven axle distribution", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Each powered wheel is selected by Wheel.CanDrive; axle torque weights " +
                "distribute engine output between axle groups. EqualTorque is the " +
                "default. SpeedBiasedApproximation prefers slower-spinning wheels " +
                "but does NOT physically lock a differential.", MessageType.None);

            if (configuration.Axles == null || configuration.Wheels == null) return;
            foreach (AxleDefinition axle in configuration.Axles)
            {
                if (axle == null) continue;
                int powered = 0;
                int wheelCount = 0;
                for (int i = 0; i < configuration.Wheels.Count; i++)
                {
                    WheelDefinition wheel = configuration.Wheels[i];
                    if (wheel == null || wheel.AxleId != axle.Id) continue;
                    wheelCount++;
                    if (wheel.CanDrive) powered++;
                }
                if (wheelCount == 0) continue;

                EditorGUILayout.LabelField(axle.Id + " — " + powered + "/" + wheelCount +
                    " driven wheels", EditorStyles.miniBoldLabel);
                bool driveEnabled = powered > 0;
                bool newDriveEnabled = EditorGUILayout.Toggle("Drive this axle", driveEnabled);
                if (newDriveEnabled != driveEnabled)
                {
                    Undo.RecordObject(configuration, "Configure KAVRA driven wheels");
                    for (int i = 0; i < configuration.Wheels.Count; i++)
                    {
                        WheelDefinition wheel = configuration.Wheels[i];
                        if (wheel != null && wheel.AxleId == axle.Id)
                            wheel.CanDrive = newDriveEnabled;
                    }
                    axle.IsDriven = newDriveEnabled;
                    EditorUtility.SetDirty(configuration);
                    powered = newDriveEnabled ? wheelCount : 0;
                }
                if (powered == 0) continue;
                float currentWeight = axle.DriveTorqueWeight > 0f
                    ? axle.DriveTorqueWeight : 1f;
                float weight = EditorGUILayout.FloatField("Axle torque weight", currentWeight);
                AxleDifferentialMode mode = (AxleDifferentialMode)EditorGUILayout.EnumPopup(
                    "Differential approximation", axle.DifferentialMode);
                float ratio = axle.MaximumSpeedBiasRatio > 0f
                    ? axle.MaximumSpeedBiasRatio : 2.5f;
                float gain = axle.SpeedBiasGainPerRadPerSec > 0f
                    ? axle.SpeedBiasGainPerRadPerSec : 0.12f;
                if (mode == AxleDifferentialMode.SpeedBiasedApproximation)
                {
                    ratio = EditorGUILayout.FloatField("Maximum bias ratio", ratio);
                    gain = EditorGUILayout.FloatField("Speed bias gain", gain);
                }

                weight = Mathf.Max(0.01f, weight);
                ratio = Mathf.Max(1f, ratio);
                gain = Mathf.Max(0f, gain);
                if (!Mathf.Approximately(axle.DriveTorqueWeight, weight) ||
                    axle.DifferentialMode != mode ||
                    !Mathf.Approximately(axle.MaximumSpeedBiasRatio, ratio) ||
                    !Mathf.Approximately(axle.SpeedBiasGainPerRadPerSec, gain))
                {
                    Undo.RecordObject(configuration, "Update KAVRA axle drive distribution");
                    axle.DriveTorqueWeight = weight;
                    axle.DifferentialMode = mode;
                    axle.MaximumSpeedBiasRatio = ratio;
                    axle.SpeedBiasGainPerRadPerSec = gain;
                    EditorUtility.SetDirty(configuration);
                }
            }
        }

        private void CreatePowertrainAsset()
        {
            if (configuration == null) return;

            string path = EditorUtility.SaveFilePanelInProject(
                "Create M1 Simple Powertrain", "KAVRA_SimplePowertrain.asset",
                "asset", "Save motor torque curve and gearbox settings.");
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA",
                    "An asset already exists at that path. Choose another name.", "OK");
                return;
            }

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
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA",
                    "A vehicle definition already exists there. Choose a unique name.", "OK");
                return;
            }
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
