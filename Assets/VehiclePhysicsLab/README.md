# KAVRA Vehicle Physics Laboratory

This is the **host project's sample**, separate from the reusable UPM
package. It uses Unity's Input System that already exists in this Unity
project; the KAVRA runtime package does not depend on it.

## Generate a test scene

1. In Unity, choose `Tools > KAVRA > Create M0 Physics Lab Scene`.
2. Save the generated, unsaved scene at
   `Assets/VehiclePhysicsLab/Scenes/M0_DynamicLab.unity`.
3. Press Play. Controls: **W/S** torque/reverse, **A/D** steering,
   **Space** service brake, **Left Shift** parking brake (arrow keys also work).
4. Select vehicle root and expand `VehicleSimulationRoot > Wheel Telemetry`.
5. Run `Window > General > Test Runner > EditMode` for physics unit tests.

Lab content: cube-ground, experimental 4-wheel M0 prototype, ramp,
keyboard adapter, follow camera and directional light. The scene is made by
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
