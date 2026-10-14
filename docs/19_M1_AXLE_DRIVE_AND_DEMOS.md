# KAVRA M1.1 — drivetrain axle routing, Wizard and multi-axle labs

Date: 2026-10-14
Issue: https://github.com/progesor/KAVRA/issues/10
Draft PR: https://github.com/progesor/KAVRA/pull/8
Base branch: `feat/m0-wheel-suspension-prototype`.
Working branch: `feat/m1-core-powertrain-foundation`.

## Main goal

Build KAVRA's reusable core for arbitrary wheel/axle layouts before
investing in small tire deformation, link animations and advanced damage.
Do not modify the proven M0 braking experiment or user recordings.

## Core contract

`VehicleDefinition.Axles` contains optional
`DriveTorqueWeight`, `DifferentialMode`,
`MaximumSpeedBiasRatio` and `SpeedBiasGainPerRadPerSec`.

Physical driven wheel selection remains `WheelDefinition.CanDrive`.
An axle may contain 1, 2 or more driven wheel definitions; each belongs
to a stable `AxleId` group. AxleDefinition.IsDriven is informational
until a formal axles-versus-wheels authority change is designed.

Gearbox output torque: `T_total = T_engine * gear * finalDrive * efficiency`.

Axle a gets
`T_a = T_total * max(0,w_a) / sum(max(0,w_j))`,
with absent/zero legacy share interpreted as `w_a=1`.

In EqualTorque mode, each driven wheel i on axle a receives
`T_i = T_a / N_driven_on_axle_a`. This is an **equal torque command**,
not an open differential limited by the traction of its lowest-grip wheel.

In SpeedBiasedApproximation mode, for each driven axle find its slowest
wheel's absolute angular speed `minOmega` and calculate per driven wheel:

`excess = max(0, abs(omega_i)-minOmega-3rad/s)`
`rawWeight = 1 / (1 + excess * biasGain)`
`weight_i = max(1 / maxBiasRatio, rawWeight)`
`T_i = T_a * weight_i / sumWheelWeightsOnAxle`

This puts more requested torque at slower-spinning wheels without relying
on a four-wheel assumption or creating artificial chassis yaw moments.
All groups preserve commanded torque when no redline cut is active.
A rotor exceeding its gear/redline speed gets zero torque and its
unused allocation is not redistributed on this pass.

**Crucial limitation:** speed bias does **not** link wheel speeds as an
actual locked differential and is not a mechanically calibrated LSD.
Do not label this as a full open/LSD/locked diff simulator. Future M2
graph can introduce constraint-based differentials behind the same
torque-request API.

## Backward compatibility

`SimplePowertrainRuntime(config, wheels)` remains supported and uses
equal-torque routing on equally weighted driven axles.
`SimplePowertrainRuntime(config, wheels, axles)` enables M1.1 settings.
A missing/zero serialized axle weight is interpreted as weight 1;
selecting EqualTorque requires no new data in old vehicle assets.

Existing M0 scenarios still use the default
`VehicleDriveSource.LegacyDirectWheelTorque`. The M1 test scenes are
separate, and every model/powertrain asset is saved under its own name.

## New Wizard controls

After creating/choosing a VehicleDefinition and attaching a
SimplePowertrainDefinition:

- Every axle shows total wheel count and powered wheel count.
- `Drive this axle` toggles `CanDrive` for its wheels and updates
  `IsDriven` metadata.
- Powered axles expose positive relative torque weight.
- Differential command mode can be EqualTorque or
  SpeedBiasedApproximation, with bias limit and gain controls.
- Existing M1 dynamic chassis and prefab saving buttons remain.
- Changes are Undo-aware and mark the ScriptableObject dirty.

## Generated lab configurations

`Tools > KAVRA` contains 3W/4W/6x6/8x8/20W powertrain test
scene generators. The user saves generated scenes as genuine .unity files.
Each preset gets a dedicated VehicleDefinition and (except the previously
created car powertrain) distinct example powertrain asset. The truck
presets have a not-physically-calibrated diesel-like torque curve and gear
ratios so that heavy-body tests do not use a tiny passenger-car motor.

20W means a **20-wheel single-chassis rig**, NOT a two-body 18-wheel
tractor/trailer setup. VehicleAssembly and joints remain future work.

Keyboard: W/S forward/reverse after slowing, Q/E manual gear, A/D
steering, Space brake, Left Shift parking brake and R manual recovery.
No automatic rollover prevention or forced upright orientation.

## Acceptance tests (real Unity required)

- [ ] Unity 6000.3.x compiles runtime/editor/lab.
- [ ] All EditMode tests pass; new tests cover 3W/6W/8W/20W,
  weighted axle torque split, slower-wheel bias, reverse and old API.
- [ ] Wizard driven-axle toggle changes CanDrive; weights/modes persist
  in ScriptableObject and saved prefabs.
- [ ] 4W and 6x6 labs can drive and down/upshift without NaN/Inf.
- [ ] 8x8 lab has four powered axles with expected telemetry.
- [ ] Neutral means no wheel output; reverse output is negative.
- [ ] Existing M0 lab scene and saved CSV files are unchanged.
- [ ] Manual R recovery works only when user requests it.

**Status:** Source implementation and static checks; no actual Unity Editor
compilation or Play Mode evidence was available when this was authored.

## Next development direction

Avoid pixel-level suspension/link realism and low-level tire tweaking
unless user tests reveal a blocker. Next core milestones: modular
gear-shifting state and explicit differential mechanical constraints
for the most common vehicle types, robust wheel visual/pivot binding,
and a first multi-body tractor/trailer. Realistic demo models are
intentionally a separate backlog issue (#9).
