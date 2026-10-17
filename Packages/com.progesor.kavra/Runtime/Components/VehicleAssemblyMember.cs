using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Marks a Rigidbody as part of an attached assembly so that a one-body
    /// teleport cannot tear the joint. No automatic recovery is performed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleAssemblyMember : MonoBehaviour
    {
        public VehicleAssemblyRoot Assembly;
        public bool IsTractor;
        public bool IsCoupled => Assembly != null && Assembly.IsAttached;
    }
}
