using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Editor-only preview/scaffold. Physics intentionally disabled (M0).
    /// Wheel visuals have no colliders and there is no tire force solver.
    /// </summary>
    internal static class VehicleScaffoldBuilder
    {
        public static GameObject Create(VehicleDefinition configuration)
        {
            var root = new GameObject("KAVRA_" + configuration.VehicleName);
            Undo.RegisterCreatedObjectUndo(root, "Create KAVRA vehicle scaffold");
            var vehicleRoot = Undo.AddComponent<VehicleRoot>(root);
            vehicleRoot.Definition = configuration;
            var body = Undo.AddComponent<Rigidbody>(root);
            body.mass = configuration.ChassisMassKg;
            body.centerOfMass = configuration.CenterOfMassLocal;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.isKinematic = true; // Prevent falling: no physical wheels in M0.

            // Preview body: not a production collider or final mass distribution.
            var minX = 100000f;
            var maxX = -100000f;
            var minZ = 100000f;
            var maxZ = -100000f;
            foreach (var wheel in configuration.Wheels)
            {
                minX = Mathf.Min(minX, wheel.MountLocalPosition.x - wheel.WidthMeters * 0.5f);
                maxX = Mathf.Max(maxX, wheel.MountLocalPosition.x + wheel.WidthMeters * 0.5f);
                minZ = Mathf.Min(minZ, wheel.MountLocalPosition.z - wheel.RadiusMeters);
                maxZ = Mathf.Max(maxZ, wheel.MountLocalPosition.z + wheel.RadiusMeters);
            }

            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "__CHASSIS_PREVIEW";
            chassis.transform.SetParent(root.transform, false);
            chassis.transform.localPosition = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
            chassis.transform.localScale = new Vector3(Mathf.Max(0.8f, maxX - minX + 0.25f),
                0.6f, Mathf.Max(1f, maxZ - minZ + 0.4f));

            var wheelContainer = new GameObject("WheelAnchors_M0_Preview");
            wheelContainer.transform.SetParent(root.transform, false);
            foreach (var wheel in configuration.Wheels)
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
                    wheel.RadiusMeters * 2f, wheel.WidthMeters * 0.5f, wheel.RadiusMeters * 2f);
                var primitiveCollider = visual.GetComponent<Collider>();
                if (primitiveCollider != null) Object.DestroyImmediate(primitiveCollider);
            }

            if (configuration.Wheels.Count > 0)
            {
                var first = configuration.Wheels[0];
                root.transform.position = new Vector3(0f,
                    first.RadiusMeters - first.MountLocalPosition.y, 0f);
            }
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            return root;
        }
    }
}
