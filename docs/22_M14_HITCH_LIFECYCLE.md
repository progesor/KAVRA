# KAVRA M1.4 — breakable, manually connectable tractor–trailer hitch

**Date:** 2026-10-17
**Issue:** https://github.com/progesor/KAVRA/issues/14
**Branch:** `feat/m13-vehicle-assembly-coupling`
**PR:** https://github.com/progesor/KAVRA/pull/13 (Draft)
**Status:** Source implementation complete, actual Unity physics test pending.

## Why and what changes

The M1.3 tractor and trailer were genuinely distinct Rigidbody objects
but the joint always stayed installed until C# explicitly destroyed it.
The new M1.4 hitch exposes a realistic **lifecycle**:

1. When user presses H while attached AND both bodies are near-stopped,
   VehicleAssemblyRoot.Detach removes the joint (Unity destroys it at
   end-of-frame in Play Mode). Trailer brake commands are cleared.
2. User moves the powered tractor independently, backs it toward the
   detached trailer's nose and presses H. Validation requires two
   vehicle bodies moving slower than configured maximum,
   hitch-point separation within tolerance, aligned headings and
   relative velocity at the hitch below configured limit.
3. If `AlignHitchesOnManualAttach` is true and the preflight passes,
   the trailer body may move by ONLY the small permitted hitch gap
   to align joint attachment points exactly before constraint creation.
   It is a deliberate H-key positioning assist, not ongoing alignment.
   In the current simple scene the maximum gap is 0.15m by default.
4. When attached, Unity's PhysX solver applies the constraints. If
   its force/torque surpasses break thresholds, the engine automatically
   destroys the ConfigurableJoint and calls OnJointBreak(float) on
   scripts on the trailer GameObject.
5. The hitch records a Broken state, break counter and reported force.
   No script auto-reattaches it. The driver must carefully approach and
   press H again. The trailer independently simulates its wheels.
6. Detached/broken trailer no longer inherits the tractor's previously
   requested service/parking brake value.

No vehicle-righting forces, road grip overrides or invisible stability
helpers are added. User-requested R recovery remains manual and, only
while connected, acts on both bodies together. After break or detach,
R can recover the active tractor alone.

## Data (VehicleAssemblyDefinition)

| Parameter | Initial value | Meaning |
|---|---:|---|
| MaximumAttachErrorMeters | 0.15m | Max hitch-point gap to accept H |
| MaximumAttachYawDegrees | 35 deg | Max ground-plane heading difference |
| MaximumAttachRelativeSpeedMps | 1.25 m/s | Max hitch-point relative velocity |
| MaximumManualCouplingBodySpeedMps | 1.75 m/s | Max each-body speed for manual attach/detach |
| AlignHitchesOnManualAttach | true | One-time trailer snap within allowed gap |
| BreakForceNewtons | 150000 N | Unity solver joint force break threshold |
| BreakTorqueNewtonMeters | 100000 Nm | Solver torque break threshold (limited/locked angular axes) |

Both thresholds equal to **0** explicitly mean **unbreakable for that
component**, via `Mathf.Infinity`; do not accidentally set the physical
joint's breakForce to zero. M1.3 asset upgrade code initializes new fields
when opening an old serialized asset without overwriting a newer user's
explicit zero thresholds.

These are illustrative test values, not real automotive fifth wheel
specifications. A low `BreakForceNewtons` might disconnect the trailer
from normal road forces or small terrain bumps; adjust only with real
Unity Play Mode data.

## Implementation and Unity API

`VehicleCoupling` constructs a `ConfigurableJoint` on the TRAILER root,
connectedBody = tractor Rigidbody, and configures:

- Linear XYZ = Locked, relative yaw = Free.
- Pitch/roll = Limited, projection disabled by default.
- `joint.breakForce = BreakForceNewtons` or `Mathf.Infinity`.
- `joint.breakTorque = BreakTorqueNewtonMeters` or `Mathf.Infinity`.

Unity invokes `MonoBehaviour.OnJointBreak(float)` when its physics
solver actually breaks the joint. The engine removes the joint itself;
`VehicleCoupling` records `Broken` status, reported force and count.
The callback is not synthesized by collision angle checks. The yaw
axis is free, so torque about that axis alone does not break the joint
via angular restriction; force along locked translations and torque
around limited axes can.

Manual Detach destroys the joint Component. It never writes
`joint.connectedBody = null`: that would anchor to the WORLD, not
unhitch. Also, `ConfigurableJoint` has no `enabled` member.
In Play Mode Destroy is deferred until frame end, so a same-frame H
reattach is rejected with JointRemovalPending instead of silently
creating a duplicate joint.

The owning `VehicleAssemblyRoot` relays brake inputs only while
connected and clears previous brake/parking commands on detachment.
The tractor can drive away and the trailer remains simulated as its own
Rigidbody with four wheel contacts.

## Unity verification — minimal

1. GitHub Desktop: update `feat/m13-vehicle-assembly-coupling` branch.
2. Open Unity 6000.3.x, run Test Runner/EditMode, inspect
   `KavraAssemblyEditModeTests` and
   `KavraHitchLifecycleEditModeTests`.
3. Generate M1.3 lab:
   `Tools > KAVRA > Create M1.3 Tractor Trailer Lab Scene`.
4. Play: rig starts coupled. Brake to near 0 and press **H** to detach.
   Hitch status turns Detached; trailer should remain physically
   independent (no phantom WORLD joint).
5. Back the tractor toward the trailer hitch markers while nearly
   stopped. Press **H** with alignment inside tolerance. Confirm Attached
   with near-zero hitch gap. Pressing H while too distant, facing far
   off-axis or travelling fast should be refused; the left HUD shows why.
6. Optional break diagnostic: while attached press **K**. Lab HUD
   applies a strong lateral **Rigidbody impulse** to the trailer, not
   a forced Detach call. Observe ConfigurableJoint's actual solver
   thresholds and `OnJointBreak` log/state. To make this easier in
   initial experiments, set lower breakForce/torque in
   `Assets/VehiclePhysicsLab/Assemblies/M13_TractorTrailer.asset`,
   then rebuild the lab or manually disconnect/reconnect so a new
   joint reads the updated settings. Use the K force only in the
   lab, not in your game input.
7. After breaking, return both hitch markers to alignment and press H:
   reconnect explicitly. Observe break count persists.
8. Verify service/parking brake values clear after uncoupling.
9. Verify manual R works for both bodies only when connected,
   and doesn't automatically fix a rollover.
10. Save an assembly prefab, reload in a different test scene and
    verify serialized joint/marker/body refs.
11. Test Real PhysX break thresholds under varying mass ratio
    and fixed time step before choosing production-safe values.

## Verification boundary

EditMode test sources exercise breakForce/torque assignment, disabled
break Infinity, attach distance/yaw/speed guard, detach/reattach,
small snap, brake-release, manual speed guards and a *simulated*
OnJointBreak callback. A simulated callback tests bookkeeping but does
**not prove Unity breaks the joint physically**. Only the real Play
Mode K impulse or another actual overload can confirm that.

The joint lifecycle does not constitute damage modeling, a real fifth
wheel locking bar, hinge wear, pneumatic brakes or automatic winch.
No topology graph or multiple trailers is implemented yet. All M0
and standalone M1 scenes remain unchanged; the KAVRA runtime UPM
still needs only UnityEngine.

Unity references:
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/joint
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/joint/breakforce
- https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/joint/onjointbreak
