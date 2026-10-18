# KAVRA embedded package (v0.1.6)

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


## M1 core vehicle drive option (0.1.0)

- `VehicleDefinition.Powertrain` is optional. Null remains valid for all
  earlier vehicles and does not alter serialized tire/axle configuration.
- `SimplePowertrainDefinition` is a ScriptableObject with one engine torque
  curve (Nm/RPM), 1..N forward gear ratios, -1 reverse, 0 neutral, final
  drive and mechanical efficiency.
- `SimplePowertrainRuntime` calculates per-wheel torque once per fixed
  chassis step, groups driven wheels by their `AxleId`, splits total torque
  equally among driven axles, then equally among driven wheels on each axle.
  `WheelDefinition.CanDrive` is the physical drive authority; the axle
  IsDriven flag is currently metadata.
- `VehicleSimulationRoot.DriveSource` defaults
  `LegacyDirectWheelTorque` for backward compatibility. Choose
  `SimplePowertrain` explicitly in a new M1 vehicle.
- `TrySetGear(int)` selects -1 reverse, 0 neutral or 1..ForwardGearCount.
  Host controllers handle speed-aware direction changes. Gear clutches and
  differential reactions are future modules, not M1's prototype.
- EngineRPM, EngineTorqueNm, TotalAppliedDriveTorqueNm and wheel-level
  AppliedDriveTorqueNm are available for telemetry and host presentation.

The embedded runtime package still references only UnityEngine; sample
Input System adapters and test scenes belong to the separate Unity host
at `Assets/VehiclePhysicsLab`. No automatic anti-rollover handling is added.

### Save powered prefab

`VehiclePrefabBuilder` is an Editor-only helper exposed through the Wizard:
`4D. Save M1 powered prefab (reusable)`. It saves a real Unity Prefab
with a Rigidbody, WheelAnchors, VehicleRoot/VehicleSimulationRoot and
links to VehicleDefinition/SimplePowertrainDefinition assets. The generated
cube and cylinders remain **temporary placeholders**, not production-grade
physics/visual meshes. It does not add host input or require URP.

### M1.1 axle routing and differential-command modes

`AxleDefinition` now contains `DriveTorqueWeight`,
`DifferentialMode`, `MaximumSpeedBiasRatio` and
`SpeedBiasGainPerRadPerSec`. Normalized axle weights distribute power
across the driven `AxleId` groups while `WheelDefinition.CanDrive`
remains authoritative. Existing wheels and cars retain equal-torque
allocation by default; absent/zero weights normalize to one.

`EqualTorque` sends equal requested torque to driven wheels on an axle.
`SpeedBiasedApproximation` changes only TORQUE SHARES when one driven
wheel spins significantly faster than another on the same axle. No
mechanical differential rotational constraint or wheel-speed lock is
implemented. The new Wizard UI exposes these choices per axle and allows
enabling/disabling its powered wheels at once.

The host has new 3W/6x6/8x8/20W test scene generator menus. These are
not production-art demos; imported mechanical models are a later issue.

### M1.2 gearbox and visual-binding foundation

`VehicleGearboxController`: allocation-free state machine with optional
`GearboxControlMode.Automatic`. Manual remains the default to avoid changing
existing M1 behavior except during explicit gear requests. A shift interrupts
engine-to-wheel torque for a configurable duration; neutral/reverse and
longitudinal direction safety are explicit. This is a simplified gear state,
**not a physical friction clutch or torque converter**.

`VehicleSimulationRoot` exports `EngagedGear`, `SelectedGear` target,
`IsGearShifting`, `GearShiftProgress01` and `GearboxMode`. A shared
`SimplePowertrainDefinition` holds time/RPM thresholds; engine/wheel states
remain per Rigidbody vehicle.

`VehicleWheelVisualRig` is an optional visual-only component attached to
`VehicleWheelAnchor`. Pivot references: `TravelPivot`, `SteeringPivot`,
`SpinPivot`. `VehicleSuspensionVisualLink` scales/reorients an optional
cylinder or modeled mechanical rod between assigned upper/lower endpoints.

The Editor `VehicleWheelVisualRigBuilder.Bind` creates missing pivot rigs
without changing anchor transforms, and new M1 primitive chassis presets
include demonstration shocks. Older M0 prefabs/scenes with no rig still use
the original direct cylinder visual animation path. There are no added
dependencies on URP, imported meshes or the Input System.

### M1.3 multi-Rigidbody assembly

`VehicleAssemblyDefinition` references a tractor and a separate trailer
VehicleDefinition. `VehicleCoupling` attaches a trailer Rigidbody
to a tractor Rigidbody through a ConfigurableJoint. Angular Y is
free for articulation; X/Z rotations are limited, all linear motion
is locked at a coincident hitch. `VehicleAssemblyRoot` relays basic
service/parking brake commands and owns member references; there is no
additional Rigidbody on the assembly parent.

`VehicleAssemblyMath`, `VehicleAssemblyValidator` and
`VehicleAssemblyRecovery` keep geometry/validation/recovery independent
of Editor tools. Manual R recovery relocates **both** bodies after
preflight; no automatic rollover correction is ever called from a
physics loop. Imported complex chassis volumes will eventually need a
richer recovery clearance provider.

Editor menus:
`Tools > KAVRA > Vehicle Assembly Wizard` and
`Tools > KAVRA > Create M1.3 Tractor Trailer Lab Scene`.
The former creates/edits configuration and saves a truly two-Rigidbody
prefab; the second produces a 6x6+4W sample in the host project.
No UPM runtime dependency on the host's Input System or URP.

### M1.4 runtime coupling lifecycle

`VehicleCoupling.TryAttach` preflights hitch distance, projected heading,
both body speeds and relative velocity AT THE HITCH before creating a
ConfigurableJoint. Optional short trailer-position alignment is triggered
only during an accepted *explicit attach request*.

`VehicleAssemblyRoot.TryAttach/Detach` exposes driver-facing commands;
manual detach is limited to very low speeds. The physical joint uses
`breakForce` and `breakTorque` from VehicleAssemblyDefinition. PhysX
overload triggers MonoBehaviour.`OnJointBreak(float)` and Unity destroys
the joint; a state/count/last breaking force are available for UI.
Neither shock load nor a loss of contact calls a user-originated
Detach() method. No automatic reattachment.

When a joint is removed, the assembly releases stale trailer brake
commands instead of repeatedly applying the tractor's controls.
`VehicleAssemblyDefinition` upgrades old M1.3 serialized settings.
The zero-value break thresholds mean Infinity/unbreakable on new
definition assets.

### M1.5 ordered multi-body vehicle chains

`VehicleAssemblyChainDefinition` lists 1..16 ordered
`VehicleAssemblyDefinition` edges. Each has a distinct tractor/trailer
pair; edge i+1 reuses edge i's Trailer as its Tractor. Vehicle assets
for different chassis nodes must be separate objects.

Runtime `VehicleAssemblyChainRoot` holds arrays of separate VehicleRoot
and VehicleCoupling objects. A fully connected chain has N vehicles
and N-1 real ConfigurableJoint components. No fixed truck/wheel count.
`TryAttachLink(i)`, `DetachLink(i)`, `SelectLink(i)`,
`TryAttachSelected()`, `DetachSelected()` provide user-controlled
lifecycle. A physically broken joint on one edge does not affect
other joint ownership/state automatically.

Basic service/parking brake signals propagate from the lead only
through continuously connected links. All trailer wheel contact and
suspension simulation still occurs independently per Rigidbody.
`VehicleAssemblyChainRecovery` is reached only from deliberate R on
lead driver input; it preflights and repositions ONLY the connected
lead prefix (never the disconnected suffix).

An Editor-only `VehicleAssemblyChainBuilder` and chain Wizard build
and save N-body prefabs; PhysicsLab remains outside UPM. No extra runtime
packages. This is an ordered convoy rather than a branched coupling graph.

### M1.5.1 easier H capture (rendering remains host-only)

`VehicleAssemblyDefinition` new defaults:
`MaximumAttachErrorMeters=0.5` and
`MaximumAttachYawDegrees=55`. Preflight speed/yaw/distance
checks and explicitly triggered one-time snap are still mandatory.
Old unchanged M1.4 values 0.15/35 migrate to new defaults; different
user-authored tolerances remain unchanged.

`VehicleCoupling.AssessAttach(tractorBody, settings,
out VehicleHitchAssessment)` exposes the SAME checks used by
`TryAttach`, but never adds a joint or moves a Rigidbody. The struct
reports gap, heading difference, relative hitch velocity, max body
speed and reason/status. This enables optional UI highlight/ready
guides for consumers without depending on host materials.

All checker road textures, shaders, vehicle colors, marker rendering
and speed-sensitive FOV are optional PhysicsLab host scripts under
`Assets/VehiclePhysicsLab`; the KAVRA UPM runtime remains
renderer-/render-pipeline-/input-system-independent.

## P1 development-only acceptance (2026-10-18)

The host's optional EditMode tests now include
`KavraDefinitionValidationEditModeTests` (corrupt ScriptableObject inputs)
and `KavraPrefabRoundtripEditModeTests` (4W, 20W, two-body and N-body
prefab reference retention). They create temporary Assets folders
and delete only those folders after each run. They do not reside in or
run as part of the portable UPM runtime.

On Windows, close the running Editor and execute
`scripts/Run-KavraUnityEditMode.ps1` from the repository root to
collect an actual Unity 6.3 Test Runner result. No real Unity PASS
has been received as of this source update.
