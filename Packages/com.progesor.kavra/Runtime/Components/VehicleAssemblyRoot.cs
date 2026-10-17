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
    public sealed class VehicleAssemblyRoot : MonoBehaviour, IVehicleAssemblyController
    {
        public VehicleAssemblyDefinition Definition;
        public VehicleRoot Tractor;
        public VehicleRoot Trailer;
        public VehicleCoupling Coupling;

        private bool previouslyAttached;

        public bool IsAttached => Coupling != null && Coupling.IsAttached;
        public VehicleHitchState HitchState => Coupling != null
            ? Coupling.State : VehicleHitchState.Detached;

        // P1 additive API: original M1.4 commands, brake relay and R
        // manual recovery remain unchanged.
        public int VehicleCount => Tractor == null ? 0 : Trailer == null ? 1 : 2;
        public int LinkCount => Coupling == null ? 0 : 1;
        public VehicleRoot GetVehicle(int vehicleIndex) =>
            vehicleIndex == 0 ? Tractor : vehicleIndex == 1 ? Trailer : null;
        public VehicleCoupling GetCoupling(int linkIndex) =>
            linkIndex == 0 ? Coupling : null;
        public bool IsLinkAttached(int linkIndex) =>
            linkIndex == 0 && IsAttached;
        public VehicleHitchState GetLinkState(int linkIndex) =>
            linkIndex == 0 ? HitchState : VehicleHitchState.Detached;
        public int ConnectedLeadVehicleCount() =>
            Tractor == null ? 0 : (IsAttached && Trailer != null ? 2 : 1);

        public VehicleCouplingResult AssessAttachLink(int linkIndex,
            out VehicleHitchAssessment assessment)
        {
            assessment = default(VehicleHitchAssessment);
            if (linkIndex != 0 || Definition == null || Tractor == null ||
                Trailer == null || Coupling == null)
                return assessment.Result = VehicleCouplingResult.InvalidConfiguration;
            return Coupling.AssessAttach(
                Tractor.GetComponent<Rigidbody>(), Definition, out assessment);
        }

        public VehicleCouplingResult TryAttachLink(int linkIndex) =>
            linkIndex == 0 ? TryAttach() : VehicleCouplingResult.InvalidConfiguration;

        public bool DetachLink(int linkIndex) =>
            linkIndex == 0 && Detach();

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
