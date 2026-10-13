using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// One rigid chassis and its vehicle definition. The Wizard can produce
    /// either a kinematic visual scaffold or an opt-in dynamic M0 prototype.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleRoot : MonoBehaviour
    {
        public VehicleDefinition Definition;
    }
}
