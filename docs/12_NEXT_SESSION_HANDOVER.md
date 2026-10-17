# KAVRA — Handover 2026-10-13: M0 experimental wheel dynamics

## Repo / branches

- Repository: https://github.com/progesor/KAVRA
- Unity project version: 6000.3.18f1; embedded package `com.progesor.kavra`.
- Working branch: `feat/m0-wheel-suspension-prototype`; PR to main.
- Issue: https://github.com/progesor/KAVRA/issues/1.

## Implemented as source in M0 v0.0.2

- RaycastNonAlloc ground probe excluding own chassis.
- Simple equivalent spring + bump/rebound damper.
- Semi-implicit angular wheel/tire slip, traction ellipse, direct wheel torque,
  service and parking brake.
- VehicleSimulationRoot computes wheel forces and applies them through Rigidbody.
- Per-wheel telemetry + optional debug contact rays.
- Wizard can create dynamic prototypes while preserving kinematic previews.
- Assets/VehiclePhysicsLab generates a test scene and host keyboard/camera.
- EditMode test scripts for 3/4/6/8/20 layouts and solver sanity.

## Important: still NOT verified in Unity

This code was authored and statically reviewed via GitHub. No Unity Editor
or Play Mode runtime was available for a real compilation or physics run.
Do not report tests as green until a local Editor test has passed.

## Next acceptance path

1. Pull/checkout M0 branch (or merge PR after acceptance).
2. Open Unity and inspect Console import + compile errors.
3. Run all `KavraPhysicsEditModeTests` in Test Runner.
4. Generate the M0 lab from Tools menu, save the scene, press Play.
5. Record rest Fz sum, frame stability, 10-second driving/braking test.
6. Smoke-check 3W and 8W Wizard dynamic chassis.
7. Send errors/logs or commit confirmed scene and measured data to repo.
8. Fix findings; only then merge PR and start richer tire/contact model work.

Known limits/risks: single contact ray, uncalibrated velocity-slip tire,
automatic equal wheel spring-rate guess, non-deterministic Unity simulation,
no actual drivetrain or soil contact deformation, test keyboard is a sample
in host project and not part of reusable package.

See `docs/13_M0_FIRST_PHYSICS_SLICE.md` for API and numerical details.


## Update: M0 0.0.3 — ride-height / roll / contact reliability

User drove the prototype and reported moderate body banking, plus a wheel
visually entering ground during some turns. New source on the same PR #2
addresses likely causes with CoM-weighted springs, end-stop, anti-roll pair,
test inertia approximation, CoM gizmo, sphere-vs-ray contact mode and
bounded PhysicsLab CSV capture. Tracked at issue #3.

Re-run the lab from the Tools menu to instantiate a fresh component with
new defaults. Check EditMode tests, 20-second CSV, bottomout_m and tilt_deg.
Unity compilation and Play Mode **remain pending independent verification**.


## Update 2026-10-14: M0 v0.0.4 surface / R recovery

Issue: https://github.com/progesor/KAVRA/issues/4

Source integrated: `SurfaceZone` collider binding, `SurfaceResponse`
per-wheel friction/rolling, lab asphalt/gravel/mud/ice equal-height strips,
`VehicleManualRecovery.TryRecover`, host-only **R** key handling,
opt-in wheel-contact cast CPU duration, 90-second diagnostic CSV and
EditMode tests.

User explicitly asked for **no artificial automatic rollover prevention**.
The vehicle can roll naturally. Pressing R while nearly stopped attempts a
one-off repositioning to an upright yaw at its current horizontal location;
if any wheel has no ground, speed is high, or space is obstructed, it refuses.

Re-generate the M0 Physics Lab via Tools menu to see surface strips and new
component defaults; previous saved scene objects are not automatically
replaced. First open Unity Console and run EditMode tests, then Play Mode
with R behavior, surface transition grip and cast cost CSV. Still no actual
Unity runtime tests performed by assistant.


## Update 2026-10-14: M0.5 v0.0.5 steering & tire calibration tools

Issue #5 / Draft PR #2. Optional `TireTuningProfile` field on
WheelDefinition (null keeps existing assets working). `SteeringMode`
offers Parallel and ApproximateAckermann with no automatic roll/yaw correction.
Precomputed steering geometry used at FixedUpdate without per-wheel layouts
scanning. Bounded `TireSlipDiagnostics` feeds Inspector/HUD/CSV; still not a
calibrated tire force model.

New `Tools > KAVRA > Create M0 Calibration Run`: a flat asphalt runway,
host `VehicleLabManeuverRunner` with repeatable StraightBrake/SteeringStep/
CoastDown schedules, keyboard driving disabled but explicit R permitted.
Extended 28s CSV includes slip, actual steering angles, tire profile/stiffness
and input/maneuver labels. Interactive surface test remains separate.

Run EditMode tests and the three scripted experiments locally, compare CSV,
inspect 3W/4W/8W. Unity was not available during source authoring; avoid
merging the Draft PR until actual tests pass.


## 2026-10-14 M0.5 v0.0.6: USER TELEMETRY REVIEW

User pushed commit `d98a683` titled `TTT2` to the SAME working branch,
with four real CSVs under `docs/Telemetry/`, and generated
`M0_DynamicLab.unity`, `M0_CalibrationLab.unity`, surface .assets/.mats.
Keep these commits and files intact.

Real data: two identical scripted braking schedules, one SteeringStep, one
CoastDown. All use 4W, asphalt, Raycast, ApproximateAckermann, 60Hz.
M0 static Fz sum ≈14224.5N for 1450kg, StraightBrake reaches 42.96 km/h.
When speed falls to ~0.28 km/h at full brake, longitudinal tire force
oscillates +14.4/-14.8 kN; this is numeric chatter, not plausible static
friction. SteeringStep reaches 58.77deg roll, loses contacts, driven RPM
~3200 (unbounded M0 test actuator); NO auto anti-flip per user's wishes.
CoastDown 35.8→29.5 km/h over 14s (~0.126m/s²) near nominal rolling
resistance. See `docs/17_M05_TELEMETRY_REVIEW_2026-10-14.md`.

M0.5 v0.0.6 sources now include `LimitNearStopBrakingImpulse` and full
body-timestep impulse cap for near-stationary brake-only Fx, with new CSV
tracer columns `brake_impulse_limited`, `brake_limiter_enabled`.
New EditMode tests added. **Unity Editor not run after the patch.**
First next acceptance: compile/Test Runner, then compare a new
StraightBrake CSV with user's two existing logs. Do not ask to re-run all
four older tests, or mark PR as accepted without a new after-run.


## 2026-10-14, T3: after-fix brake A/B received — DO NOT repeat request

The user committed `c278b42` (`T3`), containing the real Unity CSV
`docs/Telemetry/kavra_m0_telemetry_20261014_132146.csv` (4W
StraightBrake, 14.883s, 893 fixed steps, 3572 wheel rows,
`brake_limiter_enabled=1`). It also updates the Unity calibration
scene. Read the appended v0.0.6 after-results in `docs/17`.

The flat-asphalt brake chatter regression is now **validated from telemetry**:
on the common near-stop window, 243 -> 3 longitudinal sum-Fx sign flips;
p95 |sum(Fx)| 13,938 -> 668 N; mean speed 0.282 -> 0.020 km/h.
At t=11.5–14.88, old run switches Fx sign 203 times; new switches
zero times and mean speed is 0.004 km/h. Above 1m/s braking and
max speed 42.96 km/h remain unchanged. There is no need to make another
source physics change solely for this successful comparison.

Remaining open: real EditMode Test Runner evidence, slope-hold tests,
different road friction/axle layouts, before/after contact mode benchmark,
airborne wheel overspeed with future powertrain. Preserve user's
committed files and keep PR #2 Draft pending broad acceptance. Never
add automatic rollover prevention; R-only manual recovery remains.


## 2026-10-14: New M1 core powertrain branch

Branch: `feat/m1-core-powertrain-foundation` based on the existing M0
prototype branch; separate Draft PR to M0 will track this milestone.
Issue: https://github.com/progesor/KAVRA/issues/7

Main priority changed by user: *build a robust general modular vehicle
framework*, stop over-investing in minor physics imperfections.

Added M1 simple engine/gearbox ScriptableObject (torque vs RPM, idle/redline,
forward/reverse/neutral, final drive), C# powertrain runtime calculating
signed wheel torques distributed across any number of driven AxleId groups.
Gear and drive source fields integrated in VehicleSimulationRoot;
M0 legacy mode default retained. Wizard creates/assigns asset and an M1
dynamic chassis. Separate host-only M1 lab generator/input HUD/CSV.
EditMode tests include 3/4/6/8/20, neutral/reverse, torque totals and
gearing compatibility.

Open next steps: compile in user's Unity 6000.3.18f1 Editor, run EditMode
tests, drive a generated M1 lab car, smoke check custom 8W in Wizard.
Full differential/clutch/engine drag/real calibration out of scope.
No automatic rollover prevention; only R recovery.


## 2026-10-14 M1.1 axle routing & demo vehicle options

Working branch `feat/m1-core-powertrain-foundation`, Draft PR #8, issue #10.
User's priority remains completing main configurable vehicle framework over
minute vehicle physics. 3D model assets tracked in backlog issue #9.

Added per-axle weight + speed-based differential torque COMMAND approximation,
kept older engine/wheel APIs, Wizard drive layout toggles and torque settings.
Separate generated M1 lab menus for 3W/4W/6x6/8x8/20W with appropriate
sample engine torque curves. Six EditMode test source cases added.

Do NOT call the approximate speed-based mode a physically locked differential.
The engine/gearbox baseline remains simple and fast, separate from M0 tire
solver. Unity Editor compiler/tests and multi-axle Play Mode verification
are still required; PR should remain Draft. No auto-rollover prevention,
only R manually recover.


## 2026-10-17 M1.2 — gearbox state and initial visual binding

Branch `feat/m1-core-powertrain-foundation`, Draft PR #8, issue #11.

New `VehicleGearboxController` per-vehicle runtime manages manual and
optional automatic forward gear changes, explicit shift duration / temporary
torque interruption, low-speed reverse safety, target vs current gear.
`VehicleSimulationRoot` uses its state before calculating axle motor
torques, exposing telemetry. Input System host toggles automatic via T,
supports manual Q/E/N and retains W/S, brake, manual R recovery.
CSV records actual/target gear, shift activity and progress.

Optional `VehicleWheelVisualRig` separates vertical travel, steer and
spin pivots. A `VehicleSuspensionVisualLink` can animate a visual shock
between endpoints. M1 generated chassis/prefabs now have primitive
wheel rigs and shock rods; old M0 scenes without rigs still animate
the old cylinder. Existing imported wheel mesh can be bound to a chosen
anchor and converted from Tools menu, no rendering dependency in UPM.

Unity Editor compilation/Test Runner and Play Mode remain unverified here.
The next tests should be **M1 lab drive + manual/auto T shift + a wheel
rig/shock preview**, not four long repeat CSV runs. No automatic rollover
prevention is ever introduced.


## 2026-10-17 M1.3: the first real articulated VehicleAssembly

Issue #12; work branch `feat/m13-vehicle-assembly-coupling` based on
`feat/m1-core-powertrain-foundation`; separate Draft PR.

Added assembly ScriptableObject, math/validator, ConfigurableJoint coupling,
logical non-Rigidbody assembly parent and vehicle membership. Host M1.3
lab creates reusable 6x6 tractor + 4W trailer assets with 2 separate
Rigidbody/VehicleSimulationRoot components and an articulated yaw joint.
Both vehicles use KAVRA suspension/tire runtime; only the tractor has
a motor; brake commands are mirrored to the trailer.

R key from the existing host input now checks membership and, for
an attached assembly, attempts an explicit *two-body* preflighted
teleport/recovery. Do not create automatic rollover prevention.
The connected root can detach the coupling explicitly. A separate
Vehicle Assembly Wizard saves a two-body prefab for other projects.

Eight assembly EditMode test scripts plus docs/21. Need actual Unity
6.3.x compile, Test Runner and towing/reversing tests before merge.
Preserve all prior M0/M1 scenes/CSV captures and the single-body
powertrain feature. 18-wheel articulated target requires additional
layouts; current demo is 10-wheel (6+4).


## 2026-10-17 M1.4 — breakable coupling, H reconnect, K stress test

Branch: `feat/m13-vehicle-assembly-coupling`; Draft PR #13.
Issue #14. User explicitly requested connect/disconnect and automatic
overload break; all implemented as the next core assembly iteration.
No automatic rollover prevention or reconnection.

VehicleCoupling now has explicit state, threshold-specified PhysX
breakForce/breakTorque, OnJointBreak callback with count/last-force,
nearby alignment/speed/yaw guards, and only explicit driver attach.
The assembly root exposes manual detach/reconnect and releases stale
trailer brakes after disconnect. Upgraded ScriptableObject version marker
preserves old assembly assets. Host: **H** attach/detach; **K** lab-only
lateral PHYSICS impulse, which does not manually remove the joint.
Existing R only restores coupled vehicles together when safe.

New `KavraHitchLifecycleEditModeTests` exercises data/attach/rules/
notification and reattach; older assembly brake test expects zero stale
inputs after detach. Actual Unity 6.3.x compilation, Test Runner and
physical OnJointBreak feedback with a real impulse remain UNVERIFIED
and should not be claimed until user actually runs Play Mode.

See `docs/22_M14_HITCH_LIFECYCLE.md`. Old scenes and telemetry are
not modified. Thresholds 150 kN/100 kNm are demos, not certified ratings.


## 2026-10-17 M1.5 ordered convoy chain

Branch `feat/m15-multitrailer-chain`, based on working user's
M1.4 `feat/m13-vehicle-assembly-coupling`, separate Draft PR to M1.4.
Issue #15. Preserve all older branches and user telemetry files.

Implemented NEW VehicleAssemblyChainDefinition (ordered references to
existing VehicleAssemblyDefinition link assets) and validator:
chain continuity, no repeated vehicle assets, 1..16 links.
VehicleAssemblyChainRoot builds independent per-link joints,
selected manual link operations and lead-prefix-only brake relay.
ChainBuilder/Wizard saves multi-body prefab, generated 6x6 powered
tractor + 4W trailer A + 4W trailer B lab has 3 Rigidbody/14W/2
hitches. J/L selects hitch; H manually toggles it; K applies physics
stress on selected link. No forced break or auto-reconnection.
Existing M1.4 two-body demo remains untouched.

VehicleManualRecovery now delegates explicit R on chain LEAD to
VehicleAssemblyChainRecovery: preflight ground, motion/clearance of
all connected-prefix vehicles, reposition group with exact shared
hitch frames, detached suffix untouched. It never runs automatically.
Nine new EditMode source tests cover validation, two joints, brake
disconnection, physical break event bookkeeping, selection and R safety.
No Unity compile/Play acceptance yet. Read docs/23.


## 2026-10-17 M1.5.1 hitch capture and PhysicsLab readability

Same branch `feat/m15-multitrailer-chain` / PR #16; task issue #17.
User stated that the original 15cm hitch catch is frustrating and
vehicle/road both gray makes motion, drift and the hitch invisible.

Raised default capture 0.15->0.50m and yaw 35->55 degrees. Version 1
M1.4 asset defaults upgrade without overwriting custom manual values.
Added pure observable `VehicleHitchAssessment` through the
same preflight path used by `VehicleCoupling.TryAttach`.

OPTIONAL host-only PhysicsLab Editor code now writes native checker
texture/materials to `Assets/VehiclePhysicsLab/Generated/`, colors
the tractor dark blue, Trailer A amber, B red, wheels graphite;
marks fixed world distances every 15m. Guide component highlights
hitch markers by selection, shows ready/near/far colors, a catch
circle and exact status. M1.3 and M1.5 generators attach the guide.
Camera speed-FOV is enabled only for new articulated labs.
Old saved lab scenes, user CSV and real wheel physics untouched.

Four regression EditMode sources created. Actual Unity 6000.3.x
compiler/Test Runner and visual acceptance pending; keep Draft.


## 2026-10-17 M1.5.1 CS0246: hitch guide test assembly

The user reported Unity CS0246 on 3 lines of
`KavraHitchLifecycleEditModeTests.cs` referencing a host
`VehicleLabHitchGuide` in predefined Assembly-CSharp.
The hitch guide source was moved, preserving its exact script GUID,
into `Assets/VehiclePhysicsLab/Scripts/HitchVisuals/` with an
autoReferenced `Progesor.Kavra.Lab.HitchVisuals.asmdef` referencing
`Progesor.Kavra.Runtime`. EditModeTests asmdef now references that
assembly explicitly. This is required because asmdef test assemblies
cannot directly reference predefined Assembly-CSharp code.

Next action: Pull M1.5 branch, let Unity compile, then run EditMode
tests and regenerate M1.5 checker/hitch lab. No runtime physics,
manual H, generated shader settings or saved scene/CSV changed.
