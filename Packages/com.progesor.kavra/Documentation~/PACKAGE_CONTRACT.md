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


## M0.5 optional wheel tuning and steering (0.0.5)

`WheelDefinition.TireProfile` may reference a `TireTuningProfile`
ScriptableObject. Null is legal, backward compatible and applies no multipliers
to the vehicle-wide defaults. Tire profile and road SurfaceProfile multipliers
combine independently at each current contact; shared assets are never mutated.
The prototype does not implement real tire pressure, thermal model or MF coefficients.

`VehicleSimulationRoot.SteeringMode` defaults to legacy Parallel to preserve
existing scenes. ApproximateAckermann geometry is inferred from the mean
Z positions of steerable and nonsteerable wheels at vehicle initialization.
No 4-wheel assumption or automatic roll/yaw correction is present. Mixed
front/rear simultaneous steering groups require a future explicit topology.

`TireSlipDiagnostics` reports regularized slip ratio/angle but does not drive
M0's provisional velocity-slip force model. Values are meaningful as internal
comparison plots only, not validation against instrumented road tests.


## M0.5 0.0.6 near-stop braking contract

`WheelDynamicsInput.BrakeEffectiveMassKg` and
`BodyFixedDeltaTime` and `LowSpeedBrakeThresholdMps` are optional new
inputs (0 means disabled). The force limiter only applies when:
- service brake torque dominates drive torque;
- longitudinal and wheel peripheral speeds are below threshold;
- tire longitudinal force opposes ground travel;
- effective wheel mass and **full Rigidbody physics timestep** are positive.

It caps the opposing Fx to no more than
`BrakeEffectiveMassKg * abs(longitudinalSpeed) / BodyFixedDeltaTime`.
Wheel rotor substep `DeltaTime` remains independent.

`VehicleSimulationRoot.LimitNearStopBrakingImpulse` defaults true for new
components, and nominal per-wheel effective mass comes from initial static
load fractions. No profile or solver settings are silently changed. The
braking force remains approximate for yawed, steep or multiple-ground-body
situations; static friction/slope holding and exact effective mass resolution
are future work. This is **not automatic roll or rollover prevention**.


## M1 0.1.0 contract — transitional powertrain module

`SimplePowertrainDefinition` is optional (null on all earlier
VehicleDefinition assets). Existing VehicleSimulationRoot serialized
`DriveSource` enum defaults to 0 = LegacyDirectWheelTorque.
This means the user's v0.0.5/0.0.6 experiments and existing scenes remain
in the M0 drive model unless they explicitly choose the M1 mode.

`SimplePowertrainRuntime` is a plain C# stateful solver, constructed once
from a torque-curve asset and a variable-length wheel layout. Engine RPM is
an approximate function of wheel RPM plus low-speed throttle free-rev
behavior, following a finite rate. It computes signed total output torque
through the selected gear/final drive/efficiency, then shares it equally
among **driven axle IDs** and among the driven wheels on each axle.
There is NO clutch constraint, torque-converter fluid coupling, LSD,
locked or open differential mechanical state, per-axle torque biasing,
motor inertia torque feedback or engine drag. These belong to M2 modules.

`VehicleSimulationRoot.TrySetGear` is an explicit gear API. Runtime
accepts -1 reverse, 0 neutral and 1..N forward gears and fails closed on
invalid ratios/gear. A host adapter should avoid selecting reverse during
forward motion; the included sample does this. The physical tire solver
still integrates the wheel's response and contact load.

SimplePowertrain source uses only UnityEngine and no imported packages.
It does not implement automatic rollover prevention. The optional manual
R recovery remains unchanged.


## M1.1 axle group configuration

All drivetrain settings are optional additional serialized fields on
`AxleDefinition`: `DriveTorqueWeight` (relative positive share),
`DifferentialMode` (0 = EqualTorque default, 1 = SpeedBiasedApproximation),
`MaximumSpeedBiasRatio`, `SpeedBiasGainPerRadPerSec`.
Zero/missing historical `DriveTorqueWeight` resolves to 1 to retain
existing assets; to disable a driven axle, set the appropriate
`WheelDefinition.CanDrive = false` instead. `IsDriven` is kept as a
descriptive flag, not an overriding drive eligibility rule.

`SimplePowertrainRuntime(config,wheels,axles)` can now read the group
configuration. Legacy `SimplePowertrainRuntime(config,wheels)` remains
source-compatible and uses equal power per driven axle and equal torque
per driven wheel within each axle.

Speed-biased mode uses relative wheel angular-speed MAGNITUDE with a
3 rad/s deadband, preferring slower driven wheels up to a configured
maximum torque share ratio. It is only a bounded torque **command** model,
not a physical open or limited-slip differential constraint. Redline
protection may cause unused engine power, which is not redistributed
after limiting an overspeeding wheel. No auto-righting/roll stability logic.


## M1.2 — gearbox state and wheel presentation contract

`VehicleGearboxController` owns engaged/target gear, duration, progress,
and temporary 0/1 torque coupling. Manual/default avoids altering existing
gear choices automatically; the optional Automatic mode uses previous
step RPM for forward up/downshift with cooldown and hysteresis. Direction
changes at >0.75 m/s in the opposite longitudinal direction are rejected.
This transition model has **no mechanical clutch shaft or synchronizer
forces**. It does not auto-right a rolled or overturned vehicle.

`SimplePowertrainDefinition` has four new optional tuning fields:
`ShiftDurationSeconds`, `MinTimeBetweenShiftsSeconds`,
`AutomaticUpshiftRpm`, `AutomaticDownshiftRpm`. Older serialized assets
with missing automatic thresholds use engine idle/redline-derived fallback.

`VehicleWheelVisualRig` is optional and only updates its own Visual
Transform hierarchy. The physical `VehicleWheelAnchor` and its world
suspension mount are NEVER moved by this component. Imported Unity meshes
can be assigned under the travel/steering/spin pivots; the mesh can carry
its native orientation under `SpinPivot`, preserving stable axes.

`VehicleSuspensionVisualLink` is an optional visual-only interpolation
of a mesh between two attachment transforms. It tracks length and
orientation, but does NOT contribute suspension force, Rigidbody joints
or damage events. The initial local mesh scale is serialized so prefabs
do not apply a second scale reduction at load time.

`VehicleWheelVisualRigBuilder` is Editor-only, can generate rigs on
new M1 prototypes (optionally preview shock cylinders) and can bind
currently selected existing vehicles through Tools menu. For existing
M0 scenes with no rig, the original VisualTransform path is retained.


## M1.3 — actual multi-body coupling contracts

`VehicleAssemblyDefinition` is a ScriptableObject with two **distinct**
single-chassis `VehicleDefinition` refs, local hitch anchors, pitch/roll
limits and basic brake-command ratio. World-space hitch alignment uses
`VehicleAssemblyMath.CalculateTrailerRootPosition`, which assumes
unit vehicle Transform scales.

`VehicleCoupling` is installed on the TRAILER rigidbody and owns its
ConfigurableJoint. It rejects missing/identical/kinematic bodies,
already attached joints, separated anchors, and invalid definitions.
The joint connects to the tractor rigidbody and locks all three
translations, permits free angular Y yaw and limited angular X pitch /
angular Z roll. Local joint axis = right, secondaryAxis = up. Projection
OFF by default, and connected-body collisions disabled. This is a first
engine-driven constraint, not custom suspension link finite elements.
Detach disables/destroys the joint; new attachment is an explicit action.

`VehicleAssemblyRoot` is a reference holder and trailer-brake command
relay on a **non-Rigidbody** parent. There are exactly two physics
Rigidbodies in the starter. Trailer WheelDefinitions are not powered,
but their normal road/suspension/rolling/braking forces are simulated
by their own VehicleSimulationRoot.

When `VehicleManualRecovery.TryRecover` sees a *currently attached*
`VehicleAssemblyMember`, it routes an explicit R request to
`VehicleAssemblyRecovery`. This checks both bodies' speed, both sets
of wheel-ground samples and collider clearance BEFORE repositioning
either. It straightens the assembly at its current horizontal position,
restores hitch alignment and clears both bodies' transients. This
teleport is never invoked as an automatic safety system.

Assembly topology currently contains one tractor + one trailer; future
VehicleAssembly graphs can reuse the coupling component for multiple
trailers and powered modules. This simple 6x6+4W rig is **not** a finished
18-wheel tractor-trailer, nor a calibrated real-world fifth-wheel/
pneumatic-brake model. No new package dependency.


## M1.4 breakable hitch lifecycle

`VehicleAssemblyDefinition` adds:
- `MaximumAttachErrorMeters` (already existed; world hitch separation);
- `MaximumAttachYawDegrees` (projected forward heading limit);
- `MaximumAttachRelativeSpeedMps` (velocity difference at hitch points);
- `MaximumManualCouplingBodySpeedMps` (absolute speed bound for BOTH
  manual attachment and manual detachment);
- `AlignHitchesOnManualAttach` (optional one-time small trailer Rigidbody
  correction before creating the joint);
- `BreakForceNewtons` and `BreakTorqueNewtonMeters`; 0 maps to
  `Mathf.Infinity` when constructing the live joint.

Versioned ScriptableObject defaults migrate older M1.3 assets to M1.4
when loading; newer explicitly unbreakable values remain unchanged.

`VehicleCoupling` records Detached, Attached, Broken. It is the owner of
`ConfigurableJoint`. Breaks are driven by **Unity physics** exceeding
Joint.breakForce or breakTorque; Unity notifies
`OnJointBreak(float)` and removes the joint. Torque break only applies
to angular axes with Locked/Limited constraints (yaw is Free here).
The controller does not apply a fake damage event or scripted detach.

`VehicleAssemblyRoot.TryAttach/Detach` are explicit commands.
Manual release is only allowed near standstill; physical overload
breaks can happen at ANY vehicle speed. Destroying a manually released
joint is deferred to end-of-frame in Play Mode; a same-frame attempt to
reconnect returns JointRemovalPending. Do NOT set Joint.connectedBody
to null because that anchors the joint to the WORLD.

Disconnection releases the basic tractor-to-trailer service/park brake
relay and clears last mirrored brake values. User's R-only recovery
remains assembly-aware while still connected, and individual-body-only
when uncoupled. No implicit auto-coupling or auto-rollover logic exists.
