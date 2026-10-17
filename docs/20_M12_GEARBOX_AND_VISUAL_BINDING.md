# KAVRA M1.2 — Gearbox state + model-independent wheel visual binding

**Date:** 2026-10-17
**Task:** https://github.com/progesor/KAVRA/issues/11
**Branch:** `feat/m1-core-powertrain-foundation`
**Draft PR:** https://github.com/progesor/KAVRA/pull/8

## Intent

The main vehicle framework takes precedence over ultra-fine tire-force
corrections or purchasing external meshes. This milestone separates
gear selection, driveline torque and wheel visualization so future
gearbox/clutch modules and arbitrary imported model hierarchies can be
replaced independently.

## Gearbox controller

`VehicleGearboxController` exists once per vehicle. The controller tracks
`CurrentGear` (truly engaged), `TargetGear`, `IsShifting`,
`ShiftProgress01` and `TorqueTransfer01` (0 while changing, 1 engaged).

`TryRequestGear(gear, signedForwardBodySpeed)` accepts -1 reverse,
0 neutral, and 1..ForwardGearCount. A direction change while travelling
above 0.75m/s into the opposite gear is rejected, including changes
from neutral to opposing motion. Neutral remains allowed at any speed.
The host adapter still brakes first when the opposite W/S direction is
requested at speed.

`Step(dt, lastEngineRpm, accelerator, forwardSpeed, GearboxControlMode)`
advances a pending shift and only schedules automatic **forward** shifts
if Automatic mode is selected, accelerator is pressed and cooldown
elapsed. Upshift/downshift thresholds and shift time are tunable in the
`SimplePowertrainDefinition`. The automatic policy is only basic
threshold control, not a predictive adaptive transmission. User can
toggle with T in the M1 sample.

The `VehicleSimulationRoot` calls Step before the wheel torque
calculation and multiplies throttle torque request by `TorqueTransfer01`.
During gear changes drive torque is briefly interrupted. It is **not**
a physical clutch, drivetrain shaft inertia or torque converter.

M0 mode is untouched: `DriveSource.LegacyDirectWheelTorque`
remains default in previously saved scenes, with old steering/controls.

## Visual wheel rig

`VehicleWheelVisualRig` sits on the **WheelAnchor GameObject**. This
root stays a fixed sensor in chassis-local coordinates. Visuals use:

```
WheelAnchor (physical mount, DO NOT animate)
└── TravelPivot (local -Y follows wheel suspension drop)
    └── SteeringPivot (local Y follows wheel steer angle)
        └── SpinPivot (local X follows wheel rotation)
            └── [imported wheel mesh with its original local rotation]
```

Runtime `ApplyPose(dropMeters, steerDegrees, spinDegrees)` updates
those three pivots only and is independent of contact geometry, Rigidbody,
force solver and material pipeline. Component fields allow manually
specifying alternative transforms for unusual imported FBX/GLB hierarchies.

For an M1 newly generated placeholder chassis, the Editor builder
creates this hierarchy and reparents the current cylinder visual,
preserving its world transform. Existing M0 scenes are left alone. To
bind meshes in an old scene, set `WheelAnchor.VisualTransform` to each
appropriate mesh or visual root and choose
`Tools > KAVRA > Bind Selected Vehicle Wheel Visuals` after selecting the
vehicle hierarchy.

`VehicleSuspensionVisualLink` separately positions/orients/scales a
visual shock rod between upper and lower attachments. The lower endpoint
can be nested under TravelPivot; the upper endpoint stays on the chassis.
No physical wheel linkage, joints, steering rod constraints or forces
are added. Initial rod scale persists through Unity prefab serialization.

## Unity sample test checklist (still **not run** in assistant environment)

- [ ] M1 lab can still be generated from `Tools > KAVRA` without Console errors.
- [ ] Q/E manually shifts after a short torque cut, target and engaged gear
      differ while shifting; automatic T mode shifts up/down at thresholds.
- [ ] S rejects reverse while moving quickly forward, then engages reverse
      below 0.75 m/s. N allows neutral.
- [ ] M1 created chassis has Travel/Steer/Spin transforms and shock links;
      wheel meshes move but physical WheelAnchors stay fixed.
- [ ] Old M0 Unity scene still displays its single primitive wheel cylinders.
- [ ] User can bind an imported wheel mesh without changing tire physics.
- [ ] EditMode test suite passes, including duration, automatic shift,
      reverse safety and visual pivot/link tests.
- [ ] Manual R recovery still works; the framework does not automatically
      reorient rolling bodies.
- [ ] 3W/4W/6W/8W/20W layouts have variable-length arrays (no fixed four
      special cases).

## Known limits and future

- Manual torque cut does not account for mechanical clutch slip or engine
  speed dynamics during declutching. Real clutch, shaft inertia, starter
  and shift-sync force law come later.
- Automatic is a basic threshold mode only, not DCT or calibrated AT.
- Shock rods are kinematic scene visuals and do not constrain movement.
- Mesh binding assumes the chosen pivot starts at the wheel mount; wheel
  offset/orientation may need adjustments in Blender/Unity for real assets.
- No newly purchased or external automotive meshes bundled in repository.
- This step does not implement VehicleAssembly, trailer joints or roll
  stabilization. They remain future main-framework projects.

**Acceptance state:** source authored + static structure checked; Unity
compile/Test Runner and driving acceptance await actual Unity Editor runs.
