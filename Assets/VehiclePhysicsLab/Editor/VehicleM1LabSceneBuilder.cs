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
    [MenuItem("Tools/KAVRA/Create M1 Powertrain Lab Scene")]
    public static void Create() { CreateForTemplate(VehicleTemplate.PassengerCar4); }

    [MenuItem("Tools/KAVRA/Create M1 Three-Wheel Lab Scene")]
    public static void Create3W() { CreateForTemplate(VehicleTemplate.ThreeWheelDelta); }

    [MenuItem("Tools/KAVRA/Create M1 6x6 Truck Lab Scene")]
    public static void Create6x6() { CreateForTemplate(VehicleTemplate.Truck6x6); }

    [MenuItem("Tools/KAVRA/Create M1 8x8 Truck Lab Scene")]
    public static void Create8x8() { CreateForTemplate(VehicleTemplate.Truck8x8); }

    [MenuItem("Tools/KAVRA/Create M1 20W Stress Lab Scene")]
    public static void Create20W() {
        CreateForTemplate(VehicleTemplate.SingleChassis20WheelStressTest);
    }

    private static void CreateForTemplate(VehicleTemplate kind)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string definitionPath;
        if (!TryCreateAssets(kind, out definitionPath))
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
        ground.transform.localScale = new Vector3(120f, 1f, 400f);

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
        string sceneName = kind == VehicleTemplate.PassengerCar4
            ? "M1_PowertrainLab.unity" : "M1_" + kind + "Lab.unity";
        Debug.Log("KAVRA M1: " + kind + " lab ready. Save to " +
                  "Assets/VehiclePhysicsLab/Scenes/" + sceneName +
                  ". W/S forward/reverse (reverse at low speed), Q/E gears, A/D " +
                  "steer, Space brake, Left Shift park, R manual recovery. " +
                  "Inspect per-axle torque allocation and EngineRpm.");
    }

    private static bool TryCreateAssets(VehicleTemplate kind, out string definitionPath)
    {
        string suffix = kind.ToString();
        definitionPath = "Assets/VehiclePhysicsLab/M1_" + suffix + ".asset";
        string powertrainPath = kind == VehicleTemplate.PassengerCar4
            ? "Assets/VehiclePhysicsLab/M1_SimplePowertrain.asset"
            : "Assets/VehiclePhysicsLab/M1_Powertrain_" + suffix + ".asset";

        SimplePowertrainDefinition powertrain =
            AssetDatabase.LoadAssetAtPath<SimplePowertrainDefinition>(powertrainPath);

        if (powertrain == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(powertrainPath) != null)
            {
                Debug.LogError("KAVRA M1: another asset occupies " + powertrainPath +
                               ". Existing files are not overwritten.");
                return false;
            }
            powertrain = ScriptableObject.CreateInstance<SimplePowertrainDefinition>();
            // Heavier presets get a demonstration diesel-like torque curve.
            // This is an example, not a measured commercial truck engine.
            if (kind == VehicleTemplate.Truck6x6 ||
                kind == VehicleTemplate.Truck8x8 ||
                kind == VehicleTemplate.SingleChassis20WheelStressTest)
            {
                powertrain.IdleRpm = 650f;
                powertrain.MaxRpm = 3100f;
                powertrain.EngineRpmResponsePerSecond = 950f;
                powertrain.LaunchFreeRevFraction = 0.22f;
                powertrain.TorqueNmByRpm = new AnimationCurve(
                    new Keyframe(650f, 660f),
                    new Keyframe(1000f, 950f),
                    new Keyframe(1900f, 1100f),
                    new Keyframe(3000f, 790f));
                powertrain.ForwardGearRatios =
                    new[] { 8.3f, 4.6f, 2.7f, 1.7f, 1f };
                powertrain.ReverseGearRatio = 8f;
                powertrain.FinalDriveRatio = 4.5f;
                powertrain.Efficiency = 0.83f;
            }
            AssetDatabase.CreateAsset(powertrain, powertrainPath);
        }

        VehicleDefinition definition =
            AssetDatabase.LoadAssetAtPath<VehicleDefinition>(definitionPath);
        if (definition == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(definitionPath) != null)
            {
                Debug.LogError("KAVRA M1: incompatible asset at " + definitionPath);
                return false;
            }

            definition = VehicleTemplateFactory.Create(kind);
            definition.Powertrain = powertrain;
            AssetDatabase.CreateAsset(definition, definitionPath);
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
