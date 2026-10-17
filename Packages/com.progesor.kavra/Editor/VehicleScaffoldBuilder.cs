using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Creates visual-only previews or experimental dynamic M0 chassis.
    /// The cube is a temporary, physical chassis collider in dynamic mode.
    /// Neither mode builds a production-ready vehicle prefab yet.
    /// </summary>
    public static class VehicleScaffoldBuilder
    {
        public static GameObject Create(VehicleDefinition configuration,
            bool dynamicPhysics = false,
            VehicleDriveSource driveSource = VehicleDriveSource.LegacyDirectWheelTorque)
        {
            if (dynamicPhysics && driveSource == VehicleDriveSource.SimplePowertrain &&
                (configuration == null || configuration.Powertrain == null))
                throw new System.ArgumentException(
                    "M1 Basic Powertrain requires a VehicleDefinition.Powertrain asset.");

            if (configuration == null)
                throw new System.ArgumentNullException(nameof(configuration));
            foreach (VehicleValidationIssue issue in VehicleConfigurationValidator.Validate(configuration))
                if (issue.Severity == ValidationSeverity.Error)
                    throw new System.InvalidOperationException("KAVRA invalid vehicle: " + issue.Message);

            var root = new GameObject("KAVRA_" + configuration.VehicleName);
            Undo.RegisterCreatedObjectUndo(root, "Create KAVRA vehicle");
            var vehicleRoot = Undo.AddComponent<VehicleRoot>(root);
            vehicleRoot.Definition = configuration;
            var body = Undo.AddComponent<Rigidbody>(root);
            body.mass = configuration.ChassisMassKg;
            body.centerOfMass = configuration.CenterOfMassLocal;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.isKinematic = !dynamicPhysics;

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
            float initialDynamicHeight = float.NegativeInfinity;
            foreach (WheelDefinition wheel in configuration.Wheels)
            {
                minX = Mathf.Min(minX, wheel.MountLocalPosition.x - wheel.WidthMeters * 0.5f);
                maxX = Mathf.Max(maxX, wheel.MountLocalPosition.x + wheel.WidthMeters * 0.5f);
                minZ = Mathf.Min(minZ, wheel.MountLocalPosition.z - wheel.RadiusMeters);
                maxZ = Mathf.Max(maxZ, wheel.MountLocalPosition.z + wheel.RadiusMeters);
                initialDynamicHeight = Mathf.Max(initialDynamicHeight,
                    wheel.RadiusMeters + wheel.SuspensionTravelMeters * 0.5f
                    - wheel.MountLocalPosition.y);
            }

            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "__CHASSIS_TEST_COLLIDER";
            chassis.transform.SetParent(root.transform, false);
            chassis.transform.localPosition = new Vector3(
                (minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
            chassis.transform.localScale = new Vector3(
                Mathf.Max(0.8f, maxX - minX + 0.25f), 0.6f,
                Mathf.Max(1f, maxZ - minZ + 0.4f));

            var wheelContainer = new GameObject("WheelAnchors_M0");
            wheelContainer.transform.SetParent(root.transform, false);
            foreach (WheelDefinition wheel in configuration.Wheels)
            {
                var mount = new GameObject("Wheel_" + wheel.Id);
                mount.transform.SetParent(wheelContainer.transform, false);
                mount.transform.localPosition = wheel.MountLocalPosition;
                var anchor = mount.AddComponent<VehicleWheelAnchor>();
                anchor.WheelId = wheel.Id;
                anchor.AxleId = wheel.AxleId;
                anchor.RadiusMeters = wheel.RadiusMeters;
                anchor.WidthMeters = wheel.WidthMeters;
                anchor.SuspensionTravelMeters = wheel.SuspensionTravelMeters;

                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "__WHEEL_PREVIEW";
                visual.transform.SetParent(mount.transform, false);
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                visual.transform.localScale = new Vector3(
                    wheel.RadiusMeters * 2f, wheel.WidthMeters * 0.5f,
                    wheel.RadiusMeters * 2f);
                var collider = visual.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
                anchor.VisualTransform = visual.transform;
            }

            float initialHeight = dynamicPhysics
                ? initialDynamicHeight
                : configuration.Wheels[0].RadiusMeters
                  - configuration.Wheels[0].MountLocalPosition.y;
            root.transform.position = new Vector3(0f, initialHeight, 0f);

            if (dynamicPhysics)
            {
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var simulation = Undo.AddComponent<VehicleSimulationRoot>(root);
                simulation.DriveSource = driveSource;

                // Only new M1 scaffolds gain the transform rig by default.
                // Existing M0 scenes/prefabs retain their original single-
                // cylinder visual behavior for backward compatibility.
                if (driveSource == VehicleDriveSource.SimplePowertrain)
                    VehicleWheelVisualRigBuilder.Bind(root, true);
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            return root;
        }
    }
}
