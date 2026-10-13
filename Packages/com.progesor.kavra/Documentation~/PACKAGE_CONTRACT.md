# Package contract (M0 v0.0.2)

Core `Progesor.Kavra.Runtime` references UnityEngine only. Editor assembly
references UnityEditor plus runtime. **No** dependency on URP, Input System,
Cinemachine, Terrain Tools, Burst or paid physics libraries in the UPM package.

World convention: +X right / +Y up / +Z forward; SI (m, kg, s, N, N·m, rad/s).
One `VehicleDefinition` is one rigid chassis, *not* a tractor-trailer.
Composite multi-body coupling is reserved for a future assembly module.

### M0 dynamic prototype semantics

- `VehicleWheelAnchor` is a fixed mount. At full compression the wheel
  center equals the mount; at free droop it lies `travel` downward.
- `VehicleSimulationRoot` is opt-in and requires `VehicleRoot`, a
  non-kinematic Rigidbody and one uniquely named anchor for every wheel.
- Positive suspension travel is required in the **M0 prototype**.
- Contact is a single downward nonalloc ray per wheel. Own colliders are
  excluded. QueryTriggerInteraction.Ignore.
- No WheelCollider or wheel collider friction; forces are computed separately
  and applied through Rigidbody.AddForceAtPosition.
- Internal wheel angular steps do not substep the Unity Rigidbody solver.
- M0 assumes equal nominal sprung mass per wheel and linear springs; more
  general axle loading, active suspension and load sharing are later work.
- Direct drive torque applied per driven wheel is a **test actuator**, not a
  differential, engine, transmission or final production control scheme.
- `WheelTelemetry` array is read-only through the public Telemetry property
  but mutable via C# array casting: consumers should treat it as a snapshot.
- No guarantee of bit-for-bit determinism or physical calibration.

### Boundaries

Keyboard/chase camera/example lab generation live in the **host project**
`Assets/VehiclePhysicsLab`, not in the reusable runtime package.

Public API is experimental until 1.0. Serialized field renames require a
migration policy. Unity compilation and Play Mode verification have not yet
been completed. Track acceptance in GitHub issue #1.
