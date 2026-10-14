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

---

## 2026-10-14 update: v0.0.6 **after** telemetry received — narrow brake fix validated

**User commit:** [c278b42 — T3](https://github.com/progesor/KAVRA/commit/c278b426a6578bb6ae74b83d45a50f730c94c22d)
adds `docs/Telemetry/kavra_m0_telemetry_20261014_132146.csv` and saves
the updated Unity calibration scene. The CSV has **3,572 valid wheel
samples**, **893 physics steps**, from `t=0.01667` to `t=14.88328` s.
It uses the **StraightBrake** sequence, all four wheels, `M0_Asphalt`,
Raycast, ApproximateAckermann and has `brake_limiter_enabled=1`.
No malformed rows were detected. It does **not** cover the full nominal
28-second calibration duration, but includes the full accelerate/brake
transient and a sustained near-stop period.

### Same-interval A/B comparison

For comparability, compare the old `130227.csv` to the new `132146.csv`
over the **overlapping period** `t≈10.433–14.883 s`, including only
rows for physics steps where `brake_input>0.5` and `speed_mps<0.12`.
Each value of `sum(Fx)` below sums the four wheel rows **once** per
distinct fixed-time step; avoid summing duplicate per-wheel step totals.

| Measure | 0.0.5 before (130227) | 0.0.6 after (132146) |
|---|---:|---:|
| Near-stop physics steps in common window | 256 | 254 |
| Mean magnitude-style reported speed | 0.2816 km/h | 0.0201 km/h |
| `sum(Fx)` min | -14,774.1 N | -6,312.3 N |
| `sum(Fx)` max | +14,383.8 N | +1,187.3 N |
| 95th percentile of `abs(sum(Fx))` | 13,938.1 N | 668.4 N |
| Adjacent nontrivial (`abs(Fx)>1 N`) sign changes | 243 | 3 |

For the common stable interval `t≈11.50–14.883 s` with brake held:

| Measure | Before | After |
|---|---:|---:|
| Physics steps | 204 | 204 |
| Mean recorded speed | 0.2883 km/h | 0.0040 km/h |
| `sum(Fx)` min/max | -13,926.9 / +13,935.6 N | 0.0 / +0.7 N |
| Fx sign changes above 1 N | 203 | 0 |

**High-speed control:** Both versions have identical acceleration to
**42.96 km/h at 8 s**, and matching data while braking above
`1 m/s`: brake starts at 9 s, 79 matching high-speed-brake steps
have the same force behavior and no sign reversal. The new limiter only
activated in the near-stop regime, with **46 total physics steps**
containing at least one limited wheel (**182 wheel rows**). The
previous CSV has no limiter columns.

### Conclusion / remaining limitations

**The targeted low-speed brake-force chatter fix is accepted from real
user Play Mode telemetry in the 4W flat-asphalt StraightBrake scenario.**
In steady near-stop braking, the strong ±14 kN alternation is gone and
the speed settles practically to zero. This is observational validation,
not certification of tire physics. Existing Unity scenes/telemetry were
preserved; a new code modification is not needed for this specific result.

Do not interpret `speed_mps` as signed forward motion (it is magnitude),
or infer a full stopping-distance proof from the available fields.
The new capture ended before the nominal 28 s; this does not undermine
its 11.5–14.88 s steady-brake comparison.

**Still unverified:** `KavraPhysicsEditModeTests` Test Runner results,
slope hold/creep under brake, other friction surfaces and multiple axle
layouts, long-run oscillations, and separate Raycast/SphereCast performance.
The `SteeringStep` data still identifies natural high-roll/contact-loss
and airborne rotor overspeed; no auto-uprighting is desired or added.
Keep overall PR #2 **Draft** until broader acceptance. Issue #6 should
record the flat-asphalt A/B check as passed but keep non-tested criteria open.

The previous section's request for one after-fix StraightBrake recording
is now **fulfilled**. Do not request the same recording again.
