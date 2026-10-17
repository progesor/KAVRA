# Changelog

## 0.1.3 — 2026-10-17 (M1.3 actual multi-body tractor/trailer)

- Added VehicleAssemblyDefinition and validation for **two independent**
  VehicleDefinitions and Rigidbody vehicle simulations.
- Added VehicleCoupling using real ConfigurableJoint with coincident
  anchors, locked XYZ, yaw free, limited pitch/roll. No hidden yaw or
  anti-rollover steering forces.
- Added VehicleAssemblyRoot + VehicleAssemblyMember: independent
  tractor/trailer physics with a small commanded trailer-brake relay.
- Added assembly-aware explicit R recovery: checks both bodies, road and
  clearance before repositioning either body; zero velocities, preserve
  coincident hitch, **no automatic recovery**.
- Added VehicleAssemblyMath (pose/hitch checks), Validator and Editor
  builder, dedicated assembly Wizard and two-body prefab save.
- Added M1.3 host PhysicsLab generating a 6x6 tractor + passive four-wheel
  trailer, joint overlay, follow camera and existing M1 controls.
- Added eight EditMode regression test sources and docs/21.
- Existing M0 scenes, M1 demos, physics data and package dependencies
  preserved. Real Unity compile and Play Mode acceptance **pending**.


## 0.1.2 — 2026-10-17 (gearbox state + transform-based wheel rigs)

- Added allocation-free VehicleGearboxController with manual/automatic
  forward shifting, actual engaged vs requested gear, shift-time torque
  interruption and safe moving direction-change rejection.
- Added gearbox tuning fields to optional SimplePowertrainDefinition,
  preserved legacy M0 direct-torque default.
- Added host T auto/manual toggle, Q/E manual up/down, N neutral, W/S
  low-speed direction control, R manual recovery; gearbox HUD/CSV metrics.
- Added VehicleWheelVisualRig (travel/steer/spin pivot hierarchy) and
  VehicleSuspensionVisualLink (kinematic shock rod binding).
- New M1 generated scaffolds/prefabs get structured placeholder wheel rigs.
  Existing M0 wheel visuals remain unchanged. Editor menu can bind selected
  existing vehicle wheel meshes without touching wheel contact physics.
- Added gearbox/visual EditMode regression source, updated package contract
  and technical instructions in docs/20.
- No automatic rollover assistance, physical clutch, deformable tire/ground
  or real external 3D asset included.
- **Unity 6.3.x real compile and Play Mode tests pending.**


## 0.1.1 — 2026-10-14 (M1.1 axle drive routing and demo breadth)

- Added optional per-axle drive torque weights and two explicit allocation
  modes: EqualTorque (legacy), SpeedBiasedApproximation (LSD-like **command
  routing**, not wheel-speed locking or traction-limited mechanical diff).
- Updated SimplePowertrainRuntime to accept AxleDefinition configs while
  preserving the original two-argument constructor and older assets.
- Added per-axle drive flags and differential/allocation controls to Wizard.
- Added standalone 3W, 6x6, 8x8 and 20W M1 lab generation menus, including
  an approximate heavy diesel torque preset for truck demos.
- Added six EditMode regression tests for weighted axles, speed-bias torque,
  reverse, legacy weight compatibility, CanDrive authority and validation.
- New docs/19; no automatic anti-rollover and no real car 3D assets.
- Source/static checks only; Unity Editor/Play Mode acceptance pending.


## 0.1.0 — 2026-10-14 (M1 modular powertrain foundation — unverified in Unity)

- Added separate `SimplePowertrainDefinition` with torque-by-RPM curve,
  forward/reverse/neutral gears, final drive and efficiency.
- Added `SimplePowertrainRuntime` with speed-coupled engine RPM, basic
  redline torque cutoff and axle-group/wheel torque distribution for
  variable wheel counts (3/4/6/8/20).
- `VehicleSimulationRoot` now selects between legacy direct-torque and
  opt-in simple powertrain. Existing scenes and unassigned definitions
  preserve their original M0 behavior.
- Wizard now creates/assigns the asset and builds a dynamic M1 chassis.
- Added a host-only M1 Powertrain Lab menu, keyboard forward/reverse
  (direction change only when slow), Q/E gear shifting and basic engine
  RPM/gear/wheel torque HUD+CSV.
- Added EditMode regression sources for gear direction, neutral, redline,
  per-axle distribution, arbitrary wheels and M0 compatibility.
- Full clutch/gear engagement dynamics, differential types, engine braking,
  hybrid power units and vehicle certification are NOT claimed.
- **Unity Editor import/compilation and driving tests still pending.**

## 0.0.6 — 2026-10-14 (first real telemetry review and brake jitter fix)

- Reviewed four user-recorded PhysicsLab CSV sessions in `docs/Telemetry/`:
  two StraightBrake, one SteeringStep, one CoastDown.
- Findings documented in `docs/17_M05_TELEMETRY_REVIEW_2026-10-14.md`.
  Identified strong +/-14kN longitudinal-force oscillation near zero speed
  with brakes engaged; repeatable in both StraightBrake sessions.
- Added opt-in/Inspector-enabled, default-on M0.5 low-speed brake impulse
  limit, using nominal per-wheel chassis effective mass and **BODY**
  FixedUpdate time, not the wheel rotor substep interval.
- Added per-wheel BrakeImpulseLimited telemetry and corresponding CSV fields.
- Added three EditMode regression tests (near stop, normal-speed unchanged,
  no-brake/airborne unchanged). **Not run in Unity Editor yet.**
- No auto-anti-flip, artificial roll lock, new powertrain or soil deformation.
- Real Unity A/B acceptance pending; previous user files preserved.


## 0.0.5 — 2026-10-14 (M0.5 steering/slip calibration instrumentation)

- Added optional approximate Ackermann steering computed from the positions
  of steerable and nonsteerable wheels, including rear-steer and multi-axle
  layouts. Preserved legacy `Parallel` mode for existing assets.
- Added optional `TireTuningProfile` per wheel with no-profile fallback.
- Added bounded low-speed slip ratio and signed slip angle diagnostics;
  underlying M0 velocity-slip force solver remains provisional.
- Extended telemetry, HUD and CSV with steering angles, slip metrics, tire
  multipliers, control inputs and maneuver labels.
- Added a dedicated flat asphalt calibration lab + repeatable scripted
  StraightBrake / SteeringStep / CoastDown input runs.
- Added EditMode regression source for arbitrary wheel counts, rear-steer,
  low-speed diagnostic validity and tire tuning composition.
- No automatic rollover prevention. R recovery still requires a keypress.
- Unity compilation, test runner and Play Mode verification **pending**.


## 0.0.4 — 2026-10-14 (surface grip + user-triggered recovery)

- Wired SurfaceProfile multipliers to longitudinal/lateral tire grip and
  rolling resistance at each wheel's active Collider contact.
- Added optional SurfaceZone component and fallback to original default grip
  when no surface profile exists.
- Lab builder now creates colored asphalt, gravel, mud and ice sections with
  reusable ScriptableObject assets; no soil deformation physics.
- Added R-key **manual** recovery only. No automatic rollover prevention:
  recovery requires low speed and valid support, and resets wheel/rigidbody
  motion on explicit user request.
- Extended telemetry: surface identity, grip coefficients, rolling resistance,
  and optional total contact query duration (microseconds per physics step).
- Updated bounded Lab CSV default from 20s to 90s and added EditMode tests.
- Unity Editor/Play Mode validation is still pending.


## 0.0.3 — 2026-10-13 (ride-height/CoM/contact experiments)

- Added progressive bump-stop force and exposed bottom-out diagnostics.
- Nominal spring weights now derive from center of mass and wheel support layout
  rather than assuming all wheels statically carry exactly mass / wheel count.
- Added paired per-axle anti-roll transfer with no negative ground support.
- Added configurable inertia estimate to counter unrealistically thin lab
  chassis primitive, and magenta CoM gizmo.
- Added experimental spherecast contact option with raycast fallback and
  elevated query origin for deeply compressed suspension.
- Added lab telemetry HUD and bounded 20-second CSV capture.
- Added new EditMode regression cases. Actual Unity Editor test and
  Play Mode acceptance are still pending. User noted turning bank + wheel
  ground penetration; work tracked at issue #3.


## 0.0.2 — 2026-10-13 (experimental source; awaiting Unity validation)

- Added first M0 custom raycast contact and spring/damper force integration.
- Added provisional semi-implicit velocity-slip wheel dynamics and combined
  friction limit, direct test wheel torque, service and parking brakes.
- Added optional dynamic Wizard chassis while preserving visual-only mode.
- Added generator for the host Physics Lab with Input System keyboard adapter.
- Added EditMode test sources and per-wheel telemetry.
- Unity Editor compilation and Play Mode acceptance remain **unverified**.

## 0.0.1 — 2026-10-13

- Unity 6.3 LTS UPM scaffold, 3/4/6/8/20W layouts, basic Wizard and docs.
