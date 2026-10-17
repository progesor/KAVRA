using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Build a genuinely two-Rigidbody articulated assembly (not a fake
    /// 10/18 wheel single body). Runtime coupling logic stays in UPM Runtime.
    /// </summary>
    public static class VehicleAssemblyBuilder
    {
        public static GameObject Build(VehicleAssemblyDefinition definition)
        {
            foreach (VehicleValidationIssue issue in
                     VehicleAssemblyValidator.Validate(definition))
                if (issue.Severity == ValidationSeverity.Error)
                    throw new System.InvalidOperationException("KAVRA assembly: " + issue.Message);

            var assembly = new GameObject("KAVRA_Assembly_TractorTrailer");
            Undo.RegisterCreatedObjectUndo(assembly, "Create KAVRA VehicleAssembly");
            try
            {
                VehicleAssemblyRoot assemblyRoot =
                    Undo.AddComponent<VehicleAssemblyRoot>(assembly);
                assemblyRoot.Definition = definition;

                GameObject tractor = VehicleScaffoldBuilder.Create(
                    definition.Tractor, true,
                    definition.Tractor.Powertrain != null
                        ? VehicleDriveSource.SimplePowertrain
                        : VehicleDriveSource.LegacyDirectWheelTorque);
                tractor.name = "Tractor_" + definition.Tractor.VehicleName;
                tractor.transform.SetParent(assembly.transform, true);

                GameObject trailer = VehicleScaffoldBuilder.Create(
                    definition.Trailer, true, VehicleDriveSource.LegacyDirectWheelTorque);
                trailer.name = "Trailer_" + definition.Trailer.VehicleName;
                trailer.transform.SetParent(assembly.transform, true);

                // The tractor is placed at its ordinary level-ground ride
                // height by VehicleScaffoldBuilder. Match the trailer hitch
                // EXACTLY; otherwise ConfigurableJoint receives a giant
                // corrective impulse on its first simulated frame.
                trailer.transform.rotation = tractor.transform.rotation;
                trailer.transform.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                    tractor.transform.position, tractor.transform.rotation,
                    definition.TractorHitchLocal, trailer.transform.rotation,
                    definition.TrailerHitchLocal);
                Physics.SyncTransforms();

                Rigidbody towBody = tractor.GetComponent<Rigidbody>();
                VehicleCoupling coupling = Undo.AddComponent<VehicleCoupling>(trailer);
                VehicleCouplingResult joined = coupling.TryAttach(towBody, definition);
                if (joined != VehicleCouplingResult.Connected)
                    throw new System.InvalidOperationException(
                        "Could not attach trailer hitch: " + joined);

                assemblyRoot.Tractor = tractor.GetComponent<VehicleRoot>();
                assemblyRoot.Trailer = trailer.GetComponent<VehicleRoot>();
                assemblyRoot.Coupling = coupling;

                VehicleAssemblyMember towMember =
                    Undo.AddComponent<VehicleAssemblyMember>(tractor);
                towMember.Assembly = assemblyRoot;
                towMember.IsTractor = true;

                VehicleAssemblyMember towedMember =
                    Undo.AddComponent<VehicleAssemblyMember>(trailer);
                towedMember.Assembly = assemblyRoot;
                towedMember.IsTractor = false;

                AddHitchMarker(tractor.transform, definition.TractorHitchLocal,
                    "TractorHitchMarker");
                AddHitchMarker(trailer.transform, definition.TrailerHitchLocal,
                    "TrailerCouplingMarker");
                AddDrawbar(trailer.transform, definition.TrailerHitchLocal);

                Selection.activeGameObject = assembly;
                EditorGUIUtility.PingObject(assembly);
                return assembly;
            }
            catch
            {
                Undo.DestroyObjectImmediate(assembly);
                throw;
            }
        }

        public static bool SaveAssemblyPrefab(VehicleAssemblyDefinition definition)
        {
            if (VehicleAssemblyValidator.HasErrors(definition))
            {
                EditorUtility.DisplayDialog("KAVRA Assembly",
                    "AssemblyDefinition is missing or invalid.", "OK");
                return false;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save KAVRA Assembly Prefab", "KAVRA_TractorTrailer.prefab", "prefab",
                "Two-body assembly with an internal ConfigurableJoint.");
            if (string.IsNullOrEmpty(path)) return false;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA Assembly",
                    "Destination exists. Choose a new prefab filename.", "OK");
                return false;
            }

            GameObject temporary = null;
            try
            {
                temporary = Build(definition);
                bool succeeded;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporary, path, out succeeded);
                if (!succeeded || prefab == null)
                {
                    Debug.LogError("KAVRA Assembly: prefab could not be saved.");
                    return false;
                }

                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("KAVRA Assembly: saved two-body prefab " + path);
                return true;
            }
            finally
            {
                if (temporary != null) Undo.DestroyObjectImmediate(temporary);
            }
        }

        private static void AddHitchMarker(Transform parent, Vector3 local, string name)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.name = name;
            Undo.RegisterCreatedObjectUndo(point, "Create hitch marker");
            point.transform.SetParent(parent, false);
            point.transform.localPosition = local;
            point.transform.localScale = Vector3.one * 0.12f;
            Collider col = point.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        private static void AddDrawbar(Transform trailerRoot, Vector3 hitchLocal)
        {
            Vector3 towardsBody = new Vector3(0f, hitchLocal.y, -1.45f);
            Vector3 vector = hitchLocal - towardsBody;
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "__TRAILER_DRAWBAR_VISUAL";
            Undo.RegisterCreatedObjectUndo(bar, "Create trailer drawbar");
            bar.transform.SetParent(trailerRoot, false);
            bar.transform.localPosition = (towardsBody + hitchLocal) * 0.5f;
            bar.transform.localRotation = Quaternion.FromToRotation(Vector3.forward,
                vector.normalized);
            bar.transform.localScale = new Vector3(0.14f, 0.14f, vector.magnitude);
            Collider col = bar.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }
}
