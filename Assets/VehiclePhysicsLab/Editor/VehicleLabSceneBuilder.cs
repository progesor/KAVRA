using Progesor.Kavra;
using Progesor.Kavra.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Host-only laboratory scene generator; creates genuine Unity scene objects
/// in the Editor. Intentionally leaves the new scene unsaved for review.
/// </summary>
public static class VehicleLabSceneBuilder
{
    [MenuItem("Tools/KAVRA/Create M0 Physics Lab Scene")]
    public static void CreateLab()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
            "Assets/KAVRA_PassengerCar4.asset");
        if (definition == null)
        {
            const string fallback = "Assets/VehiclePhysicsLab/KAVRA_M0_LabCar.asset";
            definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(fallback);
            if (definition == null)
            {
                definition = VehicleTemplateFactory.Create(VehicleTemplate.PassengerCar4);
                AssetDatabase.CreateAsset(definition, fallback);
                AssetDatabase.SaveAssets();
            }
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "TestGround_Flat";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(120f, 1f, 120f);

        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "TestRamp_Approx_8deg";
        ramp.transform.position = new Vector3(-7f, 0.35f, 15f);
        ramp.transform.localScale = new Vector3(5f, 0.4f, 7f);
        ramp.transform.rotation = Quaternion.Euler(8f, 0f, 0f);

        GameObject vehicle = VehicleScaffoldBuilder.Create(definition, true);
        VehicleLabKeyboardInput input = vehicle.AddComponent<VehicleLabKeyboardInput>();
        input.Simulation = vehicle.GetComponent<VehicleSimulationRoot>();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 65f;
        VehicleLabChaseCamera follow = cameraObject.AddComponent<VehicleLabChaseCamera>();
        follow.Target = vehicle.transform;
        cameraObject.transform.position = vehicle.transform.position + new Vector3(0f, 5f, -10f);
        cameraObject.transform.LookAt(vehicle.transform.position);

        GameObject sunlight = new GameObject("Directional Light");
        Light light = sunlight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        sunlight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Selection.activeGameObject = vehicle;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("KAVRA M0: dynamic lab created. Save scene to " +
                  "Assets/VehiclePhysicsLab/Scenes/M0_DynamicLab.unity, then press Play. " +
                  "Keys: WASD / arrows, Space brake, Left Shift parking.");
    }
}
