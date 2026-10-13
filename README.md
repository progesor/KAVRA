# KAVRA Vehicle Framework

**A modular wheeled vehicle framework for Unity.**

**State:** design/bootstrap `0.0.1` (2026-10-13). **This is not yet a vehicle driving controller.**

**Project vision:** model varied wheeled vehicles using a configurable chassis,
axle, tire, suspension, drivetrain and terrain contact architecture; preserve
simulation-quality behavior while exposing optional arcade assistance. A
reusable Unity Wizard should make configuration practical across projects.

## What is in this starter archive?

- `Packages/com.progesor.kavra`: **installable embedded Unity Package**
  containing a functional configuration/scaffold **Editor Wizard**, sample layout
  templates and structural validation. It creates kinematic, **non-drivable**
  placeholders until the M0 physics milestones are implemented.
- `docs/`: Turkish engineering and roadmap documentation, grounded in MudRunner
  developer materials and Unity's official package guidance.
- `.github/`: issue templates and PR checklist.
- `.gitignore`, `.gitattributes`, `CONTRIBUTING.md`, `CHANGELOG.md`.
- `Assets/VehiclePhysicsLab/README.md`: recommended scene/test layout.

## Recommended environment

| Area | Decision |
|---|---|
| Editor | Unity 6.3 LTS (`6000.3.x`, latest patch within this line) |
| Project template | Universal 3D (URP), for the host/laboratory project |
| Physics | Unity 3D Rigidbody/PhysX + independently designed wheel dynamics |
| Package ID | `com.progesor.kavra` |
| C# namespaces | `Progesor.Kavra`, `Progesor.Kavra.Editor` |
| Input | Input System as **host** dependency; core independent |
| Units / axes | SI units, +X right / +Y up / +Z forward |
| Initial target | Windows PC Editor development; mobile later via quality profiles |

## Install into a NEW Unity project

1. In Unity Hub: **New Project > Universal 3D** using **Unity 6.3 LTS**;
   project name `KAVRA` (or `KAVRA-Lab`).
2. Close the Editor for initial file copy.
3. Extract the contents of this ZIP **into the Unity project root** (the folder
   that already contains `Assets/`, `Packages/` and `ProjectSettings/`).
   **Merge** folders. Do not replace the entire `Packages` folder or overwrite
   Unity-generated `Packages/manifest.json`.
4. Reopen in Unity, wait for compiling, and click
   `Tools > KAVRA > Vehicle Setup Wizard`.
5. Create a `Passenger Car 4` definition, inspect the asset, and click
   `Build non-drivable scene scaffold`.
6. Save an Editor scene at `Assets/VehiclePhysicsLab/Scenes/M0_Scaffold.unity`.
7. Configure Project Settings using `docs/07_UNITY_SETUP.md`; push the project
   to GitHub with **all generated `.meta` files** committed.

**Embedded package discovery is automatic.** Do not add `com.progesor.kavra`
to `Packages/manifest.json` when its folder is already embedded under `Packages/`.

## Recommended reading order

1. `docs/00_SCOPE_AND_GOALS.md`
2. `docs/01_MUDRUNNER_RESEARCH.md`
3. `docs/02_REQUIREMENTS_AND_USE_CASES.md`
4. `docs/03_SYSTEM_ARCHITECTURE.md`
5. `docs/04_PHYSICS_MODELS.md`
6. `docs/05_TERRAIN_AND_SURFACE_INTERACTION.md`
7. `docs/06_VEHICLE_WIZARD_SPEC.md`
8. `docs/07_UNITY_SETUP.md`
9. `docs/08_ROADMAP_AND_MILESTONES.md`
10. `docs/09_VALIDATION_AND_BENCHMARKS.md`
11. `docs/10_RISKS_AND_DECISIONS.md`
12. `docs/11_REFERENCES.md`
13. `docs/12_NEXT_SESSION_HANDOVER.md`

## Out of scope for the first physics acceptance

Fully deforming rubber tire FEM, fully resolved multibody suspension linkage,
beam/node chassis destruction, deterministic online networking, sophisticated
fluid/soil solvers and independently validated industrial digital twins.

## Licensing

No software license has been selected. Please **choose a license or keep the
repository private** before assuming the code is reusable by third parties.
Do not interpret GitHub publication as automatic grant of open-source rights.

## Integrity note

The package manifests and source files are generated and statically inspected,
**not compiled or exercised in a Unity Editor** in this environment. The first
Unity compile/import pass is part of milestone M0 acceptance and may uncover
necessary compatibility fixes.
