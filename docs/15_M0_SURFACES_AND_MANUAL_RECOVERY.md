# M0 v0.0.4 — Surface response / manual upright recovery / contact A-B

Updated: 2026-10-14
Repo: https://github.com/progesor/KAVRA
PR: https://github.com/progesor/KAVRA/pull/2 (draft until Unity acceptance)
Issue: https://github.com/progesor/KAVRA/issues/4

## Scope

The package remains a portable UnityEngine-only embedded UPM package.
No hard dependency on URP, Input System or purchased vehicle assets.
All keyboard, camera, editor-generated material assets and telemetry CSV
remain in `Assets/VehiclePhysicsLab`, not in the core package.

### Surface response

Assign `SurfaceZone.Profile` on a road Collider or an ancestor.
At each wheel contact:

- Read `SurfaceProfile.LongitudinalGripMultiplier`,
  `LateralGripMultiplier` and `RollingResistanceMultiplier`.
- Multiply each of the existing baseline tire muX, muY and rolling torque
  coefficient; clamp individual multipliers to nonnegative.
- Feed those values into the existing combined-slip / rotating wheel model.
- No profile (or no `SurfaceZone`) means multipliers of **1**, retaining
  the original driving behavior.
- `Softness` / `Wetness` are **not used** to modify traction again. They
  remain metadata for future persistent soil deformation and sinkage.
- Contact remains one ray or sphere sweep with ray fallback; **not** a true
  tire footprint or pressure distribution model.

The host lab creates same-height, adjacent test strips in longitudinal +Z
direction to reduce geometric confounds. Approximate demonstration values
are NOT measured real-world tire-road coefficients:

| Profile | Longitudinal multiplier | Lateral multiplier | Rolling multiplier |
|---|---:|---:|---:|
| Asphalt | 1.00 | 1.00 | 1.00 |
| Gravel | 0.66 | 0.60 | 1.75 |
| Mud | 0.38 | 0.32 | 4.50 |
| Ice | 0.14 | 0.12 | 0.75 |

All profiles are editable ScriptableObject assets placed in
`Assets/VehiclePhysicsLab/Surfaces/`. Asset generation never overwrites a
valid existing preset, so changes persist across lab regenerations.

### Manual recovery (R)

**No auto-stabilization, no yaw clamp, no invisible righting force.**
When the sample host receives an R key-down, it invokes
`VehicleManualRecovery.TryRecover(simulation)`. The physics solver NEVER
calls this automatically.

Recovery rejects requests when:
- The simulation has no valid Rigidbody / WheelDefinition.
- Rigidbody linear speed exceeds 1.75 m/s (~6.3 km/h).
- Any wheel footprint has no ground within its downward query.
- The supporting ground is too steep.
- The destination overlaps a blocking Collider according to the M0 test
  box check. (More advanced compound chassis need a generalized check.)

When accepted, preserve horizontal (X/Z) position and heading, remove
roll/pitch, place the chassis above all wheel ground samples at a small
free-droop clearance, zero Rigidbody linear/angular velocities, clear
wheel RPM/suspension history and input torque, and return control to physics.

This is an intentionally nonphysical **user command** to escape a rollover.
It is not a continuous driving assistance and has no effect on normal turns.

### Contact-query diagnostics

`VehicleSimulationRoot.MeasureContactQueryCost` (off by default; on in newly
generated Lab scenes) uses Stopwatch to sum wheel cast call durations within
each FixedUpdate. Inspector/HUD show total **microseconds/step**. CSV repeats
the step total per wheel row, so do not accidentally sum repeated readings
into a vehicle-wide figure. The timer has its own overhead; use Unity Profiler
for proper performance conclusions. Run identical vehicle/scene/route with
Raycast versus SphereCastWithRayFallback and compare typical rather than
individual worst-frame values.

`VehicleLabTelemetryRecorder.RecordSeconds` now defaults to 90 seconds.
CSV is under `Application.persistentDataPath` and its location is logged
when collection stops/Play exits. Regenerate the scene to get updated host
components and new road assets.

## Local Unity test plan

1. Pull the M0 feature branch, allow Unity 6000.3.18f1 to import/compile.
2. Run `Window > General > Test Runner > EditMode`.
3. `Tools > KAVRA > Create M0 Physics Lab Scene`, save the new scene, Play.
4. Drive in +Z across asphalt/gravel/mud/ice. Observe HUD `Surface:`,
   wheel-level friction and rolling values in Inspector.
5. At low speed, roll the car or rotate the body manually during an isolated
   test, then press **R**; it should return upright **once**. Recovery fails
   gracefully when driving too fast or unsupported over empty space.
6. Set `MeasureContactQueryCost=true` and compare Ray vs Sphere cast from
   similar states. Repeat after warming caches; include Editor hardware and
   selected contact mode with recorded CSV.
7. Confirm a scene without SurfaceZone still uses the original coefficients.
8. Check 3W/4W/8W/20W assets: no assumption of exactly four wheels.

**Unverified status:** code/static structural checks only, no real Unity
compiler or Play Mode execution by assistant. Do not merge PR #2 until
Unity acceptance criteria are met.
