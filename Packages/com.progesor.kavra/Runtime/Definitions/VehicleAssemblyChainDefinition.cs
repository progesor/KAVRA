using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Ordered connected-body definitions: link i's Trailer is link i+1's
    /// Tractor. Each link independently owns hitch geometry and strength.
    /// No branching or recursive self-connections in this first chain model.
    /// </summary>
    [CreateAssetMenu(fileName = "KAVRA_AssemblyChain",
        menuName = "KAVRA/Vehicle Assembly Chain")]
    public sealed class VehicleAssemblyChainDefinition : ScriptableObject
    {
        public List<VehicleAssemblyDefinition> Links =
            new List<VehicleAssemblyDefinition>();

        public int VehicleCount => Links != null ? Links.Count + 1 : 0;
    }
}
