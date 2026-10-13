using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Optional Collider -> SurfaceProfile binding. Attach to the hit Collider
    /// or one of its parents. Missing bindings use default tire parameters.
    /// Does not modify PhysicsMaterial or underlying Unity collider friction.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurfaceZone : MonoBehaviour
    {
        public SurfaceProfile Profile;
    }
}
