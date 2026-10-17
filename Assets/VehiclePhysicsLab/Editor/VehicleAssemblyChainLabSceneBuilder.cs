using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a 6x6 powered lead plus TWO separate four-wheel passive trailers.
/// Three Rigidbody objects, fourteen individual tire/suspension instances,
/// two actual ConfigurableJoints and no third-party vehicle meshes.
/// </summary>
public static class VehicleAssemblyChainLabSceneBuilder
{
    private const string Folder = "Assets/VehiclePhysicsLab/Assemblies";
    private const string PowertrainPath = Folder + "/M15_TractorPowertrain.asset";
    private const string TractorPath = Folder + "/M15_Truck6x6.asset";
    private const string TrailerAPath = Folder + "/M15_TrailerA4W.asset";
    private const string TrailerBPath = Folder + "/M15_TrailerB4W.asset";
    private const string Link0Path = Folder + "/M15_Link0_TractorToA.asset";
    private const string Link1Path = Folder + "/M15_Link1_AToB.asset";
    private const string ChainPath = Folder + "/M15_MultiTrailerChain.asset";

    [MenuItem("Tools/KAVRA/Create M1.5 Two-Trailer Lab Scene")]
    public static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        VehicleAssemblyChainDefinition chain = TryCreateAssets();
        if (chain == null) return;

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);
        chain = AssetDatabase.LoadAssetAtPath<VehicleAssemblyChainDefinition>(
            ChainPath);
        if (VehicleAssemblyChainValidator.HasErrors(chain))
        {
            Debug.LogError("KAVRA M1.5: Chain asset invalid after scene switch.");
            return;
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "M15_Ground_600m";
        ground.transform.position = new Vector3(0f, -0.5f, 180f);
        ground.transform.localScale = new Vector3(180f, 1f, 600f);

        GameObject rig;
        try
        {
            rig = VehicleAssemblyChainBuilder.Build(chain);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("KAVRA M1.5: chain build failed: " + ex);
            return;
        }

        rig.transform.position += new Vector3(0f, 0f, -30f);
        Physics.SyncTransforms();
        VehicleAssemblyChainRoot convoy = rig.GetComponent<VehicleAssemblyChainRoot>();
        // Lab-only visual readability: generated 3m checker tiles,
        // distinct chassis paint, bright selected hitch guides.
        VehicleLabArtUtility.DecorateChain(convoy, ground);

        GameObject lead = convoy.Vehicles[0].gameObject;
        VehicleSimulationRoot sim = lead.GetComponent<VehicleSimulationRoot>();
        VehicleLabKeyboardInput keyboard = lead.AddComponent<VehicleLabKeyboardInput>();
        keyboard.Simulation = sim;
        keyboard.AcceptDrivingKeys = true;

        VehicleLabTelemetryRecorder log = lead.AddComponent<VehicleLabTelemetryRecorder>();
        log.Simulation = sim;
        log.RecordSeconds = 90f;

        VehicleAssemblyChainLabHud hud = rig.AddComponent<VehicleAssemblyChainLabHud>();
        hud.Chain = convoy;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 70f;
        VehicleLabChaseCamera chase =
            cameraObject.AddComponent<VehicleLabChaseCamera>();
        chase.Target = lead.transform;
        chase.LocalOffset = new Vector3(0f, 9f, -28f);
        chase.SpeedReactiveFov = true;
        chase.MaximumAdditionalFovDegrees = 7f;
        cameraObject.transform.position =
            lead.transform.position + new Vector3(0f, 9f, -28f);
        cameraObject.transform.LookAt(lead.transform.position);

        var lightObject = new GameObject("Directional Light");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Selection.activeGameObject = rig;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("KAVRA M1.5: 6x6 tractor + two 4W trailers = 3 Rigidbodies, " +
                  "14 wheels, 2 breakable hitches. Save as " +
                  "Assets/VehiclePhysicsLab/Scenes/M15_TwoTrailerChain.unity. " +
                  "J/L selects hitch, H connects across a more forgiving 0.5m radius, " +
                  "K applies PHYSICAL impulse to selected hitch, R manual connected-" +
                  "prefix recovery.");
    }

    private static VehicleAssemblyChainDefinition TryCreateAssets()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/VehiclePhysicsLab", "Assemblies");

        SimplePowertrainDefinition engine =
            AssetDatabase.LoadAssetAtPath<SimplePowertrainDefinition>(PowertrainPath);
        if (engine == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(PowertrainPath) != null)
                return Fail(PowertrainPath);
            engine = VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain();
            AssetDatabase.CreateAsset(engine, PowertrainPath);
        }

        VehicleDefinition tow =
            AssetDatabase.LoadAssetAtPath<VehicleDefinition>(TractorPath);
        if (tow == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(TractorPath) != null)
                return Fail(TractorPath);
            tow = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            tow.VehicleName = "M1.5 6x6 Multi-Trailer Tractor";
            tow.Powertrain = engine;
            AssetDatabase.CreateAsset(tow, TractorPath);
        }
        else if (tow.Powertrain == null)
        {
            Undo.RecordObject(tow, "Connect M1.5 powertrain");
            tow.Powertrain = engine;
            EditorUtility.SetDirty(tow);
        }

        VehicleDefinition a = GetOrCreateTrailer(TrailerAPath, "Trailer A");
        VehicleDefinition b = GetOrCreateTrailer(TrailerBPath, "Trailer B");
        if (a == null || b == null) return null;

        VehicleAssemblyDefinition link0 = GetOrCreateLink(
            Link0Path, tow, a, new Vector3(0f, -0.1f, -3.0f),
            new Vector3(0f, -0.1f, 2.2f));
        VehicleAssemblyDefinition link1 = GetOrCreateLink(
            Link1Path, a, b, new Vector3(0f, -0.1f, -3.3f),
            new Vector3(0f, -0.1f, 2.2f));
        if (link0 == null || link1 == null) return null;

        VehicleAssemblyChainDefinition chain =
            AssetDatabase.LoadAssetAtPath<VehicleAssemblyChainDefinition>(
                ChainPath);
        if (chain == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(ChainPath) != null)
                return Fail(ChainPath);
            chain = ScriptableObject.CreateInstance<VehicleAssemblyChainDefinition>();
            chain.Links.Add(link0);
            chain.Links.Add(link1);
            AssetDatabase.CreateAsset(chain, ChainPath);
        }

        AssetDatabase.SaveAssets();
        foreach (VehicleValidationIssue issue in
                 VehicleAssemblyChainValidator.Validate(chain))
            if (issue.Severity == ValidationSeverity.Error)
            {
                Debug.LogError("KAVRA M1.5: " + issue.Message);
                return null;
            }
        return chain;
    }

    private static VehicleDefinition GetOrCreateTrailer(string path, string label)
    {
        VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
            path);
        if (definition != null) return definition;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            Fail(path);
            return null;
        }
        definition = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
        definition.VehicleName = "M1.5 " + label + " 4W";
        AssetDatabase.CreateAsset(definition, path);
        return definition;
    }

    private static VehicleAssemblyDefinition GetOrCreateLink(
        string path, VehicleDefinition tractor, VehicleDefinition trailer,
        Vector3 tractorHitch, Vector3 trailerHitch)
    {
        VehicleAssemblyDefinition link =
            AssetDatabase.LoadAssetAtPath<VehicleAssemblyDefinition>(path);
        if (link != null) return link;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            Fail(path);
            return null;
        }
        link = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
        link.Tractor = tractor;
        link.Trailer = trailer;
        link.TractorHitchLocal = tractorHitch;
        link.TrailerHitchLocal = trailerHitch;
        AssetDatabase.CreateAsset(link, path);
        return link;
    }

    private static VehicleAssemblyChainDefinition Fail(string path)
    {
        Debug.LogError("KAVRA M1.5: incompatible asset at " + path +
                       ". Existing user assets are never overwritten.");
        return null;
    }
}
