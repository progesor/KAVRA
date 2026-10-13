# M0 — Suspension limits, CoM, tire/road contact

Date: 2026-10-13
Track: issue #3, Draft PR #2, experimental UPM 0.0.3.

## Symptom

Prototype turns cause some expected body roll, but a wheel can penetrate
ground when stroke is exhausted. Not a reason to lock vehicle orientation;
the right approach is controllable forces, measured contact and useful visual
feedback.

## Code boundaries

- `SuspensionSolver`: spring + unequal bump/rebound damping + progressive
  bump-stop, no force pulling a wheel toward the road.
- `VehicleStaticLoadEstimator`: distributes initial static support across
  arbitrary wheel layouts while attempting sum of forces and CoM moments.
  Invalid CoM footprint returns a warning and best-effort nonnegative shares.
- `AntiRollSolver`: symmetric left/right load transfer, conserving sum.
  It never applies negative wheel support.
- `VehicleSimulationRoot`: 3-phase calculation:
  (1) contact + springs, (2) axle anti-roll, (3) slip and AddForceAtPosition.
  Temporary chassis inertia estimate uses wheelbase/track/body-height.
  Engineered vehicles will need measured mass tensor eventually.
- `WheelContactProbe`: raised-origin ray and optional sphere cast.
  Sphere hit.distance is center sweep distance, converted to an equivalent
  mount-to-ground ray length. A ray fallback handles initial overlaps.
- `VehicleLabTelemetryRecorder` host-only: CSV in persistentDataPath.

## Practical parameters

- `TargetStaticCompressionFraction` 0.5 default, i.e. nominal static half-travel.
- `BumpStopStartFraction` 0.85; `BumpStopRateMultiplier` 4.
- `AntiRollRateFraction` 0.35; zero disables extra coupling.
- `EstimateChassisInertia` true for the low-detail test cube only.
- `BodyHeightToWheelRadiusMultiplier` 3.6, approximate lab geometry.
- `ContactMode` Raycast (baseline) vs SphereCastWithRayFallback (comparison).
- The visual center may move slightly above the nominal mount on bottom-out
  to reduce obvious clipping, with capped offset. This does not alter the
  true suspension forces. `BottomOutMeters` remains visible and is the
  engineering signal to investigate.

## Regression checklist (Unity required)

- [ ] All EditMode tests pass, including 3/4/6/8/20 static fractions.
- [ ] Project compiles with no missing .meta GUIDs / broken assembly references.
- [ ] Every wheel supports approximately its CoM-derived static share on
      level ground, after settling; summed vertical support ~ m*g.
- [ ] Repeated left/right turns do not cause permanently buried wheels or
      runaway chassis oscillations; occasional natural bank is expected.
- [ ] BumpStopForceN increases at full stroke; no suction.
- [ ] Both contact modes detect flat asphalt at identical ride height.
- [ ] Raised-origin query can recover ground at slight mount penetration.
- [ ] 3W and 8W layouts stay supported and steer/brake (basic sample).
- [ ] Compare generated 20-second CSV: baseline ray vs sphere test (same
      input path / config). Record max tilt, max bottomout, missing contacts.
- [ ] Watch performance for 20-wheel stress case; avoid assuming a
      20-wheel single chassis is an 18-wheeler trailer model.

## Safety / fidelity caveat

SphereCast is NOT a full rolling circle plus deforming tire. It can miss
colliders overlapped at start and can report a sweep/contact normal that
does not equal the geometric road normal near edges. Ray fallback mitigates
some, not all, cases. Equal and opposite axle load transfer is a simplified
bar, not joint-resolved suspension. The numeric defaults are engineering
starting estimates, not measured production tire/spring data.

No Unity Editor or Play Mode executable test was available while authoring
this change. User must repeat the Editor acceptance and can share Console or
CSV findings to calibrate actual dynamics.
