# KAVRA M1.3 — Multi-body tractor–trailer foundation

**Date:** 2026-10-17
**Issue:** https://github.com/progesor/KAVRA/issues/12
**Branch:** `feat/m13-vehicle-assembly-coupling`
**Upstream:** `feat/m1-core-powertrain-foundation` / Draft PR #8
**Acceptance:** SOURCE IMPLEMENTED; Unity 6000.3.x compile/Play Mode **not yet run**.

## Why

A 20-wheel single Rigidbody is only a wheel-layout stress test; it cannot
represent an articulated tractor–trailer. This milestone introduces two
independent physically simulated wheel/chassis assemblies connected by
one Unity ConfigurableJoint rather than visually parenting a trailer to
the tractor or pretending they have a shared velocity.

## Data contracts and topology

`VehicleAssemblyDefinition` ScriptableObject:
- Tractor: its own VehicleDefinition, possibly with SimplePowertrainDefinition.
- Trailer: another distinct VehicleDefinition (passively rolling by default).
- Local tractor hitch and local trailer nose hitch positions (SI meters).
- Pitch/roll limits; yaw remains free, linear motions locked.
- Attach error tolerance and simple towed brake-command ratio.

`VehicleAssemblyRoot` belongs to a logical parent with NO Rigidbody.
Children: **Tractor** and **Trailer**, each with its own Rigidbody,
VehicleRoot and VehicleSimulationRoot. The starter is a 6x6 powered
truck coupled to a 4W passive bogie trailer: **10 wheels and 2 bodies**,
not an 18-wheel semitrailer.

`VehicleAssemblyMember` marks both chassis for explicit assembly-safe
manual recovery and future control routing.
`VehicleAssemblyValidator` rejects missing/same-asset bodies,
invalid hitch coordinates, unsupported pitch/roll limits and invalid
underlying vehicle definitions.

## Hitch coordinate setup

Let `P_t,Q_t` be tractor position/rotation, local tractor hitch `h_t`,
and trailer initial rotation `Q_r`, local trailer hitch `h_r`.
Initialize the trailer body at:

`P_r = (P_t + Q_t*h_t) - Q_r*h_r`

Then the world hitch positions coincide within numerical tolerance:
`P_t+Q_t*h_t == P_r+Q_r*h_r`.

A significant initial hitch offset causes large joint-correction impulses
and must be rejected. `VehicleCoupling.TryAttach` checks its configured
tolerance before constructing a ConfigurableJoint.

ConfigurableJoint is on the TRAILER, connectedBody = tractor Rigidbody:
- x/y/zMotion = Locked (fixed hitch anchor translation).
- angularXMotion = Limited (pitch).
- angularYMotion = Free (yaw articulation).
- angularZMotion = Limited (roll).
- axis = local +X; secondaryAxis = local +Y.
- autoConfigureConnectedAnchor = false; both anchors explicitly authored.
- enableCollision = false for the two directly connected bodies.
- projectionMode = None by default; enabling projection can create
  nonphysical momentum/collision shortcuts and needs separate testing.

References: [Unity 6 ConfigurableJoint API](https://docs.unity.cn/6000.1/Documentation/ScriptReference/ConfigurableJoint.html)
and [Unity Configurable Joint manual](https://docs.unity.com/en-us/engine/6000.0/manual/physics-section/physics-overview/joints-section/create-configurable-joint/class-configurable-joint).

No joint drive motors, custom fifth-wheel moment or artificial
anti-rollover mechanism is added. Trailer wheel-force integration is
the same existing KAVRA ray/sphere + spring/damper + tire solver.

## Control and manual recovery

Existing host keyboard drives ONLY the tractor. Its M1 drivetrain
can shift gears, steer and brake normally. The logical assembly
relays a normalized service/parking brake command to the passive
trailer before that body's own FixedUpdate. This is a control signal,
NOT a pneumatic system with pressure delays.

When the user presses R, `VehicleManualRecovery.TryRecover` checks
`VehicleAssemblyMember.IsCoupled` and dispatches to
`VehicleAssemblyRecovery`. An attached train must never teleport
the tow vehicle ALONE while leaving the towed Rigidbody behind.

Assembly recovery:
1. Requires both rigidbodies to be moving below requested safe speed.
2. Computes an upright tractor yaw based on the current facing direction.
3. Aligns trailer to the same yaw with the hitch connection coincident.
4. Queries ground under EVERY wheel before touching either body.
5. Requires reasonable road normals and unblocked sample chassis boxes.
6. If safe, teleports BOTH, clears BOTH linear/angular velocities and
   wheel/controller transient states, then SyncTransforms exactly once.
7. If any precondition fails, returns a failure without moving a body.

This is an explicit nonphysical user service only; NEVER triggered from
normal physics, roll angle or collision. It is not a stability assistant.

## Unity lab and prefab workflow

1. Pull/check out `feat/m13-vehicle-assembly-coupling`.
2. Unity menu `Tools > KAVRA > Create M1.3 Tractor Trailer Lab Scene`.
3. Save as `Assets/VehiclePhysicsLab/Scenes/M13_TractorTrailer.unity`.
4. Press Play: W/S drive, A/D steer, Q/E shift, T auto/manual, Space
   brake, Left Shift parking and R manual whole-train recovery.
5. Observe tractor controls (left HUD) and joint yaw, alignment error,
   trailer grounded wheels and body speed (right HUD).
6. Inspect hierarchy: logical assembly root with no Rigidbody; tractor
   and trailer each has independent Rigidbody and wheel anchors.
   The ConfigurableJoint should live on the Trailer root and reference
   tractor Rigidbody as connectedBody.
7. In `Tools > KAVRA > Vehicle Assembly Wizard` drag the saved
   `Assets/VehiclePhysicsLab/Assemblies/M13_TractorTrailer.asset`.
   Click **Save Reusable Assembly Prefab** and choose unique .prefab path.
8. Drag the saved prefab into a fresh scene with ground Collider and your
   own input/camera to verify serialization. No third-party models are
   bundled; the box chassis/cylinders remain visual placeholders.

Generated assets persist under `Assets/VehiclePhysicsLab/Assemblies/`.
Re-generating a lab uses valid existing assets and NEVER silently
overwrites user-adjusted powertrain, trailer, hitch or axle settings.

## Tests and acceptance (in actual Unity)

- [ ] Editor compile & assembly dependencies pass on 6000.3.x.
- [ ] `KavraAssemblyEditModeTests` passes: distinct body count, contact
  wheel counts, joint DOFs, coincident anchors, detach, brake relay,
  validation and explicit both-body manual recovery.
- [ ] Test Runner's older KavraPhysicsEditModeTests stays green.
- [ ] On flat road, tow accelerates the trailer, steers left/right,
  brakes and reverses; hitch joint does not stretch or explode.
- [ ] Observe yaw articulation while turning, limited pitch/roll and
  occasional natural wheel lift when pushed (no artificial balancing).
- [ ] Only user R can request manual recovery; both bodies reposition
  with a near-zero hitch gap and zero velocities; reject at high speed.
- [ ] Assembly prefab round-trip preserves internal rigidbody/joint refs.
- [ ] M0 and M1 standalone scene and user's previous docs/Telemetry
  captures remain unchanged.

## Limitations / future generalization

- Initial topology is one tractor connected to one passive trailer.
  Chains of multiple trailers and powered trailers need a generalized
  VehicleAssembly graph and connector port mapping.
- No real fifth-wheel fifth-axis hardware, hydraulic air brakes,
  drum brake heat, kingpin wear, measured tractor suspension physics,
  mechanical trailer axle lift or overrun brakes.
- Rear trailers pivot according to genuine Unity joint dynamics,
  but sample geometry is still primitive; externally sourced models
  are a separate low-priority backlog task (#9).
- Rigidbody solver iteration/mass-ratio tuning and projection should
  be based on real Unity profiler and Play Mode stability data, not
  guessed in advance.
- Hitch preflight/assembly manual recovery uses basic box-collider
  clearance and level-ground wheel probes; complex imported meshes,
  large shifts or underpasses need improved scene query policies.

**Next core milestone:** grow the assembly from a fixed tractor/trailer
pair into configurable articulated graph, and expose attach/detach
controls and compound prefab/module binding. Do not stall this progress
on microscopic tire/suspension tweaks.


## 2026-10-17 Unity compiler fix: ConfigurableJoint is not Behaviour

In Unity 6000.3.x, `ConfigurableJoint : Joint : Component`, **not**
`Behaviour`, so it has **no `enabled` property**. Removed the two incorrect
uses in `VehicleCoupling.IsAttached` and `VehicleCoupling.Detach`.
`IsAttached` now checks that a live joint has a connected Rigidbody.
`Detach()` clears the C# reference and destroys the actual joint component
(`Destroy` in Play Mode, `DestroyImmediate` only in Edit Mode).
Importantly, it must **NOT** set `connectedBody = null`: Unity treats
that as a WORLD-ANCHORED joint rather than disconnecting a constraint.

EditMode regression now also asserts that the ConfigurableJoint is
removed when detaching, and that a fresh attach/detach is possible.
Actual Unity compilation/Test Runner and physics acceptance after this
source fix remain the user's next check; do not declare them passed.

Unity references:
- https://docs.unity.cn/6000.7/Documentation/ScriptReference/ConfigurableJoint.html
- https://docs.unity3d.com/cn/current/ScriptReference/Joint-connectedBody.html


## M1.4 extension (2026-10-17)

M1.3's fixed sample joint has been extended with actual driver-commanded
unhitch/rehitch and configurable PhysX overload break behavior. See
`docs/22_M14_HITCH_LIFECYCLE.md`. The M1.3 scene-generation menu is
retained; M1.4 adds H (manual coupling) and K (lab-only physical load).
No automatic reconnection, no fake damage-state change when stressed,
no automatic rollover prevention.
