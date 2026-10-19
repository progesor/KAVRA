using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// One imported model node assigned to a stable KAVRA WheelDefinition.Id.
    /// ModelNodePath is relative to ModelPrefab's root, not a Unity object ID.
    /// Paths survive reimport unless the author renames/reparents the nodes.
    /// </summary>
    [Serializable]
    public sealed class VehicleImportedWheelBinding
    {
        public string WheelId = "";
        public string ModelNodePath = "";
    }

    /// <summary>
    /// P2 visual-only mapping: imported FBX/model prefab (or an already
    /// imported GLB prefab) to one chassis definition. Neither this asset
    /// nor its imported source adds extra physical bodies or modifies
    /// the KAVRA contact/gearbox/hitch solvers.
    /// </summary>
    [CreateAssetMenu(fileName = "KAVRA_VisualBinding",
        menuName = "KAVRA/Imported Model Visual Binding")]
    public sealed class VehicleVisualBindingDefinition : ScriptableObject
    {
        public VehicleDefinition Vehicle;
        public GameObject ModelPrefab;

        [Header("Model-to-chassis visual alignment (meters / degrees)")]
        public Vector3 ModelLocalOffset = Vector3.zero;
        public Vector3 ModelLocalEuler = Vector3.zero;
        [Min(0.001f)] public float ModelScale = 1f;

        [Header("Manual wheel mapping by stable WheelId")]
        public List<VehicleImportedWheelBinding> WheelVisuals =
            new List<VehicleImportedWheelBinding>();

        [Header("Disable only preview renderers; keep physics collider")]
        public bool HideScaffoldChassisRenderer = true;
        public bool HideMatchedWheelPreviews = true;
    }
}
