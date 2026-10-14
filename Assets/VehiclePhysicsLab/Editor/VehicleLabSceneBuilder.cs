using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generates a host-only physics laboratory. Before replacing the current
/// scene it resolves a VALID, persistent VehicleDefinition asset. Never passes
/// a null or transient ScriptableObject into the vehicle scaffold builder.
/// </summary>
public static class VehicleLabSceneBuilder
{
    private const string StarterDefinitionPath = "Assets/KAVRA_PassengerCar4.asset";
    private const string FallbackDefinitionPath =
        "Assets/VehiclePhysicsLab/KAVRA_M0_LabCar.asset";

    [MenuItem("Tools/KAVRA/Create M0 Physics Lab Scene")]
    public static void CreateLab()
    {
        CreateLabInternal(false);
    }

    [MenuItem("Tools/KAVRA/Create M0 Calibration Run")]
    public static void CreateCalibrationLab()
    {
        CreateLabInternal(true);
    }

    private static void CreateLabInternal(bool scripted)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Resolve and validate the asset BEFORE the previous scene is closed.
        // In some projects the starter .asset may not deserialize correctly,
        // even though the file is present. Creating a template is safer than
        // continuing with a null VehicleDefinition.
        string assetPath;
        if (!TryFindOrCreateDefinition(out assetPath))
            return;

        // Double-check immediately before changing scenes, so a missing/broken
        // asset cannot clear the user's current scene.
        VehicleDefinition configuration = LoadValidDefinition(assetPath);
        if (configuration == null)
        {
            Debug.LogError("KAVRA M0: vehicle configuration at '" + assetPath +
                           "' is missing or invalid. Check its Inspector for Missing Script " +
                           "and Unity Console for import errors; lab scene was not created.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Re-load the persistent asset after the scene transition. This also
        // avoids relying on references to objects created before NewScene.
        configuration = LoadValidDefinition(assetPath);
        if (configuration == null)
        {
            Debug.LogError("KAVRA M0: lost vehicle asset after creating the scene: '" +
                           assetPath + "'. No vehicle was built; check missing scripts/import.");
            return;
        }

        // Four simple surface sections at the SAME height: physics changes
        // come from SurfaceProfile, not hidden differences in geometry.
        // The end caps give room to accelerate/stop outside the test patches.
        if (scripted)
            CreateCalibrationRunway();
        else
        {
            CreateSurfaceTrack();

            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "TestRamp_Approx_8deg";
            ramp.transform.position = new Vector3(-7f, 0.35f, 15f);
            ramp.transform.localScale = new Vector3(5f, 0.4f, 7f);
            ramp.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }

        GameObject vehicle = VehicleScaffoldBuilder.Create(configuration, true);
        // Starts on asphalt facing the successive grip zones along +Z.
        vehicle.transform.position += new Vector3(0f, 0f, -33f);
        VehicleLabKeyboardInput input = vehicle.AddComponent<VehicleLabKeyboardInput>();
        input.Simulation = vehicle.GetComponent<VehicleSimulationRoot>();
        input.Simulation.MeasureContactQueryCost = true; // Lab-only A/B benchmark.
        input.Simulation.SteeringMode = SteeringGeometryMode.ApproximateAckermann;
        input.AcceptDrivingKeys = !scripted;
        VehicleLabTelemetryRecorder recorder = vehicle.AddComponent<VehicleLabTelemetryRecorder>();
        recorder.Simulation = input.Simulation;
        if (scripted)
        {
            VehicleLabManeuverRunner runner = vehicle.AddComponent<VehicleLabManeuverRunner>();
            runner.Simulation = input.Simulation;
            runner.Maneuver = VehicleLabManeuver.StraightBrake;
            recorder.RecordSeconds = 28f;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 65f;
        VehicleLabChaseCamera follow = cameraObject.AddComponent<VehicleLabChaseCamera>();
        follow.Target = vehicle.transform;
        cameraObject.transform.position =
            vehicle.transform.position + new Vector3(0f, 5f, -10f);
        cameraObject.transform.LookAt(vehicle.transform.position);

        GameObject sunlight = new GameObject("Directional Light");
        Light light = sunlight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        sunlight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Selection.activeGameObject = vehicle;
        EditorSceneManager.MarkSceneDirty(scene);
        if (scripted)
            Debug.Log("KAVRA M0: created scripted calibration scene. Save as " +
                      "Assets/VehiclePhysicsLab/Scenes/M0_CalibrationLab.unity. " +
                      "Press Play; experiment defaults to StraightBrake. Change " +
                      "VehicleLabManeuverRunner.Maneuver in Inspector for SteeringStep/CoastDown. " +
                      "Measured values will be written to a CSV file.");
        else
            Debug.Log("KAVRA M0: created surface lab using '" + assetPath +
                      "'. Save as Assets/VehiclePhysicsLab/Scenes/M0_DynamicLab.unity. " +
                      "WASD/arrows: torque and steer. Space: brake, Shift: park, " +
                      "R: explicit manual recovery; surface zones in +Z direction.");
    }

    /// <summary>
    /// An easily repeated, visually distinct longitudinal surface test.
    /// SurfaceProfile values affect wheel friction/rolling only; geometry is
    /// completely flat, and no soil deformation solver is enabled.
    /// </summary>
    private static void CreateCalibrationRunway()
    {
        const string folder = "Assets/VehiclePhysicsLab/Surfaces";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/VehiclePhysicsLab", "Surfaces");

        SurfaceProfile asphalt = GetSurfaceProfile("Asphalt", 1f, 1f, 1f, 0f, 0f);
        Material material = GetSurfaceMaterial("Asphalt", new Color(0.23f, 0.25f, 0.27f));
        GameObject runway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        runway.name = "Ground_CalibrationAsphalt";
        runway.transform.position = new Vector3(0f, -0.5f, 150f);
        runway.transform.localScale = new Vector3(220f, 1f, 600f);
        SurfaceZone zone = runway.AddComponent<SurfaceZone>();
        zone.Profile = asphalt;
        if (material != null)
            runway.GetComponent<Renderer>().sharedMaterial = material;
        AssetDatabase.SaveAssets();
    }

    private static void CreateSurfaceTrack()
    {
        const string folder = "Assets/VehiclePhysicsLab/Surfaces";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/VehiclePhysicsLab", "Surfaces");

        SurfaceProfile asphalt = GetSurfaceProfile("Asphalt", 1f, 1f, 1f, 0f, 0f);
        SurfaceProfile gravel = GetSurfaceProfile("Gravel", 0.66f, 0.60f, 1.75f, 0.35f, 0f);
        SurfaceProfile mud = GetSurfaceProfile("Mud", 0.38f, 0.32f, 4.5f, 0.85f, 0.75f);
        SurfaceProfile ice = GetSurfaceProfile("Ice", 0.14f, 0.12f, 0.75f, 0f, 0.95f);

        Material matAsphalt = GetSurfaceMaterial("Asphalt", new Color(0.23f, 0.25f, 0.27f));
        Material matGravel = GetSurfaceMaterial("Gravel", new Color(0.52f, 0.45f, 0.32f));
        Material matMud = GetSurfaceMaterial("Mud", new Color(0.30f, 0.19f, 0.13f));
        Material matIce = GetSurfaceMaterial("Ice", new Color(0.52f, 0.73f, 0.88f));

        CreateGroundSection("Asphalt_Start", -60f, 40f, asphalt, matAsphalt);
        CreateGroundSection("Asphalt_Test", -30f, 20f, asphalt, matAsphalt);
        CreateGroundSection("Gravel_Test", -10f, 20f, gravel, matGravel);
        CreateGroundSection("Mud_Test", 10f, 20f, mud, matMud);
        CreateGroundSection("Ice_Test", 30f, 20f, ice, matIce);
        CreateGroundSection("Asphalt_End", 80f, 80f, asphalt, matAsphalt);
        AssetDatabase.SaveAssets();
    }

    private static SurfaceProfile GetSurfaceProfile(
        string name, float longitudinal, float lateral, float rolling,
        float softness, float wetness)
    {
        string target = "Assets/VehiclePhysicsLab/Surfaces/M0_" + name + ".asset";
        SurfaceProfile profile = AssetDatabase.LoadAssetAtPath<SurfaceProfile>(target);
        if (profile != null) return profile; // Never overwrite user-tuned assets.

        // A broken/occupied asset path is preserved. Make a unique file.
        target = AssetDatabase.GenerateUniqueAssetPath(target);
        profile = ScriptableObject.CreateInstance<SurfaceProfile>();
        profile.LongitudinalGripMultiplier = longitudinal;
        profile.LateralGripMultiplier = lateral;
        profile.RollingResistanceMultiplier = rolling;
        profile.Softness = softness;
        profile.Wetness = wetness;
        AssetDatabase.CreateAsset(profile, target);
        return profile;
    }

    private static Material GetSurfaceMaterial(string name, Color tint)
    {
        string target = "Assets/VehiclePhysicsLab/Surfaces/M0_" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(target);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null; // Physics works without a material.

        material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        target = AssetDatabase.GenerateUniqueAssetPath(target);
        AssetDatabase.CreateAsset(material, target);
        return material;
    }

    private static void CreateGroundSection(
        string name, float z, float length, SurfaceProfile profile, Material material)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = "Ground_" + name;
        piece.transform.position = new Vector3(0f, -0.5f, z);
        piece.transform.localScale = new Vector3(45f, 1f, length);
        SurfaceZone zone = piece.AddComponent<SurfaceZone>();
        zone.Profile = profile;
        if (material != null)
            piece.GetComponent<Renderer>().sharedMaterial = material;
    }

    /// <summary>
    /// Existing valid user assets are preferred and never overwritten.
    /// A corrupt .asset or missing script is skipped; an independent template
    /// is saved at a unique Assets path as the last fallback.
    /// </summary>
    private static bool TryFindOrCreateDefinition(out string assetPath)
    {
        assetPath = StarterDefinitionPath;
        if (LoadValidDefinition(assetPath) != null)
            return true;

        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            Debug.LogWarning("KAVRA M0: starter vehicle asset exists but is not a " +
                             "valid VehicleDefinition: '" + assetPath +
                             "'. Creating/using a fallback instead.");

        assetPath = FallbackDefinitionPath;
        if (LoadValidDefinition(assetPath) != null)
            return true;

        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            Debug.LogWarning("KAVRA M0: fallback asset is also invalid: '" +
                             assetPath + "'. A new asset will be created without overwriting it.");

        VehicleDefinition fresh = null;
        try
        {
            fresh = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
            if (fresh == null)
            {
                Debug.LogError("KAVRA M0: VehicleTemplateFactory could not create a car.");
                return false;
            }

            // If a broken .asset file occupies the fallback path, select a
            // unique one instead of overwriting potentially valuable data.
            assetPath = AssetDatabase.GenerateUniqueAssetPath(FallbackDefinitionPath);
            AssetDatabase.CreateAsset(fresh, assetPath);
            AssetDatabase.SaveAssets();

            VehicleDefinition created = LoadValidDefinition(assetPath);
            if (created == null)
            {
                Debug.LogError("KAVRA M0: new VehicleDefinition could not be reloaded: '" +
                               assetPath + "'. Inspect package import and script GUIDs.");
                return false;
            }

            Debug.Log("KAVRA M0: generated missing/invalid lab vehicle config at '" +
                      assetPath + "'.");
            return true;
        }
        catch (System.Exception error)
        {
            Debug.LogError("KAVRA M0: unable to create a usable VehicleDefinition: " + error);
            if (fresh != null && !AssetDatabase.Contains(fresh))
                Object.DestroyImmediate(fresh);
            return false;
        }
    }

    private static VehicleDefinition LoadValidDefinition(string path)
    {
        VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(path);
        if (definition == null)
            return null;

        foreach (VehicleValidationIssue issue in VehicleConfigurationValidator.Validate(definition))
        {
            if (issue.Severity == ValidationSeverity.Error)
            {
                Debug.LogWarning("KAVRA M0: definition '" + path + "' invalid: " + issue.Message);
                return null;
            }
        }

        // M0 dynamic solver needs non-zero travel, even if a future template
        // supports rigid or unsprung wheel configurations.
        foreach (WheelDefinition wheel in definition.Wheels)
        {
            if (wheel == null || wheel.SuspensionTravelMeters <= 0f)
            {
                Debug.LogWarning("KAVRA M0: definition '" + path +
                                 "' contains a wheel with no suspension travel.");
                return null;
            }
        }

        return definition;
    }
}
