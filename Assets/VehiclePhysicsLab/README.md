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
