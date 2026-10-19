# Changelog

## Unreleased — P2.0 imported model binder (source-only)

- New visual mapping ScriptableObject and read-only vehicle visual marker.
- Editor-only mesh binding validator, dedicated Wizard and prefab
  generator; no new physics body, extra collider or hard importer package.
- Source meshes/materials referenced instead of duplicated. Existing
  vehicle definitions, joints and user data unmodified.
- Seven-suite Unity EditMode validation and real FBX visual acceptance pending.


## Unreleased — 19 October 2026 (Editor-only Undo lifecycle fix)

- Editor vehicle/assembly/visual builders now support optional
  Undo-free temporary generation. Normal scene operations preserve
  previous Undo/Redo behavior by default.
- Prefab staging uses ordinary temporary objects with no Undo
  registration. Test cleanup no longer destroys Undo-registered
  temporary hierarchies made by KAVRA.
- No changes in Runtime C#, serialized configuration, GUIDs,
  H/K coupling, manual R recovery or package dependencies.
- Unity 6.3 Editor native rollback crash **requires rerun**.


## Unreleased — 2026-10-19 wheel contact filter correction

- Prevent treating static ground colliders as own-vehicle colliders
  when `WheelContactProbe` receives a null `ownBody`.
- Other physics behavior, braking, manual-only recovery, hitch solver,
  serialized assets and UPM dependencies remain unchanged.
- Source regression added in optional PhysicsLab host. First Unity
  EditMode feedback (111/117 passed) prompted this change; **new
  Unity test acceptance is not yet available**.


## Unreleased — shared host-facing assembly API

- Additive `IVehicleAssemblyController` implemented by M1.4 and M1.5
  roots; consistent link queries and explicit manual control.
- No serialized field changes, assembly physics changes or new package
  dependency. Unity compiler and Test Runner acceptance pending.


## Unreleased — 2026-10-18 P1 guardrails (source only)

- Non-finite configuration parameters and malformed IDs are now rejected
  by validation before prefab construction. Legacy valid assets stay valid.
- Host EditMode roundtrip tests cover independent rigidbodies and joint
  references after prefab save/reimport/load. Unity results pending.
- Package version remains v0.1.6 until acceptance.

## [0.1.6] — 2026-10-17

- More forgiving manual assembly hitch defaults (0.5m / 55deg).
- Versioned migration for older default tolerance values.
- Shared read-only hitch assessment result and measurements, consistent
  with active TryAttach preflight; no automatic attachment or
  new physics corrections.
- Editor-host PhysicsLab visuals (checker surface/colored chassis and
  hitch guide) added outside UPM.
- Added 4 EditMode capture/migration regression tests.
- Source/static inspections completed; Unity actual tests pending.


## [0.1.5] — 2026-10-17

- Added ordered chain definition/validator, N-body runtime/Editor builder
  and per-link coupling selection with existing breakable hitch components.
- Added braking relay through lead-connected prefix and manual recovery
  of that prefix without moving any detached suffix vehicle.
- Added 14-wheel, 3-Rigidbody multi-trailer PhysicsLab and source tests.
- Preserved all previous scenes and UPM dependencies.
- Unity Editor compilation/Play Mode still needs user verification.


## [0.1.4] — 2026-10-17

- Added position/yaw/speed-limited manual hitch attachment and detachment.
- Added physical joint breakForce/breakTorque and OnJointBreak tracking.
- Added explicit assembly APIs, brake release on disconnect and legacy
  asset-migration defaults; zero thresholds disable mechanical breaking.
- Host H reconnect/unhitch, K physical break-load demo, status HUD.
- Added 9 hitch lifecycle EditMode tests; Play Mode verification pending.


## [0.1.3] — 2026-10-17

- Runtime VehicleAssemblyDefinition/Root/Member, VehicleCoupling,
  assembly math, validator and explicit two-body manual recovery.
- Separate tractor and trailer Rigidbody simulations; ConfigurableJoint
  yaw articulation with restricted pitch/roll and coincident anchors.
- Editor assembly builder, reusable two-body prefab Wizard, trailer
  4W + truck-powertrain template and independent host demonstration.
- Added basic trailer service/parking brake sharing, no brake delay/
  pneumatics or powered trailer shaft model.
- Eight EditMode test sources added. Unity Play Mode not yet verified.


## [0.1.2] — 2026-10-17

- Added gear shift state, torque cut, optional automatic thresholds and
  forward/reverse direction-change safety (not a real clutch).
- Added independent wheel visual pivot bindings and a visual shock link.
- Editor can bind selected wheel meshes, and newly generated M1 scaffolds
  use the new rig while older M0 scenes stay compatible.
- Updated host HUD/CSV and EditMode test sources; Unity tests pending.


## [0.1.1] — 2026-10-14

- Added axle-relative drive torque weighting and selectable EqualTorque /
  SpeedBiasedApproximation torque-command routing per driven axle.
- Extended SimplePowertrainRuntime constructor with AxleDefinition list;
  older two-parameter constructor remains supported.
- Wizard can toggle driven axles and tune axle torque/differential settings.
- Added 3W, 6x6, 8x8 and 20W host powertrain lab scene options.
- Added six EditMode test sources and documented approximation limits.
- No Unity Editor test result yet.


## [0.1.0] — 2026-10-14 — M1 core prototype

- Added optional powertrain ScriptableObject, gear ratios, torque curve,
  engine RPM state, speed/redline-limited torque distribution.
- Introduced data-driven axle grouping for driven wheel torque and neutral,
  reverse and forward gears, independent of wheel count.
- Legacy direct torque remains the default when existing scenes load.
- Wizard and dedicated host M1 lab provide asset/scene generation and input.
- Wizard 4D saves an opt-in dynamic M1 prototype prefab, refusing to overwrite an existing asset.
- Added M1 EditMode test cases and extended telemetry fields.
- Source code was statically checked; no Unity Editor test was performed
  during authorship.

## [0.0.6] — 2026-10-14

- Real user CSV analysis showed recurring +/-14 kN braking force chatter
  around 0.28 km/h; documented in docs/17.
- Added experimental near-stop braking force impulse limit to the existing
  velocity-slip solver, using chassis FixedUpdate delta and static wheel mass
  shares (optional fields for backward compatibility).
- New per-wheel brake-limiter telemetry + host CSV columns.
- Added EditMode regression tests. Actual Unity compile/Play tests still
  pending before PR acceptance.


## [0.0.5] — 2026-10-14

- SteeringGeometrySolver with parallel and approximate Ackermann per-wheel
  steering, including rear-steered and variable-axle support.
- TireTuningProfile optional per-wheel multipliers and TireTuningResolver.
- TireSlipDiagnostics returns bounded slip ratio/slip angle as **telemetry**;
  velocity-slip tire forces are unchanged.
- Host-only calibration scene and scripted steering/brake/coast input schedules.
- EditMode tests, extended CSV telemetry. Unity Play Mode not yet verified.


## [0.0.4] — 2026-10-14

- Added per-contact `SurfaceZone`, optional `SurfaceProfile` physics
  multipliers and surface/grip telemetry.
- Added explicit `VehicleManualRecovery` with ground/speed/clearance checks.
  Recovery has no automatic triggering.
- Added opt-in wheel-contact cast query cost diagnostics; sample host includes
  surface test lanes, 90-second CSV and R keyboard adapter.
- Unity Editor verification still pending.


## [0.0.3] — 2026-10-13

- Introduced `VehicleStaticLoadEstimator` for footprint/CoM-based
  nominal spring weights.
- Added `AntiRollSolver`, progressive suspension bump-stop and
  wheel-bottom-out telemetry.
- Added optional spherecast contact with raycast fallback; preserved old
  `WheelContactProbe.TryRaycast` for backward compatibility.
- Added approximate chassis inertia estimate and center-of-mass gizmo.
- Host-only lab now exports experimental telemetry CSV; Editor/Play Mode
  verification is not yet available.


## [0.0.2] — 2026-10-13

- Experimental raycast road contact, vertical spring/damper solver and
  semi-implicit wheel angular/tire slip forces.
- Added `VehicleSimulationRoot`, wheel telemetry and debug contacts.
- Added opt-in dynamic vehicle creation to existing Editor Wizard.
- Source changes not yet compiled or verified in Unity Editor.

## [0.0.1] — 2026-10-13

- Package layout, VehicleDefinition, validation, wheel anchors, non-driving Wizard.
