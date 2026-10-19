using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Read-only link to the visual binding profile used by the P2 Wizard.
    /// Visuals have no Rigidbody, WheelCollider, or vehicle-input adapter.
    /// Mesh/material assets remain referenced by the generated prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleImportedVisualRoot : MonoBehaviour
    {
        public VehicleVisualBindingDefinition Binding;
        public Transform ImportedBodyVisual;
    }
}
