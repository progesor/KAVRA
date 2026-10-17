using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Enables safe whole-prefix manual recovery for a vehicle in a convoy.
    /// On a detached chain, only the connected prefix behind the lead
    /// vehicle is affected by the user's R key.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleAssemblyChainMember : MonoBehaviour
    {
        public VehicleAssemblyChainRoot Chain;
        [Min(0)] public int VehicleIndex;
        public bool IsLead => VehicleIndex == 0;
    }
}
