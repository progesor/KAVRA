namespace Progesor.Kavra
{
    /// <summary>
    /// Allocation-free, host-facing API for either a two-body assembly or
    /// an ordered N-body chain. Link i connects vehicle i to vehicle i+1.
    /// Nothing auto-attaches, auto-detaches or auto-recovers a vehicle.
    /// </summary>
    public interface IVehicleAssemblyController
    {
        int VehicleCount { get; }
        int LinkCount { get; }
        VehicleRoot GetVehicle(int vehicleIndex);
        VehicleCoupling GetCoupling(int linkIndex);
        bool IsLinkAttached(int linkIndex);
        VehicleHitchState GetLinkState(int linkIndex);
        int ConnectedLeadVehicleCount();

        /// <summary>Read-only safety preflight: creates no joints.</summary>
        VehicleCouplingResult AssessAttachLink(int linkIndex,
            out VehicleHitchAssessment assessment);
        /// <summary>Explicit host command; invalid index means InvalidConfiguration.</summary>
        VehicleCouplingResult TryAttachLink(int linkIndex);
        /// <summary>Explicit host command; invalid index means false.</summary>
        bool DetachLink(int linkIndex);
    }
}
