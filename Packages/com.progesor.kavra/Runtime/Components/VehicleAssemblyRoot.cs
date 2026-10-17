using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Logical tractor/trailer assembly: no parent Rigidbody. Controls
    /// coupling lifecycle and a basic trailer-brake command relay.
    /// Each child keeps its own VehicleSimulationRoot and Rigidbody.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class VehicleAssemblyRoot : MonoBehaviour
    {
        public VehicleAssemblyDefinition Definition;
        public VehicleRoot Tractor;
        public VehicleRoot Trailer;
        public VehicleCoupling Coupling;

        private bool previouslyAttached;

        public bool IsAttached => Coupling != null && Coupling.IsAttached;
        public VehicleHitchState HitchState => Coupling != null
            ? Coupling.State : VehicleHitchState.Detached;

        /// <summary>Explicit player/API command. Never auto-connects nearby trailers.</summary>
        public VehicleCouplingResult TryAttach()
        {
            if (Definition == null || Tractor == null ||
                Trailer == null || Coupling == null)
                return VehicleCouplingResult.InvalidConfiguration;

            Rigidbody tractorBody = Tractor.GetComponent<Rigidbody>();
            VehicleCouplingResult outcome = Coupling.TryAttach(tractorBody, Definition);
            if (outcome == VehicleCouplingResult.Connected)
                previouslyAttached = true;
            return outcome;
        }

        /// <summary>Explicit manual disconnect (not a simulated break event).</summary>
        public bool Detach()
        {
            // Driver-operated unhitching is intentionally slow-speed only.
            // Physics BREAK thresholds still work at every speed.
            if (Definition == null || Tractor == null || Trailer == null ||
                Coupling == null || !IsAttached)
                return false;
            Rigidbody tow = Tractor.GetComponent<Rigidbody>();
            Rigidbody trailer = Trailer.GetComponent<Rigidbody>();
            if (tow == null || trailer == null) return false;
            float allowedSq = Definition.MaximumManualCouplingBodySpeedMps *
                              Definition.MaximumManualCouplingBodySpeedMps;
            if (tow.linearVelocity.sqrMagnitude > allowedSq ||
                trailer.linearVelocity.sqrMagnitude > allowedSq)
                return false;
            if (!Coupling.Detach()) return false;
            previouslyAttached = false;
            ClearTrailerBrakeCommands();
            return true;
        }

        private void FixedUpdate()
        {
            PropagateBrakes();
        }

        /// <summary>
        /// Relay service/parking brake only WHILE connected. After deliberate
        /// detachment or Unity physics overload break, do not leave the
        /// trailer stuck with yesterday's service/park brake values.
        /// </summary>
        public void PropagateBrakes()
        {
            if (!IsAttached)
            {
                if (previouslyAttached)
                    ClearTrailerBrakeCommands();
                previouslyAttached = false;
                return;
            }

            previouslyAttached = true;
            if (Definition == null || Tractor == null || Trailer == null) return;

            VehicleSimulationRoot tow = Tractor.GetComponent<VehicleSimulationRoot>();
            VehicleSimulationRoot towed = Trailer.GetComponent<VehicleSimulationRoot>();
            if (tow == null || towed == null) return;

            towed.SetInputs(0f, 0f,
                Mathf.Clamp01(tow.ServiceBrake * Definition.TrailerServiceBrakeMultiplier),
                Definition.FollowTractorParkingBrake ? tow.ParkingBrake : 0f);
        }

        private void ClearTrailerBrakeCommands()
        {
            if (Trailer == null) return;
            VehicleSimulationRoot towed = Trailer.GetComponent<VehicleSimulationRoot>();
            if (towed != null)
                towed.SetInputs(0f, 0f, 0f, 0f);
        }
    }
}
