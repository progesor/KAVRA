# KAVRA M0.5 — first real Unity telemetry review

**Date:** 2026-10-14 (Europe/Istanbul).
**User commit:** [d98a683](https://github.com/progesor/KAVRA/commit/d98a68352972b66438a7c15d573fc2475c3e7edf) / `TTT2`.
**New follow-up:** [issue #6](https://github.com/progesor/KAVRA/issues/6).
**Working PR:** [Draft #2](https://github.com/progesor/KAVRA/pull/2).

## Data provenance and checks

The user committed FOUR real experimental PhysicsLab CSV exports in
`docs/Telemetry/`. Also present: genuine Unity
`Assets/VehiclePhysicsLab/Scenes/M0_CalibrationLab.unity`,
`M0_DynamicLab.unity` and four SurfaceProfile+material assets.
These assets are **preserved**; they are not synthetic test results.

| Filename suffix | Maneuver | Complete observed span | Peak speed |
|---|---|---:|---:|
| 130227.csv | StraightBrake #1 | 22.73 s | 42.96 km/h |
| 130257.csv | StraightBrake #2 | 24.50 s | 42.96 km/h |
| 130334.csv | SteeringStep | 19.07 s | 37.40 km/h |
| 130412.csv | CoastDown | 28.02 s | 35.81 km/h |

Combined **22,636 wheel rows**, each with **34 CSV columns**, covering
**5,659 distinct physics steps**, consistently **4 wheels per step**.
No malformed CSV records and no missing/non-finite time/speed fields in
these submitted runs. Frame delta ~0.016667 s (60 Hz). User stopped some
recordings before the 28-second configured duration; available data is
still useful and covers the critical maneuver phases.

All four runs use `M0_Asphalt`, `contact_mode=ray`,
`steer_mode=ApproximateAckermann`, tire profile `Default`.
No ice/gravel/mud transition, SphereCast A/B, dynamic multiaxle,
R manual recovery, or full regression-test evidence is present yet.
**Do not interpret this as a failure to do the requested tests**:
the user delivered the calibration traces requested in v0.0.5.

## Findings

### 1. Static support and repeatability — promising

The 1450 kg passenger car at 9.81 m/s2 requires **14224.5 N** of
steady level-ground vertical support. The observed sums hover around
**14.225 kN** at rest and coast. Both StraightBrake runs are numerically
identical over the initial scripted accelerate/brake segment, even if
one shows an unexpected late contact-loss blip at about 20.43–20.60 s.
This is reproducibility evidence on one environment, not a promise of
cross-platform determinism.

### 2. Straight braking — clear near-zero-speed problem

- Peak speed **42.96 km/h** at t=8.0s.
- Service brake starts at t=9.0s (`brake_input=0.95`).
- At t~10.43–16.00s the vehicle hovers at ~**0.28 km/h**
  (~0.08 m/s) with high brake commanded.
- At that near-stop range, sum(Fx) oscillates approximately from
  **-14.8 kN to +14.4 kN** in both traces, changing direction
  continuously even when RPM telemetry is ~0.
- Each run has 323 near-stopped/strong-brake physics steps exhibiting
  alternating force signs. This is NOT plausible tire friction at rest:
  one FixedUpdate of saturated braking can reverse the chassis speed;
  the next step reverses Fx again.

Likely code-level cause: `WheelDynamicsSolver.Step` computed Fx from
semi-implicit rotor slip at a locked wheel, saturated by mu*Fz, but applied
the resulting **full road force for the entire Rigidbody FixedUpdate**,
without a chassis effective-mass / stop-impulse cap at near-zero speed.
The wheel rotor may substep 4 times while Rigidbody integrates only once.

A first targeted mitigation (v0.0.6 source, Unity NOT YET re-tested):
cap near-stationary, brake-dominated longitudinal opposing force by

`abs(Fx) <= m_wheelEffective * abs(v_forward) / deltaTime_Rigidbody`

using static nominal per-wheel mass shares, not wheel rotor substep dt.
The limiter is optional/default-on, with a 0.5 m/s threshold.
It is **not** a validated static-friction constraint and does not
guarantee slope holding or accurate multibody collision impulses.
Normal-speed braking is unchanged.

### 3. SteeringStep — natural lift/roll plus unbounded test actuator

- Peak speed **37.40 km/h** around t=11.8s.
- Peak chassis tilt **58.77 deg** at t~11.6s.
- Across t~10.40–12.45s, **124 physics steps** have fewer than
  all four wheels in contact (typically two contacting).
- Maximum recorded suspension `bottomout_m=0.03878` (3.88cm).
- Maximum rear driven-wheel speed **~3200.6 RPM** while chassis
  travels ~36.8 km/h; an ordinary free-rolling 0.36m tire at
  that ground speed is only ~271 RPM.

This is useful evidence that the current independent **direct wheel torque
test actuator** can spin airborne driven wheels unbounded; a future
motor/drivetrain has to provide a realistic speed/torque characteristic.
It is **not** evidence that anti-flip physics should be added.
The user explicitly chose natural rollovers plus an R-only manual recovery.

### 4. CoastDown — simple rolling resistance appears coherent

- After acceleration, speed near t=7s is **35.81 km/h**.
- At t=21s, speed is **29.46 km/h** without throttle/brake.
- Average slowdown about **0.126 m/s2** over this interval.
- Nominal rolling coefficient 0.014 implies 0.014*9.81 ≈
  **0.137 m/s2** for a simple pure-rolling model.

The observed deceleration is reasonably consistent with this initial
simplified model. This does not independently validate tire resistance
against real test vehicles or account for omitted aerodynamic drag.

### 5. Contact query cost — initial local measurements only

After initial warm-up, measured per-body-step cast time (all four wheels)
is ~**8.3–9.8 microseconds median**, and ~**11.4–12.0 us p95**
across runs. Some initial/rare spikes are much larger.
These are stopwatch-based Editor observations, **not** Unity Profiler
benchmarks, mobile targets, or a claim of guaranteed 20-wheel performance.
All records are Raycast: no SphereCast cost comparison can be concluded.

## Interpretation and next test (minimal user work)

The submitted CSV tests are valid and already answer the first M0.5
calibration question. Do **not** ask for all four old tests again.

The only immediate follow-up after applying v0.0.6 is to open the project
and compile/run `KavraPhysicsEditModeTests`, then produce **one new
StraightBrake run** with the new low-speed impulse limiter default-on.
Compare near-stop Fx peak and speed jitter against the two old
StraightBrake logs. CSV now records `brake_impulse_limited` and
`brake_limiter_enabled` per wheel row.

If low-speed jitter remains or slope creep increases, iterate using
effective mass and a proper static tire/friction hold model. Only
once that is stable should we tune skidpad lateral grip, contact-mode
sweeps, multiple axle load balancing and drivetrain torque limiting.
Do not add automatic rollover prevention.

**Acceptance status:** Input data is real and read/aggregated successfully;
the NEW v0.0.6 mitigation has not been compiled or tested in Unity Editor
in this workspace. Preserve Draft PR #2 pending local acceptance.
