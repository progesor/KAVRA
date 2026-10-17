# KAVRA M1.5.1 — Hitch usability and contrast PhysicsLab

**Date:** 2026-10-17
**Issue:** https://github.com/progesor/KAVRA/issues/17
**Working branch:** `feat/m15-multitrailer-chain`
**Draft PR:** https://github.com/progesor/KAVRA/pull/16
**UPM package version:** 0.1.6 (label M1.5.1)

## Problem and solution

The user confirmed M1.4's functional physical attach/detach/break
mechanism, but practical manual H attachment felt too strict. Because
the road and all vehicles were plain grey, motion, wheel slide,
backwards rolling and the tiny hitch positions were unclear.

This pass changes only safe capture defaults, exposes existing hitch
constraints to a read-only UI diagnostic, and adds visual lab-only
assets. It does NOT introduce hidden stabilization, auto-coupling,
permanent magnets or nonphysical road-wheel traction.

### Hitch capture defaults

| Item | Before | Now |
|---|---:|---:|
| Manual hitch world-point distance | 0.15 m | **0.50 m** |
| Heading/yaw difference | 35 deg | **55 deg** |
| Max hitch-point relative speed | 1.25 m/s | unchanged |
| Max each Rigidbody absolute speed | 1.75 m/s | unchanged |
| Real breakForce/breakTorque | per asset | unchanged |

These are test-friendly defaults, not real fifth-wheel geometry. Unity
preflight still refuses unsafe velocity, distance or orientation.
When H is **explicitly pressed** and checks pass, the trailer may be
snapped by no more than the configured capture radius to make hitch
points exactly coincide, just before creating the ConfigurableJoint.
There is **no automatic nearby attachment or magic continuous pull**.

The asset's private `hitchSettingsVersion` is 2; M1.3 older v0
migrates through v1; an M1.4 v1 asset upgrades default values ONLY
if they are exactly 0.15m / 35deg. A custom non-default capture
or yaw is not overwritten. Existing break settings and an explicit
0 = infinite/unbreakable setting remain unchanged.

`VehicleCoupling.AssessAttach(tractor, definition,
out VehicleHitchAssessment)` runs the exact same preflight as H but
never moves either body. It returns distance, projected heading
error, relative hitch-point velocity, max chassis-body speed and
the reason/result. `TryAttach` reuses the result so UI and actual
physics behave consistently. HUD wording: READY, TOO FAR,
BAD ANGLE, TOO FAST, WAIT or CONNECTED.

### Generated scene: distinct visual layers

`Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene`
and the older `Create M1.3 Tractor Trailer Lab Scene` both
decorate freshly GENERATED scenes with:
- A 128x128 RGBA checker texture using 8x8 large squares (3m/world cell
  when tiled on the cube), dark blue-grey vs light cool grey.
- Native generated Unity `.asset` texture and `.mat` materials:
  `Assets/VehiclePhysicsLab/Generated/` (not shipped as texture images).
- Center and roadside stationary reference stripes every **15m**,
  with no colliders so they cannot alter the vehicle dynamics.
- Matte steel blue tractor, burnt orange Trailer A, signal-red
  Trailer B, graphite tires and silver suspension previews.
- Bright cyan tractor hitch marker and amber/yellow trailer marker,
  sized far larger than the original 0.13m preview sphere.
- Runtime `VehicleLabHitchGuide` updates the selected hitch's marker
  sizes and a real LineRenderer bridge across the two hitch endpoints.
  A circular capture ring makes the 0.50m threshold visible; selected
  link highlighted by J/L in M1.5. Status is tinted green/amber/red
  depending on readiness and cyan when attached.
- Subtle optional chase-camera FOV increase with speed, capped at
  6–7 degrees by ~90 km/h (not applied to older M0 labs).

The material helper selects URP Lit, HDRP Lit or built-in Standard
depending on `GraphicsSettings.currentRenderPipeline` and avoids
overwriting any existing native material/texture at its filenames.
Only primitive laboratory renderers with known names are colored;
it does not recolor future imported real vehicle meshes.
The road still uses the same original BoxCollider and layer: it is
a purely visual change.

### How to test quickly

1. In GitHub Desktop check out and Pull
   `feat/m15-multitrailer-chain`.
2. Unity: `Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene`.
   The menu reuses existing M15_* vehicle/engine/link assets, and
   generates only the new scene hierarchy/styling. Save the new
   scene at `Assets/VehiclePhysicsLab/Scenes/M15_TwoTrailerChain.unity`
   (keep any earlier custom scene copy separately if desired).
3. Expect a dark/light checker ground, cream reference marks, blue
   tractor, orange/red trailers, bright cyan/yellow hitch markers.
4. Press Play, J/L to select a hitch, H to separate, gently back up
   to its yellow/cyan markers and watch the capture circle.
5. READY (green): press H to reconnect. TOO FAR: approach closer.
   BAD ANGLE: straighten before H. TOO FAST: stop your chassis.
6. Drive forward and reverse; use checker-cell/15m markings to
   distinguish actual ground displacement from wheel spin.
7. Optionally test the old single-trailer generator; it should
   receive the SAME visual improvements. Verify M0 untouched.
8. EditMode Test Runner: new M151 hitch migration/capture tests and
   older M13/M14/M15 regression tests should pass.

### Limitations and verification

- Demo colors and checker texture improve testing, not production
  artwork. Real FBX models and mechanical suspensions still belong
  to low-priority issue #9.
- The optional one-shot 0.5m capture snap is a usability assist after
  a deliberate H command, not a physically modeled trailer parking
  hitch mechanism. The radius remains tunable in every link asset.
- GUI/LineRenderer marker visibility depends on the project's render
  pipeline; C# source has been statically inspected but actual Unity
  6000.3.x Editor compile/render/Play Mode is **not yet run** here.
- No auto-rollover correction. R is still manually initiated, and
  M1.5 chain-only lead recovery unchanged.
