using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Optional host-project M1.3 two-body PhysicsLab (not part of the runtime
/// UPM package). Creates separate reusable vehicle/assembly .asset files.
/// </summary>
public static class VehicleAssemblyLabSceneBuilder
{
    private const string Folder = "Assets/VehiclePhysicsLab/Assemblies";
    private const string EnginePath = Folder + "/M13_TractorPowertrain.asset";
    private const string TractorPath = Folder + "/M13_Truck6x6.asset";
    private const string TrailerPath = Folder + "/M13_Trailer4W.asset";
    private const string AssemblyPath = Folder + "/M13_TractorTrailer.asset";

    [MenuItem("Tools/KAVRA/Create M1.3 Tractor Trailer Lab Scene")]
    public static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        VehicleAssemblyDefinition definition;
        if (!TryCreateAssets(out definition)) return;

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);
        definition = AssetDatabase.LoadAssetAtPath<VehicleAssemblyDefinition>(AssemblyPath);
        if (VehicleAssemblyValidator.HasErrors(definition))
        {
            Debug.LogError("KAVRA M1.3: assembly asset failed to reload.");
            return;
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "M13_Ground_Level";
        ground.transform.position = new Vector3(0f, -0.5f, 90f);
        ground.transform.localScale = new Vector3(160f, 1f, 480f);

        GameObject rig;
        try { rig = VehicleAssemblyBuilder.Build(definition); }
        catch (System.Exception e)
        {
            Debug.LogError("KAVRA M1.3: unable to build tractor/trailer: " + e);
            return;
        }

        rig.transform.position = new Vector3(0f, 0f, -42f);
        Physics.SyncTransforms();
        VehicleAssemblyRoot assembly = rig.GetComponent<VehicleAssemblyRoot>();
        VehicleLabArtUtility.DecorateTwoBody(assembly, ground);
        GameObject tractor = assembly.Tractor.gameObject;
        VehicleSimulationRoot drive = tractor.GetComponent<VehicleSimulationRoot>();

        VehicleLabKeyboardInput input = tractor.AddComponent<VehicleLabKeyboardInput>();
        input.Simulation = drive;
        input.AcceptDrivingKeys = true;

        VehicleLabTelemetryRecorder recorder =
            tractor.AddComponent<VehicleLabTelemetryRecorder>();
        recorder.Simulation = drive;
        recorder.RecordSeconds = 90f;

        VehicleAssemblyLabHud display = rig.AddComponent<VehicleAssemblyLabHud>();
        display.Assembly = assembly;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 64f;
        VehicleLabChaseCamera cameraFollow =
            cameraObject.AddComponent<VehicleLabChaseCamera>();
        cameraFollow.Target = tractor.transform;
        cameraFollow.LocalOffset = new Vector3(0f, 7f, -17f);
        cameraFollow.SpeedReactiveFov = true;
        cameraFollow.MaximumAdditionalFovDegrees = 6f;
        cameraObject.transform.position = tractor.transform.position +
                                          new Vector3(0f, 7f, -17f);
        cameraObject.transform.LookAt(tractor.transform.position);

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Selection.activeGameObject = rig;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("KAVRA M1.3: 6x6 tractor + 4W passive trailer = 2 Rigidbody / 10 wheels. " +
                  "Save scene as Assets/VehiclePhysicsLab/Scenes/M13_TractorTrailer.unity. " +
                  "W/S drive/reverse, A/D steer, Space brake, Q/E shift, T auto/manual, " +
                  "R manually recover BOTH bodies (only when near stopped).");
    }

    private static bool TryCreateAssets(out VehicleAssemblyDefinition assembly)
    {
        assembly = null;
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/VehiclePhysicsLab", "Assemblies");

        SimplePowertrainDefinition powertrain =
            LoadOrCreatePowertrain();
        if (powertrain == null) return false;

        VehicleDefinition tow = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(TractorPath);
        if (tow == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(TractorPath) != null) return Fail(TractorPath);
            tow = VehicleTemplateFactory.Create(VehicleTemplate.Truck6x6);
            tow.VehicleName = "M1.3 Six-Wheel Tractor";
            tow.Powertrain = powertrain;
            AssetDatabase.CreateAsset(tow, TractorPath);
        }
        else if (tow.Powertrain == null)
        {
            Undo.RecordObject(tow, "Connect M1.3 truck powertrain");
            tow.Powertrain = powertrain;
            EditorUtility.SetDirty(tow);
        }

        VehicleDefinition trailer = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(TrailerPath);
        if (trailer == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(TrailerPath) != null) return Fail(TrailerPath);
            trailer = VehicleAssemblyTemplateFactory.CreateFourWheelTrailer();
            AssetDatabase.CreateAsset(trailer, TrailerPath);
        }

        assembly = AssetDatabase.LoadAssetAtPath<VehicleAssemblyDefinition>(AssemblyPath);
        if (assembly == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(AssemblyPath) != null) return Fail(AssemblyPath);
            assembly = ScriptableObject.CreateInstance<VehicleAssemblyDefinition>();
            assembly.Tractor = tow;
            assembly.Trailer = trailer;
            AssetDatabase.CreateAsset(assembly, AssemblyPath);
        }

        AssetDatabase.SaveAssets();
        foreach (VehicleValidationIssue issue in VehicleAssemblyValidator.Validate(assembly))
        {
            if (issue.Severity != ValidationSeverity.Error) continue;
            Debug.LogError("KAVRA M1.3: invalid configuration: " + issue.Message);
            return false;
        }
        return true;
    }

    private static SimplePowertrainDefinition LoadOrCreatePowertrain()
    {
        SimplePowertrainDefinition existing =
            AssetDatabase.LoadAssetAtPath<SimplePowertrainDefinition>(EnginePath);
        if (existing != null) return existing;
        if (AssetDatabase.LoadMainAssetAtPath(EnginePath) != null)
        {
            Fail(EnginePath);
            return null;
        }
        var powertrain = VehicleAssemblyTemplateFactory.CreateTruckSamplePowertrain();
        AssetDatabase.CreateAsset(powertrain, EnginePath);
        return powertrain;
    }

    private static bool Fail(string path)
    {
        Debug.LogError("KAVRA M1.3: an incompatible asset occupies '" + path +
                       "'. Existing assets will not be overwritten.");
        return false;
    }
}
