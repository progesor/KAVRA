using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Host-project-only M1 demonstration generator. Creates a separate
/// VehicleDefinition/Powertrain asset pair, preserving the M0 reference
/// configurations and old Unity test scenes.
/// </summary>
public static class VehicleM1LabSceneBuilder
{
    private const string DefinitionPath =
        "Assets/VehiclePhysicsLab/M1_PassengerCar4.asset";
    private const string PowertrainPath =
        "Assets/VehiclePhysicsLab/M1_SimplePowertrain.asset";

    [MenuItem("Tools/KAVRA/Create M1 Powertrain Lab Scene")]
    public static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!TryCreateAssets(out string definitionPath))
            return;

        VehicleDefinition definition =
            AssetDatabase.LoadAssetAtPath<VehicleDefinition>(definitionPath);
        if (definition == null || definition.Powertrain == null)
        {
            Debug.LogError("KAVRA M1: asset not imported. Lab scene was not changed.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(definitionPath);
        if (definition == null || definition.Powertrain == null)
        {
            Debug.LogError("KAVRA M1: configuration could not reload after new scene.");
            return;
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "M1_Ground_Asphalt";
        ground.transform.position = new Vector3(0f, -0.5f, 80f);
        ground.transform.localScale = new Vector3(100f, 1f, 400f);

        GameObject car = VehicleScaffoldBuilder.Create(
            definition, true, VehicleDriveSource.SimplePowertrain);
        car.transform.position += new Vector3(0f, 0f, -40f);
        VehicleLabKeyboardInput input = car.AddComponent<VehicleLabKeyboardInput>();
        input.Simulation = car.GetComponent<VehicleSimulationRoot>();
        input.AcceptDrivingKeys = true;

        VehicleLabTelemetryRecorder recorder = car.AddComponent<VehicleLabTelemetryRecorder>();
        recorder.Simulation = input.Simulation;
        recorder.RecordSeconds = 90f;

        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.fieldOfView = 64f;
        VehicleLabChaseCamera chase = cameraObj.AddComponent<VehicleLabChaseCamera>();
        chase.Target = car.transform;
        cameraObj.transform.position =
            car.transform.position + new Vector3(0f, 5f, -10f);
        cameraObj.transform.LookAt(car.transform.position);

        GameObject lightObj = new GameObject("Directional Light");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Selection.activeGameObject = car;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("KAVRA M1: engine/gearbox lab ready. Save to " +
                  "Assets/VehiclePhysicsLab/Scenes/M1_PowertrainLab.unity. " +
                  "W/S forward/reverse (shift only when slow), Q/E gears, A/D " +
                  "steer, Space brake, Left Shift park, R manual recovery. " +
                  "Inspect EngineRpm, SelectedGear and wheel torque telemetry.");
    }

    private static bool TryCreateAssets(out string definitionPath)
    {
        definitionPath = DefinitionPath;
        SimplePowertrainDefinition powertrain =
            AssetDatabase.LoadAssetAtPath<SimplePowertrainDefinition>(PowertrainPath);

        if (powertrain == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(PowertrainPath) != null)
            {
                Debug.LogError("KAVRA M1: another asset occupies " + PowertrainPath +
                               ". Existing files are not overwritten.");
                return false;
            }
            powertrain = ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            AssetDatabase.CreateAsset(powertrain, PowertrainPath);
        }

        VehicleDefinition definition =
            AssetDatabase.LoadAssetAtPath<VehicleDefinition>(DefinitionPath);
        if (definition == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(DefinitionPath) != null)
            {
                Debug.LogError("KAVRA M1: incompatible asset at " + DefinitionPath);
                return false;
            }

            definition = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            definition.Powertrain = powertrain;
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }
        else if (definition.Powertrain == null)
        {
            Undo.RecordObject(definition, "Assign M1 powertrain");
            definition.Powertrain = powertrain;
            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
        foreach (VehicleValidationIssue issue in VehicleConfigurationValidator.Validate(definition))
        {
            if (issue.Severity != ValidationSeverity.Error) continue;
            Debug.LogError("KAVRA M1: invalid lab definition: " + issue.Message);
            return false;
        }

        return true;
    }
}
