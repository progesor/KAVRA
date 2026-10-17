using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// One driven lead + ordered passive trailer chain.
    /// This parent has NO Rigidbody; each member has its own physics body,
    /// while each link is an independent breakable VehicleCoupling.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    [DisallowMultipleComponent]
    public sealed class VehicleAssemblyChainRoot : MonoBehaviour, IVehicleAssemblyController
    {
        public VehicleAssemblyChainDefinition Definition;
        public VehicleRoot[] Vehicles;
        public VehicleCoupling[] Couplings;
        [SerializeField] private int selectedLinkIndex;

        private bool[] linkedLastPhysicsStep;

        public int LinkCount => Couplings != null ? Couplings.Length : 0;
        public int VehicleCount => Vehicles != null ? Vehicles.Length : 0;
        public VehicleRoot GetVehicle(int vehicleIndex) =>
            Vehicles != null && vehicleIndex >= 0 &&
            vehicleIndex < Vehicles.Length ? Vehicles[vehicleIndex] : null;
        public VehicleCoupling GetCoupling(int linkIndex) =>
            Couplings != null && linkIndex >= 0 &&
            linkIndex < Couplings.Length ? Couplings[linkIndex] : null;
        public VehicleHitchState GetLinkState(int linkIndex)
        {
            VehicleCoupling coupling = GetCoupling(linkIndex);
            return coupling != null ? coupling.State : VehicleHitchState.Detached;
        }

        public VehicleCouplingResult AssessAttachLink(int linkIndex,
            out VehicleHitchAssessment assessment)
        {
            assessment = default(VehicleHitchAssessment);
            if (Definition == null || Definition.Links == null ||
                linkIndex < 0 || linkIndex >= LinkCount ||
                linkIndex >= Definition.Links.Count ||
                GetVehicle(linkIndex) == null || GetVehicle(linkIndex + 1) == null ||
                GetCoupling(linkIndex) == null)
                return assessment.Result = VehicleCouplingResult.InvalidConfiguration;
            return Couplings[linkIndex].AssessAttach(
                Vehicles[linkIndex].GetComponent<Rigidbody>(),
                Definition.Links[linkIndex], out assessment);
        }
        public int SelectedLinkIndex => LinkCount > 0
            ? Mathf.Clamp(selectedLinkIndex, 0, LinkCount - 1) : 0;

        private void Awake()
        {
            EnsureRelayState();
        }

        private void EnsureRelayState()
        {
            if (Vehicles != null &&
                (linkedLastPhysicsStep == null ||
                 linkedLastPhysicsStep.Length != Vehicles.Length))
                linkedLastPhysicsStep = new bool[Vehicles.Length];
        }

        public void SelectLink(int index)
        {
            if (LinkCount > 0)
                selectedLinkIndex = Mathf.Clamp(index, 0, LinkCount - 1);
        }

        public void SelectRelativeLink(int offset)
        {
            if (LinkCount == 0) return;
            int mod = (SelectedLinkIndex + offset) % LinkCount;
            selectedLinkIndex = mod < 0 ? mod + LinkCount : mod;
        }

        public bool IsLinkAttached(int linkIndex)
        {
            return Couplings != null && linkIndex >= 0 &&
                   linkIndex < Couplings.Length &&
                   Couplings[linkIndex] != null &&
                   Couplings[linkIndex].IsAttached;
        }

        /// <summary>Number of vehicles still connected to the LEAD unit.</summary>
        public int ConnectedLeadVehicleCount()
        {
            if (Vehicles == null || Vehicles.Length == 0) return 0;
            int count = 1;
            while (count < Vehicles.Length && IsLinkAttached(count - 1))
                count++;
            return count;
        }

        public VehicleCouplingResult TryAttachLink(int index)
        {
            if (Definition == null || Definition.Links == null ||
                Vehicles == null || Couplings == null ||
                index < 0 || index >= LinkCount ||
                index >= Definition.Links.Count ||
                index + 1 >= Vehicles.Length ||
                Vehicles[index] == null || Vehicles[index + 1] == null ||
                Couplings[index] == null)
                return VehicleCouplingResult.InvalidConfiguration;

            Rigidbody parentBody = Vehicles[index].GetComponent<Rigidbody>();
            VehicleCouplingResult result = Couplings[index].TryAttach(
                parentBody, Definition.Links[index]);
            if (result == VehicleCouplingResult.Connected)
                PropagateBrakes();
            return result;
        }

        public bool DetachLink(int index)
        {
            if (Definition == null || Definition.Links == null ||
                Vehicles == null || Couplings == null ||
                index < 0 || index >= LinkCount ||
                index + 1 >= Vehicles.Length ||
                index >= Definition.Links.Count ||
                !IsLinkAttached(index))
                return false;

            VehicleAssemblyDefinition settings = Definition.Links[index];
            Rigidbody front = Vehicles[index].GetComponent<Rigidbody>();
            Rigidbody rear = Vehicles[index + 1].GetComponent<Rigidbody>();
            if (front == null || rear == null || settings == null) return false;
            float maxSquared = settings.MaximumManualCouplingBodySpeedMps *
                               settings.MaximumManualCouplingBodySpeedMps;
            if (front.linearVelocity.sqrMagnitude > maxSquared ||
                rear.linearVelocity.sqrMagnitude > maxSquared)
                return false;

            if (!Couplings[index].Detach()) return false;
            ClearBrakeCommandsStartingAt(index + 1);
            return true;
        }

        public VehicleCouplingResult TryAttachSelected() =>
            TryAttachLink(SelectedLinkIndex);

        public bool DetachSelected() => DetachLink(SelectedLinkIndex);

        private void FixedUpdate()
        {
            PropagateBrakes();
        }

        /// <summary>
        /// Relay brakes through ONLY the links connected all the way back to
        /// the driven lead. If any earlier link breaks, a still-connected
        /// downstream subgroup no longer receives phantom tractor brakes.
        /// </summary>
        public void PropagateBrakes()
        {
            if (Definition == null || Definition.Links == null ||
                Vehicles == null || Couplings == null)
                return;
            EnsureRelayState();
            if (linkedLastPhysicsStep == null) return;

            bool connectedToLead = true;
            for (int i = 1; i < Vehicles.Length; i++)
            {
                connectedToLead = connectedToLead && IsLinkAttached(i - 1);
                if (Vehicles[i] == null) continue;
                VehicleSimulationRoot towed =
                    Vehicles[i].GetComponent<VehicleSimulationRoot>();
                if (towed == null) continue;

                if (connectedToLead && Vehicles[i - 1] != null &&
                    i - 1 < Definition.Links.Count)
                {
                    VehicleSimulationRoot front =
                        Vehicles[i - 1].GetComponent<VehicleSimulationRoot>();
                    VehicleAssemblyDefinition settings = Definition.Links[i - 1];
                    if (front != null && settings != null)
                    {
                        towed.SetInputs(0f, 0f,
                            Mathf.Clamp01(front.ServiceBrake *
                                          settings.TrailerServiceBrakeMultiplier),
                            settings.FollowTractorParkingBrake
                                ? front.ParkingBrake : 0f);
                        linkedLastPhysicsStep[i] = true;
                        continue;
                    }
                }

                if (linkedLastPhysicsStep[i])
                {
                    towed.SetInputs(0f, 0f, 0f, 0f);
                    linkedLastPhysicsStep[i] = false;
                }
            }
        }

        private void ClearBrakeCommandsStartingAt(int startVehicleIndex)
        {
            EnsureRelayState();
            if (Vehicles == null || linkedLastPhysicsStep == null) return;
            for (int i = Mathf.Max(1, startVehicleIndex); i < Vehicles.Length; i++)
            {
                if (Vehicles[i] == null) continue;
                VehicleSimulationRoot sim =
                    Vehicles[i].GetComponent<VehicleSimulationRoot>();
                if (sim != null) sim.SetInputs(0f, 0f, 0f, 0f);
                linkedLastPhysicsStep[i] = false;
            }
        }
    }
}
