using UnityEditor;
using UnityEngine;

namespace Progesor.Kavra.Editor
{
    /// <summary>
    /// Builds an ordered N-body assembly without sharing Rigidbody objects.
    /// Every trailer has its own Rigidbody, wheel solver and breakable hitch.
    /// </summary>
    public static class VehicleAssemblyChainBuilder
    {
        public static GameObject Build(VehicleAssemblyChainDefinition definition,
            bool recordUndo = true)
        {
            foreach (VehicleValidationIssue issue in
                     VehicleAssemblyChainValidator.Validate(definition))
                if (issue.Severity == ValidationSeverity.Error)
                    throw new System.InvalidOperationException(
                        "KAVRA chain configuration: " + issue.Message);

            var assemblyObject = new GameObject("KAVRA_AssemblyChain");
            if (recordUndo)
                Undo.RegisterCreatedObjectUndo(assemblyObject, "Create KAVRA chain");
            try
            {
                var chain = recordUndo
                    ? Undo.AddComponent<VehicleAssemblyChainRoot>(assemblyObject)
                    : assemblyObject.AddComponent<VehicleAssemblyChainRoot>();
                chain.Definition = definition;

                int bodyCount = definition.VehicleCount;
                var vehicles = new VehicleRoot[bodyCount];
                var couplings = new VehicleCoupling[bodyCount - 1];

                for (int i = 0; i < bodyCount; i++)
                {
                    VehicleDefinition source = i == 0
                        ? definition.Links[0].Tractor
                        : definition.Links[i - 1].Trailer;
                    // Only the lead chassis has an active drivetrain.
                    VehicleDriveSource driveSource = i == 0 && source.Powertrain != null
                        ? VehicleDriveSource.SimplePowertrain
                        : VehicleDriveSource.LegacyDirectWheelTorque;
                    GameObject body = VehicleScaffoldBuilder.Create(
                        source, true, driveSource, recordUndo);
                    body.name = (i == 0 ? "Tractor_" : "Trailer_" + i + "_") +
                                source.VehicleName;
                    body.transform.SetParent(assemblyObject.transform, true);

                    if (i > 0)
                    {
                        VehicleAssemblyDefinition edge = definition.Links[i - 1];
                        body.transform.rotation = vehicles[i - 1].transform.rotation;
                        body.transform.position = VehicleAssemblyMath.CalculateTrailerRootPosition(
                            vehicles[i - 1].transform.position,
                            vehicles[i - 1].transform.rotation,
                            edge.TractorHitchLocal, body.transform.rotation,
                            edge.TrailerHitchLocal);
                        Physics.SyncTransforms();

                        VehicleCoupling coupling = recordUndo
                            ? Undo.AddComponent<VehicleCoupling>(body)
                            : body.AddComponent<VehicleCoupling>();
                        VehicleCouplingResult result = coupling.TryAttach(
                            vehicles[i - 1].GetComponent<Rigidbody>(), edge);
                        if (result != VehicleCouplingResult.Connected)
                            throw new System.InvalidOperationException(
                                "KAVRA chain link " + (i - 1) +
                                " could not connect: " + result);
                        couplings[i - 1] = coupling;
                        AddHitchMarker(vehicles[i - 1].transform,
                            edge.TractorHitchLocal, "HitchRear_To_" + i,
                            recordUndo);
                        AddHitchMarker(body.transform, edge.TrailerHitchLocal,
                            "HitchFront_From_" + (i - 1), recordUndo);
                    }

                    vehicles[i] = body.GetComponent<VehicleRoot>();
                    var member = recordUndo
                        ? Undo.AddComponent<VehicleAssemblyChainMember>(body)
                        : body.AddComponent<VehicleAssemblyChainMember>();
                    member.Chain = chain;
                    member.VehicleIndex = i;
                }

                chain.Vehicles = vehicles;
                chain.Couplings = couplings;
                if (recordUndo)
                {
                    Selection.activeGameObject = assemblyObject;
                    EditorGUIUtility.PingObject(assemblyObject);
                }
                return assemblyObject;
            }
            catch
            {
                if (recordUndo) Undo.DestroyObjectImmediate(assemblyObject);
                else Object.DestroyImmediate(assemblyObject);
                throw;
            }
        }

        public static bool SavePrefab(VehicleAssemblyChainDefinition definition)
        {
            if (VehicleAssemblyChainValidator.HasErrors(definition))
            {
                EditorUtility.DisplayDialog("KAVRA Chain",
                    "Fix assembly-chain configuration errors first.", "OK");
                return false;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save KAVRA Multi-Trailer Prefab", "KAVRA_MultiTrailer.prefab",
                "prefab", "Save a real N-body prefab with connected joints.");
            if (string.IsNullOrEmpty(path)) return false;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                EditorUtility.DisplayDialog("KAVRA Chain",
                    "Destination already exists. Choose a new filename.", "OK");
                return false;
            }

            GameObject temporary = null;
            try
            {
                temporary = Build(definition, false);
                bool success;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporary, path, out success);
                if (!success || prefab == null) return false;
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("KAVRA: N-body vehicle chain saved at " + path);
                return true;
            }
            finally
            {
                if (temporary != null)
                    Object.DestroyImmediate(temporary);
            }
        }

        private static void AddHitchMarker(Transform parent,
            Vector3 localPosition, string name, bool recordUndo)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            if (recordUndo)
                Undo.RegisterCreatedObjectUndo(marker, "Create KAVRA hitch marker");
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = Vector3.one * 0.13f;
            Collider collision = marker.GetComponent<Collider>();
            if (collision != null) Object.DestroyImmediate(collision);
        }
    }
}
