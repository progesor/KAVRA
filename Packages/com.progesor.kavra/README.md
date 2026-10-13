# KAVRA embedded package (v0.0.2)

Unity 6.3 LTS `com.progesor.kavra`. Core is UnityEngine-only and remains
independent of rendering or input assets.

### Runtime

- `VehicleDefinition`, `VehicleRoot`, `VehicleWheelAnchor`: single-chassis
  configuration and wheel mounts.
- `VehicleSimulationRoot`: **optional dynamic prototype**. FixedUpdate probes
  contacts, integrates provisional wheel angular dynamics and applies spring,
  longitudinal and lateral forces to Rigidbody at contact positions.
- `SuspensionSolver`, `WheelDynamicsSolver`, `WheelContactProbe`: separable
  force components, first M0 approximations.
- `WheelTelemetry` in the simulation component: live per-wheel metrics.

### Editor

Menu `Tools > KAVRA > Vehicle Setup Wizard` creates a VehicleDefinition and:
- `3A`: original kinematic/non-drivable preview;
- `3B`: experimental dynamic prototype.

Package Editor builders and template factory are publicly callable from
host-project Editor scripts. They create a cube test collider and wheel
cylinders with **no** wheel physics colliders.

### Host sample (outside package)

`Assets/VehiclePhysicsLab` contains optional Input System keyboard driver,
chase camera and `Tools > KAVRA > Create M0 Physics Lab Scene` generator.

**Experimental only.** Uncalibrated tires, single ray per wheel, direct test
wheel torque, approximate equal wheel spring loads, no gearboxes/differentials.
No Unity compilation/Play Mode verification yet; use docs/13 and issue #1.
