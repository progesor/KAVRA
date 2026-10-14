# Changelog

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
