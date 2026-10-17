# KAVRA Vehicle Physics Laboratory

This is the **host project's sample**, separate from the reusable UPM
package. It uses Unity's Input System that already exists in this Unity
project; the KAVRA runtime package does not depend on it.

## Generate a test scene

1. In Unity, choose `Tools > KAVRA > Create M0 Physics Lab Scene`.
2. Save the generated, unsaved scene at
   `Assets/VehiclePhysicsLab/Scenes/M0_DynamicLab.unity`.
3. Press Play. Controls: **W/S** torque/reverse, **A/D** steering,
   **Space** service brake, **Left Shift** parking brake, **R** explicit
   vehicle recovery (only when nearly stopped; arrow keys also work).
4. Select vehicle root and expand `VehicleSimulationRoot > Wheel Telemetry`.
5. Run `Window > General > Test Runner > EditMode` for physics unit tests.

Lab content: flat colored asphalt / gravel / mud / ice strip colliders,
experimental 4-wheel M0 prototype, off-axis ramp, keyboard adapter,
follow camera and directional light. The scene is made by
an Editor script, not checked in as a pre-serialized Unity scene.

## Null VehicleDefinition regression fix (2026-10-13)

The lab generator now looks for a **valid, serialized** configuration at
`Assets/KAVRA_PassengerCar4.asset`, then
`Assets/VehiclePhysicsLab/KAVRA_M0_LabCar.asset`. If both are missing,
have a broken `m_Script` reference, or fail validation, it generates a new
`KAVRA_M0_LabCar.asset` at a unique path. It never overwrites existing
assets and checks the reference *both before and after* changing scenes.

When an asset cannot be created/reloaded, the generator logs an actionable
error and does not pass a null configuration to `VehicleScaffoldBuilder`.
For repeated issues, inspect the affected `.asset` file in Inspector for
`Missing Script`, and check Unity's import/compile errors.

## Checks required

- Errors during package import/Editor compile?
- Grounded wheel count, static suspension force sum roughly mass * 9.81?
- Car neither slowly sinks nor oscillates uncontrollably at rest?
- W/S, A/D, service brake and parking brake cause plausible motion?
- 3W and 8W from Wizard physically rest on the ground?
- Results recorded at `docs/13_M0_FIRST_PHYSICS_SLICE.md` / issue #1.

Raycast wheels and provisional tire model can be wrong at rocks, sharp curbs
and low-speed slopes. M0 is a technical feasibility experiment, not polished
vehicle handling or MudRunner-style deforming terrain.

Tests: `Assets/VehiclePhysicsLab/Tests/EditMode`.

## M0 suspension/contact/CoM follow-up (0.0.3)

User observation: the chassis banks in a turn and a wheel may visually enter
the ground at high suspension compression. The update adds:

- **Progressive bump-stop** within the final 15% of wheel travel, extra
  geometric end-stop response after bottoming, and telemetry `BottomOutMeters`.
- **Static per-wheel spring-weight estimate** from CoM X/Z and wheel footprint
  (no global mass/N assumption for every wheel).
- Optional **anti-roll pairing** for wheels sharing an axle. It conserves
  per-pair vertical support and cannot create negative road load.
- More realistic **temporary chassis inertia** from footprint and estimated
  body height instead of the thin cube's mass inertia.
- **Center of mass magenta gizmo** when the vehicle root is selected.
- Selectable `WheelContactMode`: default baseline Raycast; alternative
  SphereCastWithRayFallback for curb experiments.
- A raised ray origin makes the suspension contact query less vulnerable to
  the wheel mount already crossing the ground surface.
- The lab now auto-records a **20-second CSV** of suspension/contact/speed
  telemetry under `Application.persistentDataPath`. Unity Console prints path.

### Ride-height acceptance steps

1. Pull the updated M0 branch and regenerate the lab scene with
   `Tools > KAVRA > Create M0 Physics Lab Scene`.
2. Drive through a left/right turn while observing the HUD: contact count,
   support force vs static reference, maximum bottom-out (mm), body tilt
   and world CoM height. Some body roll is physically expected.
3. Select the vehicle to see the magenta CoM gizmo. Modify
   `VehicleDefinition.CenterOfMassLocal` **offline** (not by changing the
   Gizmo) to tune future cars, then regenerate a test chassis.
4. In `VehicleSimulationRoot` compare `ContactMode: Raycast` with
   `SphereCastWithRayFallback`, keeping other settings equal.
5. Review the generated CSV after 20 seconds and check whether
   `bottomout_m` increases. A zero/near-zero bottom-out is desirable on
   level asphalt. Bump-stop / visual correction do NOT guarantee no clip on
   every sharp edge or extreme impact.

Use normal-speed driving before ramp impacts. The model is still an
unvalidated prototype, not an FEM tire or MudRunner soil simulation.


## M0 surface comparison and manual recovery (v0.0.4)

`Tools > KAVRA > Create M0 Physics Lab Scene` produces contiguous flat
ground segments. Driving along +Z crosses **asphalt, gravel, mud, ice**.
Same-height Colliders isolate the friction differences; these are test
presets, not physically validated coefficients or deformable soils.

The host creates/reuses `Assets/VehiclePhysicsLab/Surfaces/M0_*.asset`
(`SurfaceProfile`) and `M0_*.mat` assets. Existing tuned assets are never
overwritten. `SurfaceZone` on each Collider supplies the profile.
Where no profile exists, baseline tire grip is unchanged.

Per-wheel `WheelTelemetry` exposes `SurfaceName`, effective friction
coefficients and rolling resistance. The HUD reports surface transitions;
a **90-second CSV** is recorded at `Application.persistentDataPath`.
If `MeasureContactQueryCost` is enabled, HUD and CSV include total cast
time in microseconds per physics step. Compare ray vs sphere+fallback
on the same hardware/scene. This is a diagnostic, not a Unity profiler
replacement or a general performance guarantee.

The **R** key tries an explicit manual upright recovery: vehicle must be
slower than **1.75 m/s**, all wheels need ground below, steep/blocked spaces
are rejected. It keeps horizontal heading and position, chooses safe clearance,
and clears velocities and rotor state. Recovery is NOT automatically called
for rollover, collisions, idle or speed thresholds; the truck/car is free
to tip over under physical forces. Rejected recovery is shown in HUD.

EditMode tests cover surface coefficient mapping, missing-profile fallback,
surface component lookup, heading preservation and recovery speed/ground.
Real Unity Play Mode acceptance and measured CPU comparisons are pending.


## M0.5 scripted tire and steering calibration (v0.0.5)

Use `Tools > KAVRA > Create M0 Calibration Run` for a new flat asphalt
lab with a **wide 600 m** surface to avoid geometry/surface changes while
measuring basic handling. Save as
`Assets/VehiclePhysicsLab/Scenes/M0_CalibrationLab.unity`.

The chassis contains `VehicleLabManeuverRunner`, configured by default
for StraightBrake. Choose among:

- `StraightBrake`: 2s settle, 6s torque, 1s coast, 7s brake.
- `SteeringStep`: 2s settle, 6s accelerate, 2s steer right, 2s steer
  left, then brake.
- `CoastDown`: 2s settle, 5s accelerate, coast through 21s then brake.

`VehicleLabKeyboardInput.AcceptDrivingKeys` is disabled in these scenes,
so keypresses cannot overwrite the scripted controls. R remains a user-
requested, **manual** upright recovery; using it invalidates run comparisons.
The regular interactive surface lab retains W/S/A/D/Space/Shift.

The calibration scene sets ApproximateAckermann mode. Physics code uses
a precomputed layout pivot; each steered wheel receives a geometric angle.
The global `MaximumSteerAngleDeg` remains an approximate center angle.

`WheelTelemetry` and CSV now include `SlipRatio`, `SlipAngleDegrees`,
`SteerAngleDegrees`, optional `TireProfileName`, effective longitudinal and
lateral stiffness, normalized input signals and maneuver identity.
Slip ratio/angle are intentionally bounded and regularized near zero speed.
**They are diagnostics, not new tire force calculations or validated metrics.**

For tuning wheel-specific grip/stiffness, create `Assets > Create > KAVRA
> Tire Tuning Profile` and assign it under the desired WheelDefinition in
the VehicleDefinition asset. Re-open Play to observe changed parameters.
Presets are not silently assigned to existing vehicle definitions.

All scripts are written, but the Unity Editor compile/Test Runner/Play Mode
acceptance must still be checked locally. Results will guide future low-speed
tire-force stabilization and braking calibration.


## v0.0.6 user telemetry analysis / near-stop brake force fix

The initial four user CSV files are committed in `docs/Telemetry/` in
commit `d98a683`. See `docs/17_M05_TELEMETRY_REVIEW_2026-10-14.md`
for quantitative conclusions. The project generated actual calibration
scenes, so there is no need to recreate old data.

The `VehicleSimulationRoot` inspector now offers
`LimitNearStopBrakingImpulse` (enabled by default on new components)
and `NearStopBrakeSpeedThresholdMps` (initially 0.5 m/s).

New CSV columns `brake_impulse_limited` and `brake_limiter_enabled`
show when low-speed opposing brake force was clipped and whether the
option was active. The limiter affects only near-stop locked-wheel brake
impulses; normal road grip / braking above 0.5 m/s is unchanged.

After Unity compiles and tests are run, ONE new StraightBrake
A/B run with default limiter settings is sufficient for the first
post-fix comparison. No automatic vehicle righting is ever invoked:
the user must press R to request manual recovery.


## M1 core powertrain lab (0.1.0)

The M0 Physics Lab and scripted M0 calibration scene are **preserved**.
To test the new powertrain separately choose:

`Tools > KAVRA > Create M1 Powertrain Lab Scene`

Save the generated scene to
`Assets/VehiclePhysicsLab/Scenes/M1_PowertrainLab.unity` and Play.

The M1 menu creates/reuses two **dedicated** host assets:
`Assets/VehiclePhysicsLab/M1_PassengerCar4.asset` (vehicle)
and `Assets/VehiclePhysicsLab/M1_SimplePowertrain.asset` (engine/gearbox).
They do not overwrite your earlier M0 definition or telemetry CSVs.
It builds one 4W dynamic chassis on a long flat test road.

Keyboard: **W** forward accelerator, **S** reverse accelerator when almost
stopped (brakes first if still moving forward), **A/D** steer, **Q/E**
down/up-shift when in forward gears, **Space** brake, **Left Shift**
parking brake, **R** request manual upright recovery at low speed.

Inspector/HUD: `DriveSource`, `SelectedGear`, `EngineRpm`, motor Nm,
total distributed wheel torque, per-wheel applied torque.
CSV also contains `drive_source,selected_gear,engine_rpm,engine_nm,
delivered_wheel_torque_nm`.

**Limitations:** no automatic transmission shift logic, clutch or
differential torque feedback, engine braking, measured vehicle tire
calibration or automatic anti-flip. The M1 starter is a working simple
powertrain chain, **not** an industrial automotive simulation yet.
M1 source requires Unity compile and Play Mode verification. M0 data should
not be used as a numerical baseline for the powered M1 vehicle.


## M1.1: quick multi-axle demonstration menus

With the `feat/m1-core-powertrain-foundation` branch open, use any of:

- `Tools > KAVRA > Create M1 Powertrain Lab Scene` — 4W car.
- `Tools > KAVRA > Create M1 Three-Wheel Lab Scene` — 3W delta.
- `Tools > KAVRA > Create M1 6x6 Truck Lab Scene` — 6W / 3 driven axles.
- `Tools > KAVRA > Create M1 8x8 Truck Lab Scene` — 8W / 4 driven axles.
- `Tools > KAVRA > Create M1 20W Stress Lab Scene` — 20W
  **single-chassis** load/stress test, NOT a multi-body semi-trailer.

Each menu creates its own reusable `M1_<template>.asset` and
`M1_Powertrain_<template>.asset` (car keeps existing
`M1_SimplePowertrain.asset`). Valid existing tuned assets are **reused**
without overwriting. Truck examples generate an uncalibrated diesel-like
engine torque curve appropriate to a heavy demo vehicle.

Save the generated scene from the Unity Editor; no pre-generated scenes
are committed for these menu options. Use W/S, Q/E, A/D, Space, Shift and
manual R recovery as in the 4W host sample. No high-fidelity meshes
or automatically preventing rollovers.


## M1.2 gearbox and visual wheels

New M1 lab vehicles/prefabs generated from the Wizard contain separate
`TravelPivot/SteeringPivot/SpinPivot` transforms under each WheelAnchor,
plus a simple shock preview cylinder following two endpoints. They are
visual-only and **do not change wheel physics**. Replace the mesh under
SpinPivot with imported FBX/GLB wheel meshes, retaining correct pivot
position. For an existing scene: select vehicle root and invoke
`Tools > KAVRA > Bind Selected Vehicle Wheel Visuals`.
Assign an imported mesh to `VehicleWheelAnchor.VisualTransform` first.

Input on interactive M1 lab: W forward, S reverse when near stopped,
Q/E manual forward gear down/up, N neutral, T toggle automatic/manual
forward shift scheduling, Space brake, Shift parking brake,
**R** explicit user-triggered upright recovery. Existing M0 sample
control mode remains unchanged. Gearbox HUD now shows engaged gear,
target, shifting status, and selected mode. CSV adds
`engaged_gear,gearbox_mode,shift_in_progress,shift_progress01`.

This is a starter visual binding, not realistic suspension linkage
geometry or automatic mesh discovery. Neither gearbox changes nor the
visual hierarchy have been verified in actual Unity Play Mode yet.


## M1.3 two-body tractor-trailer (ten wheels)

Menu: `Tools > KAVRA > Create M1.3 Tractor Trailer Lab Scene`.
Save the generated scene to
`Assets/VehiclePhysicsLab/Scenes/M13_TractorTrailer.unity`.

Dedicated editable asset folder `Assets/VehiclePhysicsLab/Assemblies/`:
`M13_Truck6x6.asset`, `M13_Trailer4W.asset`,
`M13_TractorPowertrain.asset` and `M13_TractorTrailer.asset`.
A valid existing asset is reused and never overwritten.

The demonstration is a **6x6 powered tractor plus 4W passive trailer**:
two separate Rigidbody components, 10 independent wheel sensor/rotor
states, one ConfigurableJoint with free yaw/limited pitch+roll.
Basic brake commands are propagated to the trailer; the engine does
not send shaft torque to it. A simple drawbar/hitch marker and HUD
show coupling alignment and relative yaw; neither changes physics.

Use W/S forward/reverse, Q/E gears, T auto/manual, Space brakes, R
to manually reposition **both vehicles** when near stopped and
preflight safe. User input is the only recovery trigger. There is no
automatic rollover prevention or auto-upright. The assembled prefab
can be saved through `Tools > KAVRA > Vehicle Assembly Wizard`.

Actual Unity Editor verification is still needed for joint drift,
turning/reversing, collision behavior, and two-body manual recovery.


## M1.4 — hitch connect/disconnect and physics break test

In `Tools > KAVRA > Create M1.3 Tractor Trailer Lab Scene`,
the generated M1.4-capable assembly begins attached. Controls:

- **H** while near stopped: manually detach; after backing hitch points
  near each other with a matching heading, press H again to reconnect.
  Too far / bad yaw / excessive point or body speed is rejected with
  a concrete reason in the existing sample HUD.
- **K**, while connected: optional **lab-only** lateral trailer impulse
  using Rigidbody.AddForceAtPosition. This applies real PHYSICS load to
  the ConfigurableJoint; only the Unity solver decides whether the joint
  breaks based on configured force/torque thresholds. No scripted detach.
- **R**: only user-triggered upright recovery. If still coupled,
  reposition both bodies after safe preflight; otherwise only the
  selected tractor chassis is recovered.

`VehicleAssemblyDefinition` Inspector shows `BreakForceNewtons`
(default 150000 N), `BreakTorqueNewtonMeters` (default 100000 Nm),
hitch gap, heading and approach speed settings. For a visible lab
demonstration adjust thresholds downward and reconstruct the scene or
unhitch/rehitch so the new live joint gets those values; the raw
Unity ConfigurableJoint Inspector also exposes current breakForce/
breakTorque. Thresholds are UNCALIBRATED placeholders.

The assembly HUD displays Attached/Detached/Broken, number of actual
physics break callbacks, last solver-reported break force, hitch gap,
yaw and break thresholds. The K shock test does not fake break events.
Reattachment never happens automatically.

A new EditMode suite `KavraHitchLifecycleEditModeTests` covers joint
threshold transfer, attach prerequisites, manual unlink/relink,
OnJointBreak status notification simulation and legacy asset
migration. Actual physical overload breaking requires Unity Play Mode;
a synthetic EditMode callback does not claim to prove solver breaking.


## M1.5 — true multi-trailer lab (6x6 + 4W + 4W)

`Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene` generates three
independent Rigidbody objects and two ConfigurableJoints:
**6-wheel powered truck + two passive four-wheel trailers**, 14 total.
Save as `Assets/VehiclePhysicsLab/Scenes/M15_TwoTrailerChain.unity`.

Reused/created data under `Assets/VehiclePhysicsLab/Assemblies/`:
M15 tractor definition, two DIFFERENT 4W trailer definition assets,
separate M15_Link0_TractorToA and M15_Link1_AToB hitch assets, one
M15_MultiTrailerChain asset and the truck motor profile.
Existing assets are reused without silent overwrites.

Controls: W/S forward/reverse, A/D steering, Q/E shifting, T
manual/automatic gearbox, Space brake, R EXPLICIT manual recovery.
**J/L** chooses a hitch index (0 tractor→A, 1 A→B).
**H** manually attaches/detaches THAT hitch only (requires low speed,
small aligned gap); **K** physically stresses the selected hitch
without directly invoking a break. The HUD shows each hitch's
Attached/Detached/Broken status.

Example: detach link 1 via J/L→H. The tractor still tows trailer A,
but trailer B is independent and loses inherited brake signals. When
link 0 is broken, the still-attached A→B pair does not receive tractor
brakes. Only an intentional H can reattach a valid link.
**R** recovers ONLY the lead-connected vehicles together after a
full preflight, without moving any disconnected rear convoy.

Save a reusable N-body prefab via
`Tools > KAVRA > Vehicle Chain Wizard`. ConfigurableJoint force/torque
failure limits remain each link asset's own values (v0.1.4 subsystem).
Unity 6000.3.x compile, Test Runner, actual towing/reverse/hitch break
and prefab round-trip must be checked in Unity.


## M1.5.1 — easy-to-read checker lab and visible hitch capture

The **two** existing lab generation menus now decorate their generated
scenes with a large high-contrast procedural checker road (3-meter
squares), stationary 15m reference markers, blue tractor, orange
trailer A, red trailer B, and dark tires. All such styling is
EDITOR-only and does not change tire forces, collision geometry
or the UPM solver. Assets are cached as native files under:
`Assets/VehiclePhysicsLab/Generated/`.

The H coupling default is **0.50m** instead of 0.15m, and allowed
initial yaw is **55 degrees** instead of 35 degrees. It is still
explicit and only works at safe low body/hitch speeds. A one-time
small trailer snap finishes alignment after an accepted H request.

A new `VehicleLabHitchGuide` presents a bright cyan/yellow pair,
larger selected markers, a green/yellow/red colored guide line,
a circular capture radius and a status message:
`READY`, `TOO FAR`, `BAD ANGLE`, `TOO FAST`,
`WAIT` or `CONNECTED`. In the multi-trailer scene, J/L selects
the hitch to highlight and H operates it. The two-body scene
has its own identical guide. The guide is visual only; it NEVER
auto-attaches.

An opt-in chase camera FOV effect adds at most 6–7 degrees as the
vehicle reaches roughly 90 km/h. This and fixed checker squares help
show whether the tractor is advancing, skidding or pulling the
trailers backwards.

**To see changes in the lab:** Pull the M1.5 branch in GitHub Desktop,
run the M1.5 or M1.3 lab menu again and save the generated Unity scene.
Existing scenes and custom assets are not overwritten. If a prior
VehicleAssemblyDefinition uses the old exact default 0.15m / 35deg
values, the version upgrade updates those; a custom manual value
is left as it was. For exact scope, see docs/24.


### M1.5.1 hitch guide hotfix (Editor setup)

If the M1.5 lab generator previously threw
`NullReferenceException at VehicleLabHitchGuide.TintRenderer`,
update the branch: `Setup()` now initializes `MaterialPropertyBlock`
before rendering hitch markers in the Editor. No need to recreate
or delete the generated checker/material assets. Regenerate the lab
scene after the pull and verify it opens with visible markers.
