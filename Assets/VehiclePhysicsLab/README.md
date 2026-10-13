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
