# Changelog

## Unreleased — P2.0 imported model visual binding (2026-10-19)

- Added external FBX/model GameObject visual binding profile, validator,
  dedicated Editor Wizard (also linked from Vehicle Setup Wizard), and
  reusable prefab builder.
- Mesh/material references are reused; imported body and separate wheel
  meshes move into KAVRA's existing 3-pivot visual rig without copying
  source physics/colliders/scripts or touching input, drivetrain, hitch
  or manual R recovery.
- Added 4W/6W/20W synthetic model regression, prefab save/reimport,
  invalid source node mapping and scale-mismatch checks.
- FBX first, GLB only once host imports it to a prefab via optional
  importer; no core UPM dependency. Actual Unity P2 acceptance pending.
- User confirmed resolved Undo/Test Runner native crash.


## Unreleased — Unity native Test Runner Undo rollback mitigation (2026-10-19)

- User reports all EditMode tests passed on a later local rerun, but
  native Unity Editor crash occurred **after** results were saved.
- Analyzed supplied compressed Unity crash report: Test Runner calls
  `Undo.RevertAllDownToGroup`, native `Dereferencing NULL ImmediatePtr!`.
- Editor scaffold, wheel visual, two-body and multi-trailer builders
  now accept additive `recordUndo = true` argument.
- Temporary NUnit test objects and prefab export staging explicitly
  build with `recordUndo = false`, including nested pivots/hitches;
  regular Editor interactive Undo remains available by default.
- Source-only CI guard added. Native crash is not claimed fixed
  until an actual Unity Test Runner rerun exits cleanly. No UPM
  Runtime physics or user scenes/telemetry modified.


## Unreleased — 2026-10-19 Unity P0 test feedback (acceptance pending)

- Received actual Unity 6.3 EditMode evidence: 111/117 PASS, 6 FAIL.
  The P1 prefab roundtrip suite passed all four cases.
- Fixed static collider exclusion when `ownBody` is null in wheel
  contact/raycast queries, with a new regression case.
- Reworked two EditMode joint-break callback tests to avoid native
  `SendMessage` assertions; real solver breaks still need Play Mode.
- Corrected M1.3 EditMode recovery assertions to check authoritative
  `Rigidbody.rotation` instead of one-tick-stale `Transform.rotation`.
- Removed incompatible `-quit` when running `-runTests` in Windows
  acceptance script; added failed-case report and clearer log locations.
- **Source/CI only after fixes; fresh Unity NUnit run and crash-log
  diagnostics still pending.** Saved scenes/prefabs/CSV unchanged.


## Unreleased — P1 definition validation and prefab contract (2026-10-18)

- Strictly validate numeric finiteness of chassis, wheel mounts/geometry
  and optional engine/gearbox data; reject whitespace IDs and unknown
  axle/wheel enum values, without rewriting original ScriptableObjects.
- Added NUnit regression sources for corrupted configs and isolated
  prefab save/reimport/load for 4W, 20W, two-body and multi-trailer.
- Added Windows Unity 6000.3.18f1 EditMode acceptance runner; temp logs
  and XML outside the repository. **Actual Unity acceptance still pending.**

## Unreleased — P1 common assembly control API (source-only)

- Both existing two-body and ordered chain roots now implement the
  backward-compatible `IVehicleAssemblyController` API.
- PhysicsLab keyboard H handler now consumes the common controller while
  preserving existing manual hitch UI text and controls.
- Added EditMode source regressions for zero-based links, read-only
  attachment preflight and explicit H-only attach/detach behavior.
- Repaired outdated 0.0.1 static bootstrap expectation; added
  asmdef/MonoScript GUID guards and source-only CI workflow.
- No PhysicsLab scenes, user CSV, recovery behavior or real solver
  modifications. Unity compile and Play Mode remain unverified.


## 0.1.6 assembly hotfix — 2026-10-17

- Fixed CS0246 from KavraHitchLifecycleEditModeTests trying to reference
  VehicleLabHitchGuide in the predefined Assembly-CSharp assembly.
- Moved VehicleLabHitchGuide.cs into an isolated auto-referenced
  Progesor.Kavra.Lab.HitchVisuals assembly definition that references
  the KAVRA UPM runtime, then added this assembly to EditModeTests.asmdef.
- Preserved the original MonoScript .meta GUID during the move so
  serialized scenes retain their script component references.
- No added UPM dependencies, Input System assembly requirements,
  physics solver modifications or texture reimports.
- Static assembly-reference graph and source/file GUID checks completed.
  Actual Unity 6000.3.x recompile and EditMode Test Runner still pending.


## 0.1.6 hotfix — 2026-10-17 (HitchGuide scene creation NRE)

- Fixed a real Unity NullReferenceException reported during
  `Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene`.
- `VehicleLabHitchGuide.Setup()` now creates its MaterialPropertyBlock
  BEFORE coloring the first tractor/trailer hitch marker, even when
  Unity Editor invokes Setup before MonoBehaviour.Awake.
- `TintRenderer` also lazily initializes the block for domain-reload
  or alternate setup paths.
- Added a reproducer EditMode test forcibly resetting the color block
  before Setup; verifies marker and line/capture-ring initialization.
- Does not change hitch physics, road colors, CSV data or existing
  saved Unity scenes. **Local Unity compiler/test verification pending.**


## 0.1.6 — 2026-10-17 (M1.5.1 hitch usability / PhysicsLab visuals)

- Made manual hitch capture easier: initial 0.15m -> 0.50m,
  yaw 35deg -> 55deg; safe body/hitch approach velocities remain
  required and all coupling is still explicitly triggered with H.
- Versioned hitch settings migration updates only old default
  tolerances, preserving user-customized link assets.
- Added non-mutating VehicleCoupling.AssessAttach and runtime
  VehicleHitchAssessment measurements for consistent UI diagnostics.
- Added generated checker road material/texture, metric roadside/
  centerline references, distinct matte vehicle colors and dark tires.
- Added selected hitch-colored endpoints and connection guide lines,
  circular capture radius and READY/TOO FAR/BAD ANGLE/TOO FAST readout.
- Added optional subtle speed-dependent chase-camera FOV to articulated
  PhysicsLab generators only. Prior saved M0/M1 scenes unchanged.
- Added 4 new EditMode hitch capture/migration regression sources.
- Generated assets live under Assets/VehiclePhysicsLab/Generated and
  do not silently overwrite existing native .asset/.mat files.
- Real Unity Editor import/Play Mode verification pending.


## 0.1.5 — 2026-10-17 (M1.5 multiple trailers / chain assemblies)

- Added ordered `VehicleAssemblyChainDefinition` and validation
  requiring each link's Tractor to be the previous Trailer.
- Added `VehicleAssemblyChainRoot` with 1..16 independent breakable
  ConfigurableJoint links, user-selected attach/detach, lead-connected
  vehicle count and brake relay that stops at a broken link.
- Added per-member markers and R-only `VehicleAssemblyChainRecovery`.
  Recover the connected lead prefix together while detached trailers
  are left in place; preflight ground/support/clearance before movement.
- Added Editor chain builder, Wizard, N-body prefab save and host
  M1.5 6x6 tractor + two 4W trailers (14W/3 Rigidbody) lab.
- Added J/L link selector, H manual hitch toggle and K real PhysX
  impulse test on selected link; separate HUD.
- Added nine EditMode source tests for topology, 3-body build,
  brake propagation/breaking, selection and safe recovery.
- Does NOT change M1.4 single-trailer logic, M0 scenes, existing telemetry,
  powertrain/suspension or add auto-rollover help.
- Unity import/compile, Test Runner and dynamic hitch chain tests pending.


## 0.1.4 — 2026-10-17 (manual trailer hitch + physical overload break)

- Extended VehicleAssemblyDefinition with safe approach/yaw/body speed
  requirements, optional short manual hitch snap, and separate solver
  force/torque break thresholds (0=unbreakable).
- Added old asset migration guard so v0.1.3 assembly assets inherit
  workable defaults without invalidating intentionally unbreakable
  newer assets.
- VehicleCoupling now tracks Detached/Attached/Broken, rejects unsafe
  manual hitch attempts, assigns Unity Joint.breakForce/breakTorque,
  and handles engine OnJointBreak notification (no scripted break).
- VehicleAssemblyRoot offers TryAttach/Detach; manual detach near stop,
  and trailer service/park command resets after detachment or physical break.
- H hitch key and K lab-only *real Rigidbody impulse* stress test,
  live hitch/break telemetry in the articulated sample overlay.
- Added 9 EditMode tests plus updated M1.3 stale-brake regression.
- No extra Unity package dependencies or automatic rollover prevention;
  source checks passed, real Unity joint-break Play Mode acceptance pending.


## 0.1.3 hotfix — 2026-10-17

- Fixed CS1061 in VehicleCoupling: Unity's ConfigurableJoint does not
  expose Behaviour.enabled; attachment state checks a live connected body.
- Detach now destroys the joint component rather than setting connectedBody
  to null, which would unintentionally constrain the trailer to WORLD.
- Strengthened EditMode coverage for detach and reattach.
- Unity Editor compilation and runtime verification still pending.


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
