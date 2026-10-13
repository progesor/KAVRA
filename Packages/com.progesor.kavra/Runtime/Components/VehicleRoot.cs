using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Marks a single-chassis vehicle scaffold. The M0 release does not drive
    /// the Rigidbody. Kinematic mode is intentional until real solvers arrive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleRoot : MonoBehaviour
    {
        public VehicleDefinition Definition;
    }
}
