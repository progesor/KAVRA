# KAVRA P1 — Common assembly control API

**Status:** Implemented in source, Unity 6000.3.18f1 verification pending.
Branch: `feat/m15-multitrailer-chain`. Package remains v0.1.6
until Unity acceptance.

`Progesor.Kavra.IVehicleAssemblyController` is now implemented by the
existing two-body `VehicleAssemblyRoot` and ordered N-body
`VehicleAssemblyChainRoot`. It is an **additive interface** that removes
host-side dependence on which assembly kind a consumer received.

## Public contract

- `VehicleCount`, `LinkCount`: zero-alloc runtime topology metadata.
- `GetVehicle(i)`, `GetCoupling(i)`: returns null if index invalid.
- `IsLinkAttached(i)`, `GetLinkState(i)`: read-only state.
- `ConnectedLeadVehicleCount()`: number of connected bodies from the lead.
- `AssessAttachLink(i, out assessment)`: read-only safety gate; invalid
  input returns `InvalidConfiguration`. Does not move a Rigidbody.
- `TryAttachLink(i)` and `DetachLink(i)`: explicit user/host commands;
  original speed, yaw and hitch-distance safety checks remain active.

Indices are zero-based. Link 0 connects vehicle 0 to vehicle 1. Only
index 0 is valid for the original two-body type; chains allow 0..N-1.

Example in a consuming Unity project:

```csharp
using Progesor.Kavra;

IVehicleAssemblyController control = assemblyRoot;
VehicleHitchAssessment preflight;
if (!control.IsLinkAttached(0) &&
    control.AssessAttachLink(0, out preflight) ==
        VehicleCouplingResult.Connected)
{
    // If the driver explicitly requests H:
    // control.TryAttachLink(0);
}
```

The example intentionally does not connect in Update. There is no
automatic reconnect or stabilization. Real joint breaks remain
`ConfigurableJoint` solver events; manual R recovery remains separate.
The UPM runtime does not depend on lab input, camera or rendering APIs.

## Evidence and next acceptance

Two new EditMode source tests cover both interfaces, link indices,
read-only preflight, explicit detach/attach, and connected-lead prefix.
The source-only CI validator now guards Unity assembly boundaries and
the existing hitch guide MonoScript GUID.

Actual Unity C# compilation, Test Runner, prefab roundtrip, checker
road/hitch marker visual acceptance, PhysX stress and R recovery
**have not been verified in this environment**. Keep issue #17 and
Draft PR #16 open. Existing scene and CSV files were not modified.
