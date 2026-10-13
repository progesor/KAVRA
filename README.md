# KAVRA Vehicle Framework

**Modular, reusable Unity vehicle mechanics — from 3-wheel layouts to many-axle machines.**

**State (2026-10-13): M0 experimental physics source, package 0.0.5. Not yet verified in a Unity Editor.**
Repository: https://github.com/progesor/KAVRA

KAVRA is a Unity 6.3 LTS embedded UPM package (`Packages/com.progesor.kavra`)
plus a separate `Assets/VehiclePhysicsLab` sample host. Its physics assembly
depends on UnityEngine only: no forced URP, Input System, Cinemachine, Burst,
or paid vehicle asset dependency.

## What works in source (NOT yet Play Mode accepted)

- VehicleDefinition assets and layouts: 3W, 4W, 6x6, 8x8, 20W **single chassis**.
- Wheel/axle ID and geometry validation; Editor wizard with visual-only mode.
- New **optional** dynamic prototype using `Rigidbody.AddForceAtPosition`.
- NonAlloc single-ray wheel contact with own-chassis hit filtering.
- Equivalent spring / compression–rebound damper with nominal static sag.
- Experimental velocity-slip tire forces with combined friction ellipse.
- Rotational wheel inertia, direct wheel test torque and service/parking brakes.
- New: progressive suspension bump stops, CoM-based nominal wheel loads,
  paired anti-roll, configurable provisional chassis inertia, CoM gizmo.
- Selectable raycast/spherecast-with-ray-fallback contact probe and CSV lab telemetry.
- Optional `SurfaceZone` -> `SurfaceProfile`: per-wheel longitudinal/lateral grip
  and rolling resistance. Sample segmented track: asphalt / gravel / mud / ice.
- Manual **R** recovery (ONLY on key press, near stop, usable ground required);
  resets upright pose and wheel state without preventing natural rollovers.
- Opt-in total wheel-contact cast CPU timing (measured in the local Lab).
- Geometry-aware approximate Ackermann steering (parallel mode remains available).
- Optional `TireTuningProfile` on individual wheels: stiffness, friction, rolling
  resistance and inertia multipliers, with no-profile compatibility fallback.
- Bounded low-speed slip ratio and signed slip angle **diagnostics** for
  calibration; no claim that the underlying force model is Pacejka.
- Dedicated scripted calibration run (straight brake, steering step, coast-down)
  with a long asphalt surface and an extended per-wheel CSV.
- Inspector per-wheel telemetry and contact debug rays.
- Host keyboard adapter and chase camera; generated temporary laboratory scene.
- EditMode test sources: 3/4/6/8/20 layouts, spring statics, slip cap, contact filter.

**Not present:** calibrated tire model, full differential/transmission, accurate
off-road soil deformation, independent suspension linkage kinematics, trailers,
ABS/ESC, advanced damage. This is an engineering prototype, not a production
vehicle or simulation-accuracy certification.

## Try M0 locally

1. Open the already-created Unity project in its current **6000.3.18f1** editor
   (or another installed Unity 6.3 LTS patch).
2. Pull the development branch `feat/m0-wheel-suspension-prototype` or check out its PR.
3. Wait for Unity's import/compile. In `Window > General > Test Runner`, run
   `KavraPhysicsEditModeTests` (EditMode).
4. Choose `Tools > KAVRA > Create M0 Physics Lab Scene`. This creates a new,
   **unsaved** scene with a surface test track, a vehicle, keyboard input, ramp
   and follow camera. Surface preset assets are saved under
   `Assets/VehiclePhysicsLab/Surfaces/` without overwriting existing assets.
5. Save as `Assets/VehiclePhysicsLab/Scenes/M0_DynamicLab.unity`.
6. Press Play. Drive with W/S (test wheel torque/reverse), A/D (steer),
   Space (service brake), Left Shift (parking brake), or arrow keys.
   **R = manual recovery** only while moving under ~1.75 m/s; no auto-anti-flip.
7. Drive forward through the colored asphalt, gravel, mud and ice sections.
   The experimental CSV defaults to **90 seconds** and is saved to
   `Application.persistentDataPath` (location printed in Console).
8. Observe surface and grip in Wheel Telemetry, and cast CPU timing in HUD.
   Compare ContactMode: Raycast vs SphereCastWithRayFallback in Inspector.
9. For repeatable tests choose `Tools > KAVRA > Create M0 Calibration Run`.
   In the generated scene's `VehicleLabManeuverRunner` choose
   `StraightBrake`, `SteeringStep` or `CoastDown`. The calibration scene
   sets `SteeringMode = ApproximateAckermann` and manual driving keys are off;
   only explicit R recovery remains available. Generated CSV includes actual
   steering angle and slip metrics.
10. Select the root `KAVRA_Passenger Car 4W` GameObject, expand
   `VehicleSimulationRoot > Wheel Telemetry`; measure static Fz and stability.

The original `Tools > KAVRA > Vehicle Setup Wizard` remains available:
`3A` produces kinematic scaffolds, `3B` dynamic experimental ones.
Dynamic chassis needs a suitable ground Collider to support its wheels.

### Acceptance before merging

Do not mark tests passed without actual Unity Test Runner evidence.
Capture Console errors, the results of EditMode tests, a 4W Play Mode trial,
and at least 3W + 8W smoke checks. See
`docs/13_M0_FIRST_PHYSICS_SLICE.md` and GitHub issue #1.

## Physics assumptions

- Coordinate convention: +X right, +Y up, +Z forward; SI units.
- Wheel anchor is the suspension mount: at full compression the wheel center
  is at the anchor, at full droop it is `travel` below the anchor.
- The Builder initializes the chassis around half-travel sag on level ground.
- Each wheel is a sensor; its visual cylinder has no friction/collision physics.
- The temporary cube is a real chassis collider, **not a finished vehicle shape**.
- Per-wheel spring rate is estimated from CoM projection and wheel support
  footprint; does not yet represent measured axle loads, air springs,
  or load-sharing bogies.
- Optional per-wheel TireProfile assets multiply baseline stiffness/grip/
  rolling coefficient/inertia; without one, legacy global parameters remain.
- Slip ratio/angle telemetry uses regularized speed and is a measured
  diagnostic, not the tire-force law or a substitute for road testing.
- Softness/Wetness are currently descriptive only. "Mud" reduces tire grip
  and increases rolling resistance but does NOT deform the ground mesh.
- Static slope-hold, rough-rock 3D contact and drivetrain/differential coupling
  are future work. Automatic rollover prevention is intentionally absent.

## Documentation

Start with `docs/00_SCOPE_AND_GOALS.md`,
`docs/04_PHYSICS_MODELS.md`, `docs/08_ROADMAP_AND_MILESTONES.md` and
`docs/13_M0_FIRST_PHYSICS_SLICE.md`. The entire architecture, MudRunner
notes, terrain interaction design and planned Wizard capabilities are in
`docs/`. Package API notes: `Packages/com.progesor.kavra/Documentation~/PACKAGE_CONTRACT.md`.

## License

No license has been selected. Publishing on GitHub does not grant reuse rights.
