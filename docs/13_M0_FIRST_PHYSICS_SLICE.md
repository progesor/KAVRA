# M0 v0.0.2 — first dynamic wheel/suspension slice

**Status:** Source implemented in `feat/m0-wheel-suspension-prototype`; Unity
Editor compile, Test Runner and Play Mode **not verified** in this environment.
Track: https://github.com/progesor/KAVRA/issues/1.

## Purpose

Prove that KAVRA can support a variable number of wheel sensors with a
single rigid chassis and minimal dependencies. Avoid FEM tires and true
multibody suspension linkages; compute reasonable forces and useful telemetry.

## Vertical contact & suspension

Each anchor is fixed on a chassis. The free wheel center is one suspension
travel below the mount. The query is a single downward ray with maximum length

`radius + travel + 0.02 metres`.

The nearest non-trigger, non-self hit is used. Compression is

`clamp(radius + travel - hitDistance, 0, travel)`.

Linear spring force is `springRate * compression`. Compression/rebound
damping uses the change in compression divided by delta time (velocity clamped
at 5 m/s), and negative total suspension force is clamped to zero.
No suction forces. First contact frame has no damper impulse. The spring
reacts along the mount's up axis.

The Editor builder places a level-ground car at approximately half stroke.
When every wheel supports the same nominal mass, the automatic rate is

`k = (bodyMass / numberOfWheels * |gravity|) / (travel * sagFraction)`.

This is a symmetric **initial approximation** only; real axle loads,
center-of-gravity distribution and bogie load sharing need later tuning.
Contact normal load used for grip is the spring force's projection onto the
contact normal.

## Provisional tire physics

At each FixedUpdate, chassis contact point speed minus ground body's contact
point speed is projected onto local wheel-forward and contact-right axes.
Steer rotates the local forward axis. Angular wheel dynamics use

`I * domega/dt = T_drive - T_brake - T_rolling - Fx * wheelRadius`.

A semi-implicit linear velocity-slip estimate computes longitudinal `Fx`;
lateral resistance is `Fy = -k_y * lateralSpeed`. Both are clamped into a
combined longitudinal/lateral friction ellipse using normal load. Internal
wheel substeps stabilize the rotor, but DO NOT step Unity Rigidbody faster.
All tire + spring forces are added to the chassis at the contact point;
dynamic ground Rigidbody gets opposite reaction force.

This is **not** a calibrated Pacejka/Fiala solver; at very low speed and on
inclines the slip-velocity model may exhibit creep. There is no static
contact patch or wheel deformation model yet.

## M0 sample controls

`VehicleSimulationRoot.SetInputs(throttle, steering, brake, parking)`
consumes normalized signals. `Throttle` applies test torque to each driven
wheel directly. There is no engine RPM, gearbox or torque split; the same
engineered vehicle could later use a real powertrain.

Host script `VehicleLabKeyboardInput` maps W/S, A/D, Space and Left Shift.
If Input System is not enabled, keyboard input is disabled and the sample
shows a HUD warning. Core package does **not** reference Input System.

## Reproducible Unity acceptance

1. Open Unity 6000.3.x and check Console for errors.
2. Run EditMode `KavraPhysicsEditModeTests` via Test Runner.
3. Menu `Tools > KAVRA > Create M0 Physics Lab Scene`.
4. Save `Assets/VehiclePhysicsLab/Scenes/M0_DynamicLab.unity`; Play.
5. Let vehicle settle. Record chassis mass `m`, wheel telemetry `Fz`
   and total vertical support. Compare with `m*9.81` (not equal at transient
   frames or on non-level surfaces).
6. Test W/S straight-line acceleration, Space braking, A/D steering, Shift
   parking brake. Check wheel RPM and longitudinal/lateral forces for
   NaN/Inf, oscillation, snap or unrealistic roll.
7. Run Wizard dynamic chassis with 3W and 8W configurations; place on ground.
8. Test own-collider filtering and rough ramp crossing as separate tests.
9. Record findings, Unity Console output and machine performance.

### Acceptance sheet (to be completed **in actual Editor**)

| Check | Status |
|---|---|
| Unity package import / compile | NOT RUN |
| EditMode tests | NOT RUN |
| Static wheel load 4W | NOT RUN |
| 4W drive/steer/brake | NOT RUN |
| 3W smoke | NOT RUN |
| 8W smoke | NOT RUN |
| One-minute stable simulation | NOT RUN |

## Known limitations

- Raycast tire response across rocks, edges and gutters is not production
  quality; sphere/multi-point contact is a later comparison.
- Body collision cube is an intentionally primitive fallback collider; the
  wheel preview cylinders have no physics colliders.
- No steering Ackermann, caster/camber, sidewall elasticity, self-aligning
  torque, load-sensitive tire curves, static hold on slopes, transfer
  diff/engine, engine drag, ABS/ESC, terrain deformation or damage.
- Compression rate relies on frame-to-frame samples and may spike at hard
  geometric transitions even with a cap.
- No CPU benchmark data has been collected; performance claims are prohibited
  until actual profiling is recorded.
- `RaycastNonAlloc` takes the nearest valid hit from a **32-hit buffer**.
  Extremely overlap-dense scenes may overflow its buffer.
- `WheelTelemetry` is a runtime snapshot, not a recorder; CSV/graph capture
  and regression suites come next.
- Tuning a single spring rate across every axle is approximate for trucks;
  axle mass support and ride-height profiles need their own calibration.

## Next improvements once this slice passes

- Real contact-mode comparison (Ray vs Sphere vs width sampling).
- Fixes based on Unity runtime results, load balance and no-contact behavior.
- Tire static hold / load sensitivity / configurable surface multipliers.
- Contact telemetry recorder and performance profiling.
- Separate powertrain and per-axle torque allocation, not extra hardcoded
  4-wheel behavior.
