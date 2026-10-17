# KAVRA Vehicle Framework

**Modular, reusable Unity vehicle mechanics — from 3-wheel layouts to many-axle machines.**

**State (2026-10-14): M1 vehicle framework foundation, package 0.1.6. New M1.1 source awaiting Unity Editor verification.**
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

## M1.3 — Actual tractor/trailer assemblies (two Unity Rigidbodies)

The first *multi-body* foundation is an optional
`VehicleAssemblyDefinition` referencing **two different**
`VehicleDefinition` assets. Each remains a complete independent
rigid chassis with its own ray/sphere wheel contact, tire response,
suspension and wheel visual rigs. A `VehicleCoupling` puts a genuine
`ConfigurableJoint` on the trailer connecting it to the tractor.

- Three linear axes LOCKED at a shared local hitch point.
- Relative yaw around the joint's Y axis FREE.
- Pitch and roll LIMITED (configurable in the assembly asset).
- Joint projection OFF by default; it can violate physics conservation.
- Basic service/park-brake commands shared from tractor to trailer.
- **Manual R** is assembly-aware: when nearly stopped, checks both
  vehicles' ground and free space, then repositions them TOGETHER with
  hitch points coincident. It is never an automatic rollover fix.

For a working-source sample use
`Tools > KAVRA > Create M1.3 Tractor Trailer Lab Scene`. This makes
a 6x6 powered tractor and separate passive four-wheel trailer, with
**two Rigidbody components and ten wheel contacts**. Save the generated
scene under `Assets/VehiclePhysicsLab/Scenes/M13_TractorTrailer.unity`.

`Tools > KAVRA > Vehicle Assembly Wizard` supports arbitrary pairs of
existing VehicleDefinition assets, editable local hitch anchors and
saving a two-Rigidbody reusable Unity prefab. The prefab uses the same
existing primitive placeholder models until actual vehicle meshes are
introduced in separate backlog issue #9.

Technical reference: `docs/21_M13_MULTIBODY_ASSEMBLY.md`.
All M0 scenes, M1 single-body presets and user's prior CSV data remain
unchanged. This feature is experimental until Unity Editor compile and
actual connected driving tests pass.

## M1.4 — Driver-operated hitch and genuine breakable joint

The two-Rigidbody tractor–trailer coupling now has an explicit
**hitch lifecycle**. In the M1.3/M1.4 sample, press **H**:
- Connected: manually release only after both vehicles slow down.
- Detached or previously broken: attempt a new connection ONLY when
  hitch points are close, heading is aligned and approach speed is safe.
  A small close-range correction may snap the trailer's hitch into place
  once, on that keypress, before ConfigurableJoint is created.

While connected, a genuinely overloaded Unity ConfigurableJoint can
**break automatically** if the force or torque exceeds the configured
`VehicleAssemblyDefinition.BreakForceNewtons` or
`BreakTorqueNewtonMeters`. Unity itself removes the joint and sends
`OnJointBreak(float)`; a counter and force reading appear in the lab HUD.
Set either threshold to **0** to disable breaking by that quantity.
The initial 150 kN / 100 kNm defaults are illustrative only, not measured
hardware capacities, and the yaw axis is deliberately free.

**K** in the host lab applies a strong lateral PHYSICS impulse to the
trailer so the real joint solver's overload response can be inspected.
It never calls Detach or fakes a break event. Tune the break thresholds
in your assembly asset, regenerate/test as appropriate.
Once physically broken, the hitch remains broken until the user backs
into position and presses H; trailer brake commands no longer follow
the tractor. The original R remains a manual recovery request only.
No automatic rollover/auto-upright or autonomous reattachment.
See `docs/22_M14_HITCH_LIFECYCLE.md`.

## M1.5 — Variable-length tractor / multi-trailer chains

KAVRA now has a separate, **opt-in** ordered assembly chain for more than
two independent vehicle bodies. Every link is a standard
`VehicleAssemblyDefinition` with its own physical `VehicleCoupling`,
reusable hitch geometry and force/torque break limits. The next link's
Tractor definition must be the previous link's Trailer definition.
Cycles, repeated chassis assets and missing links are rejected.

The logical `VehicleAssemblyChainRoot` holds the vehicles and hitch
components. It relays service/parking brakes **only through the
continuous connected prefix from the lead tractor**. A broken hitch
does not fake an engine-driven trailer, reattach itself or preserve stale
brake values on separated segments.

In the dedicated PhysicsLab, choose
`Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene`.
This creates a 6x6 powered tractor + TWO passive four-wheel trailers:
**3 independent Rigidbodies, 14 independent wheels, 2 breakable joints**.
Original M1.4 single-trailer test remains unchanged.

Lab controls: **J/L** selects a hitch, **H** explicitly detaches/attaches
the selected hitch, and **K** applies physical stress to that hitch's
downstream body. **R** only manually recovers the lead-connected subset
of the convoy, leaving separated rear trailers untouched. Normal
W/S/A/D/Space/Q/E/T drivetrain controls remain on the tractor.
The UI reports each link's Attached/Detached/Broken state.

A new `Tools > KAVRA > Vehicle Chain Wizard` builds/saves an actual
N-Rigidbody Unity prefab from an ordered list of link assets.
The current implementation is a *linear chain*, not a free-form branching
vehicle graph. See `docs/23_M15_MULTI_TRAILER_CHAIN.md`. Real imported
truck models, coupling force calibration and a generalized attachment-port
graph are later milestones. All earlier scenes/CSV captures are preserved.

## M1.5.1 PhysicsLab usability — easier hitch capture and readable motion

This is a **user-requested usability pass** on the same M1.5 modular
physics framework, not another round of microscopic suspension tweaks.

- Default manual `VehicleAssemblyDefinition.MaximumAttachErrorMeters`
  expanded from **0.15 to 0.50 meters**; default yaw range from
  **35 to 55 degrees**. The existing H command, safe body/relative
  speeds, a one-time tiny trailer snap and physics breakForce/torque
  remain intact — **no automatic coupling**.
- Old M1.4 assembly assets migrate only original/default tolerance
  values. Customized tolerances are preserved. These values are still
  exposed in each link's ScriptableObject inspector.
- `VehicleCoupling.AssessAttach` shares a non-mutating result and gap,
  yaw and approach-speed diagnostics with the real `TryAttach` path.
- The newly generated **M1.3 one-trailer** and **M1.5 two-trailer**
  PhysicsLabs use a procedural dark/light checker road, stationary
  15m lane/edge distance markers, blue tractor, orange trailer A,
  red trailer B and dark tires. Ground texture/materials are generated
  locally by Editor under `Assets/VehiclePhysicsLab/Generated/`.
- Electric-cyan and yellow hitch endpoints are visible even in Editor.
  While playing, the selected hitch is bigger and the colored guide
  shows **green READY**, amber NEAR, red FAR, cyan ATTACHED;
  a circular capture radius and HUD status explain whether H can couple.
- Optional, subtle speed-based camera FOV makes forward motion easier
  to read. It is enabled ONLY in newly generated articulated
  demonstration scenes. Old PhysicsLab/M0 scenes are unchanged.

Run `Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene` again (it
reuses and does not overwrite valid user-created vehicle/link assets),
save the generated scene and Play. Press J/L to pick a link, align the
highlighted colored spheres inside its catch ring, and press H. The
road texture and metric reference marks make slip and backwards drift
visible. See `docs/24_M151_PHYSICSLAB_VISIBILITY.md`.
