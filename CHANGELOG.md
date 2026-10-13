# Changelog

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
