# KAVRA embedded package (v0.0.6)

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
- `VehicleStaticLoadEstimator` computes first-order static wheel load shares
  using axle/wheel mount positions and configured center of mass.
- `AntiRollSolver` pairs left/right wheels of the same axle.
- `SuspensionSolver` adds progressive end-stop loading and reports bottom-out.
- `WheelContactProbe.TrySample` provides two contact modes and ray fallback.
- `WheelTelemetry` includes bottom-out, bump stop, anti-roll, and static
  load shares; selected vehicles show a magenta CoM gizmo.
- `SurfaceZone` is a separate opt-in MonoBehaviour for colliders;
  `SurfaceResponse` applies `SurfaceProfile` grip and rolling multipliers
  per wheel while leaving soft soil deformation for later.
- `VehicleManualRecovery.TryRecover(VehicleSimulationRoot)` is an explicit
  teleport API. It is NEVER invoked by the physics solver. Requires
  sufficiently low speed and valid ground under every wheel.
- Optional `MeasureContactQueryCost` provides a per-physics-step microsecond
  estimate of cast calls, not a frame-wide CPU benchmark.
- `SteeringGeometrySolver` computes geometry-aware approximate Ackermann
  per-wheel steering angles from the WheelDefinition layout. `Parallel`
  steering is retained for exact pre-0.0.5 compatibility.
- `TireTuningProfile` is an optional per-wheel ScriptableObject; the
  `TireTuningResolver` composes profile and road coefficients without
  modifying the shared assets. Null means legacy/default handling.
- `TireSlipDiagnostics` produces finite, bounded slip ratio and angle
  telemetry without changing the underlying provisional force formula.
- `WheelDynamicsSolver` now accepts OPTIONAL `BrakeEffectiveMassKg`,
  `BodyFixedDeltaTime` and `LowSpeedBrakeThresholdMps` for the near-stop
  brake impulse cap. All default to zero for callers using its old API.
- `VehicleSimulationRoot.LimitNearStopBrakingImpulse` (default true) only
  changes near-stationary locked-wheel braking, not high-speed tire friction
  or normal throttle behavior. Each wheel telemeters `BrakeImpulseLimited`.
  No static-contact solver or slope-hold guarantee is claimed.

### Editor

Menu `Tools > KAVRA > Vehicle Setup Wizard` creates a VehicleDefinition and:
- `3A`: original kinematic/non-drivable preview;
- `3B`: experimental dynamic prototype.

Package Editor builders and template factory are publicly callable from
host-project Editor scripts. They create a cube test collider and wheel
cylinders with **no** wheel physics colliders.

**M0 suspension changes:** spring rates/weights are computed at Awake using
`VehicleDefinition.CenterOfMassLocal`, and estimated lab inertia can be turned
on/off. `AntiRollRateFraction` defaults to a modest nonzero value. Alter
configuration assets and rebuild test chassis for load changes. Fine steering
and full suspension geometry are later work.

### Host sample (outside package)

`Assets/VehiclePhysicsLab` contains optional Input System keyboard driver,
chase camera and `Tools > KAVRA > Create M0 Physics Lab Scene` generator.
`Tools > KAVRA > Create M0 Calibration Run` creates a separate long asphalt
run with a `VehicleLabManeuverRunner` (brake/steer/coast schedules).
Scripted input is host-only and does not auto-prevent rollovers.

**Experimental only.** Uncalibrated tires, approximate CoM wheel shares,
no real soil deformation, direct test torque, no gearboxes/differentials.
Manual recovery is a lab control, not an automatic stability assistant.
No Unity compilation/Play Mode verification yet; use docs/13 and issue #1.
