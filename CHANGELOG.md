# Changelog

## 0.1.0 — 2026-10-14 (M1 modular powertrain foundation — unverified in Unity)

- Added separate `SimplePowertrainDefinition` with torque-by-RPM curve,
  forward/reverse/neutral gears, final drive and efficiency.
- Added `SimplePowertrainRuntime` with speed-coupled engine RPM, basic
  redline torque cutoff and axle-group/wheel torque distribution for
  variable wheel counts (3/4/6/8/20).
- `VehicleSimulationRoot` now selects between legacy direct-torque and
  opt-in simple powertrain. Existing scenes and unassigned definitions
  preserve their original M0 behavior.
- Wizard now creates/assigns the asset and builds a dynamic M1 chassis.
- Added a host-only M1 Powertrain Lab menu, keyboard forward/reverse
  (direction change only when slow), Q/E gear shifting and basic engine
  RPM/gear/wheel torque HUD+CSV.
- Added EditMode regression sources for gear direction, neutral, redline,
  per-axle distribution, arbitrary wheels and M0 compatibility.
- Full clutch/gear engagement dynamics, differential types, engine braking,
  hybrid power units and vehicle certification are NOT claimed.
- **Unity Editor import/compilation and driving tests still pending.**

## 0.0.6 — 2026-10-14 (first real telemetry review and brake jitter fix)

- Reviewed four user-recorded PhysicsLab CSV sessions in `docs/Telemetry/`:
  two StraightBrake, one SteeringStep, one CoastDown.
- Findings documented in `docs/17_M05_TELEMETRY_REVIEW_2026-10-14.md`.
  Identified strong +/-14kN longitudinal-force oscillation near zero speed
  with brakes engaged; repeatable in both StraightBrake sessions.
- Added opt-in/Inspector-enabled, default-on M0.5 low-speed brake impulse
  limit, using nominal per-wheel chassis effective mass and **BODY**
  FixedUpdate time, not the wheel rotor substep interval.
- Added per-wheel BrakeImpulseLimited telemetry and corresponding CSV fields.
- Added three EditMode regression tests (near stop, normal-speed unchanged,
  no-brake/airborne unchanged). **Not run in Unity Editor yet.**
- No auto-anti-flip, artificial roll lock, new powertrain or soil deformation.
- Real Unity A/B acceptance pending; previous user files preserved.


## 0.0.5 — 2026-10-14 (M0.5 steering/slip calibration instrumentation)

- Added optional approximate Ackermann steering computed from the positions
  of steerable and nonsteerable wheels, including rear-steer and multi-axle
  layouts. Preserved legacy `Parallel` mode for existing assets.
- Added optional `TireTuningProfile` per wheel with no-profile fallback.
- Added bounded low-speed slip ratio and signed slip angle diagnostics;
  underlying M0 velocity-slip force solver remains provisional.
- Extended telemetry, HUD and CSV with steering angles, slip metrics, tire
  multipliers, control inputs and maneuver labels.
- Added a dedicated flat asphalt calibration lab + repeatable scripted
  StraightBrake / SteeringStep / CoastDown input runs.
- Added EditMode regression source for arbitrary wheel counts, rear-steer,
  low-speed diagnostic validity and tire tuning composition.
- No automatic rollover prevention. R recovery still requires a keypress.
- Unity compilation, test runner and Play Mode verification **pending**.


## 0.0.4 — 2026-10-14 (surface grip + user-triggered recovery)

- Wired SurfaceProfile multipliers to longitudinal/lateral tire grip and
  rolling resistance at each wheel's active Collider contact.
- Added optional SurfaceZone component and fallback to original default grip
  when no surface profile exists.
- Lab builder now creates colored asphalt, gravel, mud and ice sections with
  reusable ScriptableObject assets; no soil deformation physics.
- Added R-key **manual** recovery only. No automatic rollover prevention:
  recovery requires low speed and valid support, and resets wheel/rigidbody
  motion on explicit user request.
- Extended telemetry: surface identity, grip coefficients, rolling resistance,
  and optional total contact query duration (microseconds per physics step).
- Updated bounded Lab CSV default from 20s to 90s and added EditMode tests.
- Unity Editor/Play Mode validation is still pending.


## 0.0.3 — 2026-10-13 (ride-height/CoM/contact experiments)

- Added progressive bump-stop force and exposed bottom-out diagnostics.
- Nominal spring weights now derive from center of mass and wheel support layout
  rather than assuming all wheels statically carry exactly mass / wheel count.
- Added paired per-axle anti-roll transfer with no negative ground support.
- Added configurable inertia estimate to counter unrealistically thin lab
  chassis primitive, and magenta CoM gizmo.
- Added experimental spherecast contact option with raycast fallback and
  elevated query origin for deeply compressed suspension.
- Added lab telemetry HUD and bounded 20-second CSV capture.
- Added new EditMode regression cases. Actual Unity Editor test and
  Play Mode acceptance are still pending. User noted turning bank + wheel
  ground penetration; work tracked at issue #3.


## 0.0.2 — 2026-10-13 (experimental source; awaiting Unity validation)

- Added first M0 custom raycast contact and spring/damper force integration.
- Added provisional semi-implicit velocity-slip wheel dynamics and combined
  friction limit, direct test wheel torque, service and parking brakes.
- Added optional dynamic Wizard chassis while preserving visual-only mode.
- Added generator for the host Physics Lab with Input System keyboard adapter.
- Added EditMode test sources and per-wheel telemetry.
- Unity Editor compilation and Play Mode acceptance remain **unverified**.

## 0.0.1 — 2026-10-13

- Unity 6.3 LTS UPM scaffold, 3/4/6/8/20W layouts, basic Wizard and docs.
