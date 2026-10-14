# M0.5 — steering geometry and tire calibration instrumentation

Updated: 2026-10-14
Repository: https://github.com/progesor/KAVRA
Work item: https://github.com/progesor/KAVRA/issues/5
Draft PR: https://github.com/progesor/KAVRA/pull/2

## Why

The original M0 physics treated all steerable wheels as parallel at exactly
the same input angle, regardless of wheelbase/track. Its provisional
velocity-slip model had no bounded wheel-slip ratio/angle diagnostics and
vehicle-wide stiffness/grip settings had no per-wheel override.

This pass introduces testability and configuration rather than promising
high-fidelity tire modeling. The existing force solver is deliberately
**not replaced** until real Play Mode data and regressions are available.

## Steering

`SteeringGeometryMode.Parallel`: wheel steer = input * MaximumSteerAngleDeg.
This is the legacy default in existing scenes and the ScriptableObject model.

`SteeringGeometryMode.ApproximateAckermann`: estimate a pivot line from
mean unsteered wheel Z, and mean steered wheel Z as nominal wheelbase.
The per-wheel angle for steering input with sign s and wheel local x,z is
an atan2 geometry with circle radius:

`R = L / tan(abs(driverCenterAngle))`

`delta_i = atan2(s * (z_i - pivotZ), R - s*x_i)`

This produces different inner/outer angles, including multiple steered front
axles and a rear-steered forklift-like vehicle. For all-wheel-steered-only
layouts with no unsteered reference axle, code falls back to Parallel rather
than fabricating a pivot. Independent four-wheel counter-steer/crab groups
are future topology features.

Geometry is analyzed once at initialization, then per-wheel angle is
computed without allocations or rescanning layout each FixedUpdate. This
is geometry **input mapping**, not automatic chassis yaw/roll stabilization.
Natural body roll, understeer/oversteer and vehicle rollover remain possible.

## Tire tuning

Optional `WheelDefinition.TireProfile`:
- Longitudinal/lateral stiffness multipliers
- Longitudinal/lateral maximum grip multipliers
- Rolling resistance multiplier
- Approximate rotational inertia multiplier

Null profile means all 1.0 multipliers and retains original behavior.
SurfaceZone -> SurfaceProfile is resolved for the ground under each
wheel, then optional tire profile composes with it. Sample asset is an
Editor-created Unity ScriptableObject; no extra package dependencies.

The tire profile is **not** an M0 tire model selector; it has no thermal,
pressure, deformation or calibrated Pacejka coefficients.

## Slip metrics (diagnostics only)

Contact longitudinal speed = v_x; lateral speed = v_y; wheel peripheral
speed = omega*r.

`slipRatio = clamp((omega*r - v_x) / max(v_ref, abs(v_x), abs(omega*r)), -2, 2)`

`slipAngle = atan2(v_y, max(v_ref, abs(v_x)))` (converted to degrees)

Use `v_ref=1.0 m/s` initially for stability around standstill.
A wheel stopped while moving forward ~10 m/s reports slip ratio -1;
a stationary car/wheel reports 0. Positive lateral slip angle results
from positive contact lateral velocity. This convention is for consistent
plots, not a definitive tire industry formula. Slip ratio is **bounded and
regularized** and is not the core's velocity-slip force equation.

## Calibration scene: same inputs, same road

Menu: `Tools > KAVRA > Create M0 Calibration Run`.
Produces 600 m x 220 m level asphalt surface and a 4-wheel test chassis,
without ramp or gravel/ice transitions. The experiment runner has three
time-scheduled command sequences:
- StraightBrake
- SteeringStep
- CoastDown

`VehicleLabManeuverRunner` updates controls at FixedUpdate (priority -200)
before the default-order `VehicleSimulationRoot`. Keyboard driving is
suppressed on scripted runs, while manual R recovery is still possible.
Avoid R during a measurement. A new calibration lab records for 28 seconds.

Telemetry CSV is stored in Application.persistentDataPath and includes wheel
grip/stiffness, steering angle, slip ratio/angle, torque/brake inputs, speed,
normal load, roll and active maneuver. Compare runs with identical
vehicle settings/contact method, repeat multiple times and annotate Editor
version, hardware and physics fixed timestep. This input sequence is
repeatable; Unity's rigidbody integration is not guaranteed bit-for-bit
deterministic across platforms.

## Unity acceptance (NOT YET RUN)

- [ ] C# compile and assembly references resolve on Unity 6000.3.18f1.
- [ ] All KavraPhysicsEditModeTests pass (including steering sign/inner-outer,
      variable 3/4/6/8/20W, tire profile composition, near-zero slip).
- [ ] Interactive surface lab still works and R manual recovery works.
- [ ] Calibration scene launches independently and produces a 28s CSV.
- [ ] StraightBrake run: no NaN, brake force/time plot sane.
- [ ] SteeringStep: inner wheel angle magnitude greater on inside, not an
      artificial chassis yaw correction.
- [ ] CoastDown: no runaway wheel angular speed, finite deceleration.
- [ ] New assets do not change behavior of existing definitions with null
      TireProfile and SteeringMode.Parallel.
- [ ] Record results and only then decide next iteration of tire force model.

## Future step

Plot CSV of a series of ScriptedManeuver runs, compare against independently
chosen vehicle/reference data, then target low-speed slip/lock, inertia and
brake reaction fixes with regression tests before adding the full powertrain.
