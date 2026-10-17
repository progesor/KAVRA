using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Logical assembly parent. Has NO Rigidbody of its own; each vehicle
    /// has an independent Rigidbody and VehicleSimulationRoot.
    /// For now one driven tractor + one passive trailer is supported.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class VehicleAssemblyRoot : MonoBehaviour
    {
        public VehicleAssemblyDefinition Definition;
        public VehicleRoot Tractor;
        public VehicleRoot Trailer;
        public VehicleCoupling Coupling;

        public bool IsAttached => Coupling != null && Coupling.IsAttached;

        private void FixedUpdate()
        {
            PropagateBrakes();
        }

        /// <summary>
        /// Small reusable command relay. The trailer still simulates its own
        /// wheel braking/contact forces as a separate Rigidbody. Exposed for
        /// deterministic EditMode testing and future custom input adapters.
        /// </summary>
        public void PropagateBrakes()
        {
            // Basic brake command sharing. Trailer physics/contacts still
            // integrate separately from the powered tractor.
            if (!IsAttached || Definition == null ||
                Tractor == null || Trailer == null)
                return;

            VehicleSimulationRoot tow = Tractor.GetComponent<VehicleSimulationRoot>();
            VehicleSimulationRoot towed = Trailer.GetComponent<VehicleSimulationRoot>();
            if (tow == null || towed == null) return;

            towed.SetInputs(0f, 0f,
                Mathf.Clamp01(tow.ServiceBrake * Definition.TrailerServiceBrakeMultiplier),
                Definition.FollowTractorParkingBrake ? tow.ParkingBrake : 0f);
        }
    }
}
