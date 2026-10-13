# KAVRA — Handover 2026-10-13: M0 experimental wheel dynamics

## Repo / branches

- Repository: https://github.com/progesor/KAVRA
- Unity project version: 6000.3.18f1; embedded package `com.progesor.kavra`.
- Working branch: `feat/m0-wheel-suspension-prototype`; PR to main.
- Issue: https://github.com/progesor/KAVRA/issues/1.

## Implemented as source in M0 v0.0.2

- RaycastNonAlloc ground probe excluding own chassis.
- Simple equivalent spring + bump/rebound damper.
- Semi-implicit angular wheel/tire slip, traction ellipse, direct wheel torque,
  service and parking brake.
- VehicleSimulationRoot computes wheel forces and applies them through Rigidbody.
- Per-wheel telemetry + optional debug contact rays.
- Wizard can create dynamic prototypes while preserving kinematic previews.
- Assets/VehiclePhysicsLab generates a test scene and host keyboard/camera.
- EditMode test scripts for 3/4/6/8/20 layouts and solver sanity.

## Important: still NOT verified in Unity

This code was authored and statically reviewed via GitHub. No Unity Editor
or Play Mode runtime was available for a real compilation or physics run.
Do not report tests as green until a local Editor test has passed.

## Next acceptance path

1. Pull/checkout M0 branch (or merge PR after acceptance).
2. Open Unity and inspect Console import + compile errors.
3. Run all `KavraPhysicsEditModeTests` in Test Runner.
4. Generate the M0 lab from Tools menu, save the scene, press Play.
5. Record rest Fz sum, frame stability, 10-second driving/braking test.
6. Smoke-check 3W and 8W Wizard dynamic chassis.
7. Send errors/logs or commit confirmed scene and measured data to repo.
8. Fix findings; only then merge PR and start richer tire/contact model work.

Known limits/risks: single contact ray, uncalibrated velocity-slip tire,
automatic equal wheel spring-rate guess, non-deterministic Unity simulation,
no actual drivetrain or soil contact deformation, test keyboard is a sample
in host project and not part of reusable package.

See `docs/13_M0_FIRST_PHYSICS_SLICE.md` for API and numerical details.


## Update: M0 0.0.3 — ride-height / roll / contact reliability

User drove the prototype and reported moderate body banking, plus a wheel
visually entering ground during some turns. New source on the same PR #2
addresses likely causes with CoM-weighted springs, end-stop, anti-roll pair,
test inertia approximation, CoM gizmo, sphere-vs-ray contact mode and
bounded PhysicsLab CSV capture. Tracked at issue #3.

Re-run the lab from the Tools menu to instantiate a fresh component with
new defaults. Check EditMode tests, 20-second CSV, bottomout_m and tilt_deg.
Unity compilation and Play Mode **remain pending independent verification**.


## Update 2026-10-14: M0 v0.0.4 surface / R recovery

Issue: https://github.com/progesor/KAVRA/issues/4

Source integrated: `SurfaceZone` collider binding, `SurfaceResponse`
per-wheel friction/rolling, lab asphalt/gravel/mud/ice equal-height strips,
`VehicleManualRecovery.TryRecover`, host-only **R** key handling,
opt-in wheel-contact cast CPU duration, 90-second diagnostic CSV and
EditMode tests.

User explicitly asked for **no artificial automatic rollover prevention**.
The vehicle can roll naturally. Pressing R while nearly stopped attempts a
one-off repositioning to an upright yaw at its current horizontal location;
if any wheel has no ground, speed is high, or space is obstructed, it refuses.

Re-generate the M0 Physics Lab via Tools menu to see surface strips and new
component defaults; previous saved scene objects are not automatically
replaced. First open Unity Console and run EditMode tests, then Play Mode
with R behavior, surface transition grip and cast cost CSV. Still no actual
Unity runtime tests performed by assistant.


## Update 2026-10-14: M0.5 v0.0.5 steering & tire calibration tools

Issue #5 / Draft PR #2. Optional `TireTuningProfile` field on
WheelDefinition (null keeps existing assets working). `SteeringMode`
offers Parallel and ApproximateAckermann with no automatic roll/yaw correction.
Precomputed steering geometry used at FixedUpdate without per-wheel layouts
scanning. Bounded `TireSlipDiagnostics` feeds Inspector/HUD/CSV; still not a
calibrated tire force model.

New `Tools > KAVRA > Create M0 Calibration Run`: a flat asphalt runway,
host `VehicleLabManeuverRunner` with repeatable StraightBrake/SteeringStep/
CoastDown schedules, keyboard driving disabled but explicit R permitted.
Extended 28s CSV includes slip, actual steering angles, tire profile/stiffness
and input/maneuver labels. Interactive surface test remains separate.

Run EditMode tests and the three scripted experiments locally, compare CSV,
inspect 3W/4W/8W. Unity was not available during source authoring; avoid
merging the Draft PR until actual tests pass.
