# KAVRA Vehicle Framework

**Modular, reusable Unity vehicle mechanics — from 3-wheel layouts to many-axle machines.**

**State (2026-10-14): M1 vehicle framework foundation, package 0.1.2. New M1.1 source awaiting Unity Editor verification.**
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
- Measured M0.5 brake issue: optional near-stop locked-wheel **impulse
  limiter**, avoiding unrealistically strong alternating Fx near zero speed.
  The limiter uses nominal wheel load shares and the **full Rigidbody time
  step**, leaving braking at normal speeds unchanged. Each wheel reports
  whether its braking force was limited.
- **M1 foundation:** optional `SimplePowertrainDefinition` asset with an
  engine RPM/torque curve, neutral/reverse/forward gear ratios, final drive
  and efficiency. `SimplePowertrainRuntime` distributes actual calculated
  wheel torque among driven AXLE groups and wheels. No fixed 4-wheel layout.
- Wizard creates a powertrain asset and an opt-in M1 dynamic chassis.
- A separate `Create M1 Powertrain Lab Scene` uses W/S, Q/E gearing, A/D,
  Space and the existing manual R recovery. M0 modes are preserved.
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
it creates kinematic preview (3A), dynamic M0 (4B) and, after adding an
optional Powertrain asset, powered M1 (4C) prototypes. For the **new**
standalone M1 demonstration, choose
`Tools > KAVRA > Create M1 Powertrain Lab Scene` and save the generated
`Assets/VehiclePhysicsLab/Scenes/M1_PowertrainLab.unity`. W accelerates,
S selects reverse only when slowed, Q/E down/upshifts, A/D steers, Space
brakes, Left Shift parks, R explicitly requests recovery.
Dynamic chassis needs a suitable ground Collider to support its wheels.

### Acceptance before merging

Do not mark tests passed without actual Unity Test Runner evidence.
Capture Console errors, the results of EditMode tests, a 4W Play Mode trial,
and at least 3W + 8W smoke checks. See
`docs/13_M0_FIRST_PHYSICS_SLICE.md`, `docs/17_M05_TELEMETRY_REVIEW_2026-10-14.md`,
and GitHub issues #1 and #6. Real user CSV evidence is available under
`docs/Telemetry/`; the brake fix still requires an AFTER comparison.

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
- New Powertrain field defaults null in pre-M1 vehicle assets; existing
  VehicleSimulationRoot.DriveSource defaults LegacyDirectWheelTorque, so old
  scenes do **not** switch propulsion or invalidate previous CSV captures.
- M1 SimplePowertrain uses one torque curve and a simple speed-coupled RPM
  estimate; per-axle equal distribution is NOT an open/LSD/locked
  differential, clutch or real measured automotive powertrain.
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

### Save an M1 test vehicle as a reusable prefab

Use `Tools > KAVRA > Vehicle Setup Wizard`, assign a VehicleDefinition
and create/assign a SimplePowertrainDefinition asset. Choose
`4D. Save M1 powered prefab (reusable)`. It saves the dynamic
single-chassis placeholder vehicle as an asset under your host project's
`Assets/` folder and retains definition/powertrain references. It does
**not** silently overwrite an existing prefab.

The prefab contains test cube body/wheel visuals with real KAVRA wheel
physics; bring your own ground Collider, input, camera and 3D meshes when
integrating into another game. Full imported-mesh analysis, production
collision construction and polished asset wizard are subsequent milestones.

## M1.1 — Configurable driven axles (issue #10)

This extension focuses on the **main framework**, not microscopic
tire-physics tuning. The original M0 modes and previous user CSV files
are still preserved.

- `AxleDefinition.DriveTorqueWeight` assigns relative engine output torque
  shares among powered axle groups. Zero/missing serialized values read as
  the legacy default 1; to disable power use wheel `CanDrive` flags.
- `AxleDifferentialMode.EqualTorque` is the original equal-torque split.
- `AxleDifferentialMode.SpeedBiasedApproximation` mildly favors the slower
  spinning driven wheel(s) on one axle, with adjustable maximum bias and gain.
  **It does not lock wheel speeds or model a real mechanical LSD.**
- The Vehicle Wizard now shows drive-enable switches and torque allocation
  settings for each axle when a powertrain is assigned.
- The M1 lab menu now offers a 3W demo, 4W car, 6x6 truck, 8x8 truck and
  20W single-chassis stress layout. Heavy presets get a separate, *example*
  diesel-like torque curve. No complete vehicle-model meshes are bundled.
- The compatibility constructor of `SimplePowertrainRuntime` retains equal
  weights and mode; core Unity package still has **no extra dependencies**.

See `docs/19_M1_AXLE_DRIVE_AND_DEMOS.md`. Existing powered M1 vehicles
keep their per-axle routing unless axle settings are changed explicitly.
Unity compilation and driving verification are still required.

## M1.2 — Dedicated gearbox state and removable wheel presentation

New `VehicleGearboxController` is a **per-vehicle** state machine. Manual
gear requests and optional automatic up/downshifts have an actual
transition interval (`SimplePowertrainDefinition.ShiftDurationSeconds`)
with torque cut while shifting. The current engaged gear, requested
target and shift progress are separate. Reversing against vehicle travel
above 0.75 m/s is rejected in the gearbox itself, independent of input.
No physical clutch or torque-converter is claimed.

On a powered test car, press **T** to toggle manual/automatic forward gear
selection; in manual mode **Q/E** down/upshift and **N** selects neutral
when no other direction key is pressed. W/S select forward/reverse at
low speed, Space brake, **R** manual recovery (no auto rollover control).

The separate `VehicleWheelVisualRig` allows real 3D wheel meshes to follow
three transforms: suspension travel, steering pivot, spin pivot. A basic
`VehicleSuspensionVisualLink` follows designated upper/lower shock points,
without adding physical links or Unity Rigidbody joints.

**New M1 scaffolds** get pivot rigs and simple visible shock rods by default.
**Saved M0 scenes** still use legacy single-cylinder visuals. To prepare
existing scene meshes, select a vehicle and choose
`Tools > KAVRA > Bind Selected Vehicle Wheel Visuals`, after assigning
each WheelAnchor.VisualTransform to its mesh. The menu skips anchors with an
existing rig and does not alter the physical contact mount. The shared
runtime assembly remains independent of URP/Input System/Blender.

See `docs/20_M12_GEARBOX_AND_VISUAL_BINDING.md`.
