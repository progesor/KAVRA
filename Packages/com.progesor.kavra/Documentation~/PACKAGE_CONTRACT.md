# Package contract (M0)

The core assembly references UnityEngine only. The Editor assembly references
UnityEditor plus the runtime assembly. No dependency on URP, Input System,
Cinemachine, Terrain Tools, Burst or optional physics middleware exists in M0.

**Expected future dependencies:** keep rendering and input in optional adapters,
not in `Progesor.Kavra.Runtime`.

**Data directions:** +X right, +Y up, +Z forward. SI units: m, kg, s, N,
N·m, rad/s internally; user interfaces may display km/h and rpm.

**Single-chassis semantics:** one VehicleDefinition = one chassis. Tractor,
trailer or articulated machines will use a composite VehicleAssembly with
independent Rigidbody nodes and mechanical couplings.

**Public API:** pre-1.0 experimental. Serialization migrations must be explicitly
planned before any field renames.
