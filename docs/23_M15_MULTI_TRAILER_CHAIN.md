# KAVRA M1.5 — multi-trailer chain foundation

**Date:** 2026-10-17
**Issue:** https://github.com/progesor/KAVRA/issues/15
**Working branch:** `feat/m15-multitrailer-chain`
**Base:** `feat/m13-vehicle-assembly-coupling` (M1.4)
**Unity:** 6000.3.x source targeted, actual Editor acceptance pending.

## Scope

KAVRA now has an opt-in **linear multi-body chain**. It is not a
single-Rigidbody trick or an arbitrary branching graph. It extends
the user's working v0.1.4 two-body hitch without replacing that
module or changing older saved scenes.

`VehicleAssemblyChainDefinition`: list of 1..16
`VehicleAssemblyDefinition` link assets. Every link owns its
Tractor/Tailer VehicleDefinitions, local hitch poses, joint break
limits and trailer service/parking brake multiplier. For link index
i>0, link[i].Tractor must be link[i-1].Trailer. The validator rejects
missing definitions, discontinuities and repeated chassis asset
references/cycles.

`VehicleAssemblyChainRoot` is a logical parent with NO Rigidbody.
For N links it references N+1 VehicleRoot objects and N separate
VehicleCoupling components. A coupling belongs to its downstream
trailer Rigidbody. Coupling ownership, physical OnJointBreak,
manual safe approach and break force settings all reuse M1.4 code.

## Connection lifecycle

`SelectLink(index)` or `SelectRelativeLink(delta)` chooses the
driver's current hitch. `TryAttachLink(index)` and
`DetachLink(index)` use the existing safety checks for distance,
yaw, speed and physical joint removal. Selecting/changing hitch
does not attach anything automatically.

When link k is broken or manually detached, other still-connected
edges remain physically simulated. Commands from the LEAD travel
only as far as the first continuously connected prefix. All
downstream trailers stop inheriting the tractor's brakes; any
old mirrored brake/park command is cleared. No unrequested
physics snap, reattachment or scripted break.

## R-only manual recovery

For a user's R request from the lead tractor, route to the
`VehicleAssemblyChainRecovery` helper rather than the old
one-body teleport, which could tear still-connected joints.

Compute the number of vehicles still joined to the LEAD.
Preflight every one: Rigidbody must be present and slow,
VehicleDefinition and all wheels must be valid, road probes
must find reasonably flat ground, and tentative chassis
bounding boxes must not overlap unrelated obstacles. Then
place the connected prefix upright with a shared yaw and
vertical lift preserving the exact world-space hitch points.
Every detached tail is left in its own position and rotation;
a still-connected detached pair remains attached to each
other. R is NEVER called by physics integration or angle
detection — no automatic rollover prevention.

The first recovery routine assumes KAVRA's simple box chassis
and wheel probes. For later imported compound colliders,
bridges and complex terrain, a richer clearance-provider
interface will be needed.

## New editor workflow

`Tools > KAVRA > Vehicle Chain Wizard` creates a chain asset,
displays its ordered link list and validator findings, builds
a scene hierarchy or saves a multi-Rigidbody prefab with
internal ConfigurableJoint references.

The M1.5 PhysicsLab menu:
`Tools > KAVRA > Create M1.5 Two-Trailer Lab Scene`.
It creates/reuses (without overwriting valid user assets):
- A 6x6 powered lead truck and a separate example diesel powertrain
- A 4W passive Trailer A definition
- A second distinct 4W passive Trailer B definition
- Link 0: truck → Trailer A, link 1: Trailer A → Trailer B
- One chain definition linking these two link assets
- A long ground slab, follow camera, HUD and standard tractor input

Total: **14 physical wheel states**, **3 independent Rigidbodies**,
**2 actual breakable ConfigurableJoints**. An actual 18-wheel semitrailer,
air-brake plumbing and imported FBX models are out of scope.

### Test controls

W/S drive/reverse, A/D steer, Q/E manual shift, T automatic/manual,
Space service brake, Left Shift park, R explicit manual recovery.
J/L selects hitch 0 or 1, H manually detaches/attaches that selected
hitch when alignment and speed permit. K applies a real lateral
Rigidbody impulse to the selected trailer to challenge its hitch;
PhysX alone decides if it breaks. HUD indicates status for each
link and break count.

## Acceptance (real Unity)

- [ ] Unity 6000.3.18f1 compiles the runtime/editor/host.
- [ ] `KavraAssemblyChainEditModeTests` and older suites PASS.
- [ ] Three distinct Rigidbody objects and two connected joints
      generated, all wheel anchors recognized.
- [ ] Towing, reversing and cornering the 14W train works without
      unexpectedly exploding constraints.
- [ ] J/L selection and H link attach/detach respects all M1.4 checks.
- [ ] K physical overload only breaks the stressed joint, no fake
      break or automatic repair. Each link maintains its own state.
- [ ] Brake propagation follows connected lead prefix; detached
      suffix loses stale commands.
- [ ] R recovers only connected prefix and refuses when any connected
      body is too fast or ground/clearance unsafe.
- [ ] Prefab roundtrip preserves N Rigidbodies, linked references.
- [ ] Existing M1.4 lab, M0 test scenes and user's docs/Telemetry
      files unchanged.

This is source work and static inspection only; actual physics
stability and CPU/performance values require the user's Unity runs.

## Follow-up

Next broaden the attachment system toward **ports and branching graph**,
including rear-module identity/selection, power/brake line interfaces,
more concrete trailer configurations and imported model binding.
Avoid low-level tire/anti-roll tuning unless blocking the core.
