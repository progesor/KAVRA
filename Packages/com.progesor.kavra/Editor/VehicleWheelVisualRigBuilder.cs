using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Non-destructive wheel pivot hierarchy construction. Existing assigned
    /// mesh is reparented under a new spin pivot while preserving its world
    /// transform. The original WheelAnchor remains the physics sensor.
    /// </summary>
    public static class VehicleWheelVisualRigBuilder
    {
        [MenuItem("Tools/KAVRA/Bind Selected Vehicle Wheel Visuals")]
        public static void BindSelectedVehicle()
        {
            GameObject selected = Selection.activeGameObject;
            VehicleRoot root = selected != null
                ? selected.GetComponentInParent<VehicleRoot>() : null;
            if (root == null)
            {
                EditorUtility.DisplayDialog("KAVRA",
                    "Select a vehicle root (or one of its children) first.", "OK");
                return;
            }
            int count = Bind(root.gameObject, false);
            Debug.Log("KAVRA: added " + count + " visual wheel rigs to " + root.name);
        }

        /// <summary>
        /// addDemonstrationShock creates simple visible cylinders; true for
        /// newly generated M1 primitive previews, false for imported meshes.
        /// </summary>
        public static int Bind(GameObject vehicle, bool addDemonstrationShock)
        {
            if (vehicle == null) return 0;
            VehicleWheelAnchor[] mounts =
                vehicle.GetComponentsInChildren<VehicleWheelAnchor>(true);
            int linked = 0;
            foreach (VehicleWheelAnchor mount in mounts)
            {
                if (mount == null || mount.GetComponent<VehicleWheelVisualRig>() != null)
                    continue;

                Transform mesh = mount.VisualTransform != null
                    ? mount.VisualTransform : mount.transform.Find("__WHEEL_PREVIEW");
                if (mesh == null) continue;

                GameObject travel = MakePivot("TravelPivot", mount.transform);
                GameObject steering = MakePivot("SteeringPivot", travel.transform);
                GameObject spin = MakePivot("SpinPivot", steering.transform);

                Undo.SetTransformParent(mesh, spin.transform,
                    "Bind wheel visual to KAVRA pivot rig");

                VehicleWheelVisualRig rig =
                    Undo.AddComponent<VehicleWheelVisualRig>(mount.gameObject);
                rig.TravelPivot = travel.transform;
                rig.SteeringPivot = steering.transform;
                rig.SpinPivot = spin.transform;
                rig.CaptureInitialPose();

                if (addDemonstrationShock)
                    AddDemonstrationShock(mount, travel.transform);

                EditorUtility.SetDirty(mount.gameObject);
                linked++;
            }
            return linked;
        }

        private static GameObject MakePivot(string name, Transform parent)
        {
            var pivot = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(pivot, "Create KAVRA wheel visual pivot");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localRotation = Quaternion.identity;
            pivot.transform.localScale = Vector3.one;
            return pivot;
        }

        private static void AddDemonstrationShock(
            VehicleWheelAnchor anchor, Transform travel)
        {
            float offset = anchor.transform.localPosition.x < 0f ? 0.12f : -0.12f;
            GameObject upper = MakePivot("ShockUpper", anchor.transform);
            upper.transform.localPosition = new Vector3(offset, 0.28f, 0.1f);
            GameObject lower = MakePivot("ShockLower", travel);
            lower.transform.localPosition = new Vector3(offset, 0.02f, 0.1f);

            GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = "__SHOCK_PREVIEW";
            Undo.RegisterCreatedObjectUndo(rod, "Create KAVRA shock visual");
            rod.transform.SetParent(anchor.transform, false);
            rod.transform.localPosition = Vector3.zero;
            rod.transform.localRotation = Quaternion.identity;
            rod.transform.localScale = new Vector3(0.025f, 0.5f, 0.025f);
            Collider collider = rod.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            VehicleSuspensionVisualLink link =
                Undo.AddComponent<VehicleSuspensionVisualLink>(anchor.gameObject);
            link.UpperAttachment = upper.transform;
            link.LowerAttachment = lower.transform;
            link.LinkMesh = rod.transform;
            link.ReferenceLengthMeters = 1f;
            link.CaptureInitialScale();
            link.UpdateLink();
        }
    }
}
