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

---

## 2026-10-13 hotfix: null configuration in lab scene

A generated or previously committed `.asset` file is not guaranteed to
deserialize into a usable `VehicleDefinition`. The lab scene builder now:

1. Finds and validates `Assets/KAVRA_PassengerCar4.asset`.
2. Uses `Assets/VehiclePhysicsLab/KAVRA_M0_LabCar.asset` as a fallback.
3. Creates a *new*, uniquely named persisted PassengerCar4 config if neither
   can be loaded and validated. Existing user assets are not overwritten.
4. Verifies the reference BEFORE discarding the current scene and reacquires
   the persistent object afterward.
5. Logs a precise, actionable error and aborts instead of calling the
   scaffold builder with a null config.

The asset's `m_Script` GUID, package import state and load pipeline should be
investigated locally if this fallback also fails. The original exception was
reported by the user; exact Unity-side reason for the null load was not yet
reproduced in this environment.

**Acceptance status:** Static code inspection only. Unity Editor and Play Mode
must be verified before marking this fix complete.


---

## 2026-10-13 ride-height / center-of-mass / contact follow-up (v0.0.3)

Triggered by a real user Play Mode observation: the vehicle banks lightly in
corners (not inherently a bug), but wheels may intersect the ground when
suspension bottoms. Tracked at https://github.com/progesor/KAVRA/issues/3.

**Implemented in source** (Unity Editor execution not yet verified):

- Estimated static load shares via non-negative best-effort equilibrium around
  `VehicleDefinition.CenterOfMassLocal.x/z`. Uses all wheel mounts, no fixed
  four-wheel assumption. Warns if CoM cannot be statically supported.
- Spring rate is proportional to nominal wheel support, not globally mass/N.
- New progressive bump-stop starts near 85% of travel and continues increasing
  after travel is exhausted. `BottomOutMeters` distinguishes physical
  penetration from an intentional small visual correction.
- Anti-roll bar moves equal/opposite vertical support across wheel pairs with
  matching AxleId (both contacts must be grounded); it does not use an
  artificial global roll-lock constraint.
- Rigidbody inertia in the **test-lab** is approximated with an upright box
  derived from wheel layout and estimated body height. Toggle available.
- CoM is visualized with a magenta gizmo. It remains a local-space definition
  parameter and is not silently shifted down during simulation.
- Contact mode allows baseline raycast or spherecast with ray fallback.
  The raised probe origin improves overlap robustness but does not guarantee
  recoverability under arbitrarily deep impact or extreme scenery geometry.
- HUD now shows contact count, static support reference, bottom-out and tilt.
  Host sample captures 20 seconds CSV at `Application.persistentDataPath`.

**Known limitation:** These changes reduce likely causes of ground penetration
and make clipping measurable, but cannot guarantee zero geometry intersection
with every extreme curb, deep contact, rolled wheel mesh or moving collider.
Do not claim physical calibration before actual Unity test and reference data.

### Repeatable A/B lab run

1. Re-generate the dynamic lab scene and save; avoid comparing stale
   serialized vehicles with new component defaults.
2. Let settle 3–5 seconds; compare `sum(Fz)` to `m*|g|` on flat ground.
3. Run straight acceleration, repeated moderate left/right turns, then brakes.
4. Watch `bottom-out mm` and `tilt`; save the first 20-second CSV.
5. Repeat with `ContactMode=SphereCastWithRayFallback`; record mode and time.
6. Compare CSV columns `bottomout_m,antiroll_n,fz_n,tilt_deg,com_height_m`.
7. Smoke-check 3W, 8W, 20W; monitor extreme CoM outside wheel support.

**Acceptance remains pending:** actual Unity Editor compile, Test Runner
EditMode results, driving videos and before/after telemetry.
