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

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "TestGround_Flat";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(120f, 1f, 120f);

        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "TestRamp_Approx_8deg";
        ramp.transform.position = new Vector3(-7f, 0.35f, 15f);
        ramp.transform.localScale = new Vector3(5f, 0.4f, 7f);
        ramp.transform.rotation = Quaternion.Euler(8f, 0f, 0f);

        GameObject vehicle = VehicleScaffoldBuilder.Create(configuration, true);
        VehicleLabKeyboardInput input = vehicle.AddComponent<VehicleLabKeyboardInput>();
        input.Simulation = vehicle.GetComponent<VehicleSimulationRoot>();

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
        Debug.Log("KAVRA M0: created dynamic lab using '" + assetPath + "'. Save scene to " +
                  "Assets/VehiclePhysicsLab/Scenes/M0_DynamicLab.unity, then press Play. " +
                  "Keys: WASD/arrows, Space brake, Left Shift parking brake.");
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
