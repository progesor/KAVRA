# KAVRA M1 — A working general-purpose powertrain foundation

**Status:** authored in source on `feat/m1-core-powertrain-foundation`,
Unity 6000.3.18f1 real compiler and Test Runner results pending.
Task: https://github.com/progesor/KAVRA/issues/7

## Product focus

Build a genuinely reusable, working vehicle framework before refining
very small tire/brake numerical details. Keep the M0 vehicle physics intact;
new propulsion is **opt-in**. No new packages or commercial assets required
for the UPM runtime; Engine/gear logic references only UnityEngine.

## Implemented source contracts

### VehicleDefinition and Wizard

- `VehicleDefinition.Powertrain : SimplePowertrainDefinition` — **optional**
  ScriptableObject link. All prior .asset files remain valid without it.
- `VehicleSimulationRoot.DriveSource` — enum:
  `LegacyDirectWheelTorque` (0, default) or `SimplePowertrain` (1).
- `VehicleScaffoldBuilder.Create(definition, dynamicPhysics, driveSource)`
  retains two-argument call sites and handles optional M1 mode.
- Wizard creates basic powertrain .asset, assigns it to a vehicle definition
  and builds an M1 chassis without disturbing the old M0 mode.

### PowertrainDefinition

Torque/RPM curve in Newton-metres, idle RPM, rev-limit RPM, engine response
rate, optional launch free-rev estimate, forward gear ratio array, reverse
gear ratio, final-drive ratio, mechanical efficiency. Gears: -1 reverse,
0 neutral, 1..N forward.

### SimplePowertrainRuntime

One state object per vehicle (NOT shared across powertrain assets).
At every chassis physics step:

1. Read previous driven-wheel angular velocities.
2. Estimate shaft-connected RPM through selected gear/final drive.
3. Smooth engine RPM toward wheel-coupled RPM with a small throttle response
   floor for launch. This is a **model approximation**, not a clutch.
4. Evaluate engine torque Nm at RPM and throttle; zero torque if neutral,
   no driven axles, invalid gear or excessive RPM.
5. Multiply by gear, final drive and mechanical efficiency.
6. Group driven wheels by stable `AxleId`, share output equally among
   driven axles and among each axle's powered wheels.
7. Limit power for wheels already exceeding gear/redline-driven speed;
   each wheel's angular dynamics and surface tire forces remain delegated
   to existing physics. No wheel-count constant of four.

`AxleDefinition.IsDriven` is presentation metadata in this first
implementation. The physical routing follows each
`WheelDefinition.CanDrive`, grouped via `WheelDefinition.AxleId`.
Later M2 explicit differential mapping can replace this without
changing the wheel/tire contact solver.

## Known compromises, openly stated

- Engine RPM is approximated from wheel speed; not a real
  inertia/drag/clutch/torque-converter numerical solver.
- Neutral currently yields no wheel power, not an authentic free-revving
  ICE engine. No engine braking or automatic clutch.
- Gear selection is immediate, not a synchronized or automatic gearbox.
  Sample input handles forward/reverse swaps near standstill.
- Group splitting is even per driven axle, not an Open/LSD/Locked
  differential resolving wheel load/relative RPM.
- Single power source; electric/hybrid/multi-motor topology planned later.
- No calibrated automobile behavior, no automatic roll/stability lock.
  R manual vehicle recovery is a separate optional user action.

## Unity starter walkthrough

1. Pull branch `feat/m1-core-powertrain-foundation`.
2. `Tools > KAVRA > Create M1 Powertrain Lab Scene`.
3. Save `Assets/VehiclePhysicsLab/Scenes/M1_PowertrainLab.unity`.
4. Press Play. W forward, S reverse when almost stopped (brakes first
   when moving forward), Q/E down/up manual forward gears, A/D steering,
   Space service brake, Shift park brake, R manually recover upright.
5. Check EngineRpm and SelectedGear on root VehicleSimulationRoot.
   Inspect wheel telemetry `AppliedDriveTorqueNm`; recorder saves
   `drive_source,selected_gear,engine_rpm,engine_nm,delivered_wheel_torque_nm`.
6. For 8x8 and other layouts, Wizard: create definition, create/assign
   powertrain, build M1 dynamic chassis and attach host input & ground.
7. Run EditMode KavraPhysicsEditModeTests and confirm M0 source/legacy
   recordings were not overwritten.

## M1 acceptance criteria

- [ ] Unity compile with no new errors (Unity 6000.3.x).
- [ ] All new powertrain EditMode tests pass (3W/4W/6W/8W/20W).
- [ ] Runtime drives M1 car with W, changes gear via Q/E.
- [ ] S selects reverse only at low speed; braking first at forward speed.
- [ ] Reverse torque negative, neutral has zero drive torque; changing gears
      changes axle drive torque without NaN/Infinity.
- [ ] 8x8 Wizard build runs and wheel torques distribute across 4 driven axles.
- [ ] Previously committed M0 calibration lab still uses LegacyDirectWheelTorque.
- [ ] Natural roll and manual R recovery unchanged.

Actual acceptance evidence must come from Unity Play Mode, **not** a
static analysis assertion. This branch should remain Draft until tested.

## Prefab output — first reusable workflow

Wizard button `4D. Save M1 powered prefab (reusable)` persists a genuine
Unity prefab. It creates the same valid M1 scene scaffold, saves it through
PrefabUtility and removes the temporary scene copy. The saved object
references the vehicle/powertrain assets rather than cloning configuration
at runtime. A destination already occupied by another asset is rejected.

This is an important **framework usability** milestone, but still uses
a box chassis and cylinder wheel mesh placeholders. It cannot yet
bind imported GLB/FBX meshes, trailer articulation, damage skeletons,
production collider mapping or lights/sounds. These remain Wizard M3 tasks.


## M1.1 continuation

A subsequent M1.1 pass extends the foundational implementation with
per-axle torque allocation weights, a bounded wheel-speed-biased torque
split, Wizard axle drive toggles and dedicated 3W/6x6/8x8/20W sample labs.
For design limitations and tests, see `docs/19_M1_AXLE_DRIVE_AND_DEMOS.md`.
