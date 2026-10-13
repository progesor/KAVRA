# KAVRA Vehicle Framework — embedded Unity package

**Version:** `0.0.1` · **State:** M0 design/bootstrap, no driving physics implemented.

This folder is an **embedded Unity Package Manager (UPM) package**. Place it under
`<UnityProject>/Packages/com.progesor.kavra/` with the included `package.json`.
Unity automatically discovers embedded packages: do **not** add a duplicate
`com.progesor.kavra` entry to your project's `Packages/manifest.json`.

## What works now

- A Unity `VehicleDefinition` ScriptableObject with an arbitrary number of wheels,
  grouped into axles by ID (no hard-coded 4-wheel assumption).
- `SurfaceProfile` ScriptableObject (data-only placeholder).
- `Tools > KAVRA > Vehicle Setup Wizard` Editor window.
- Starter layouts: 3-wheel delta, 4-wheel car, 6×6, 8×8,
  and a 20-wheel **single-chassis** structural stress test.
- Structural validation: missing/duplicate identifiers and invalid dimensions.
- Generates a visible scene **scaffold** with a kinematic Rigidbody,
  primitive chassis preview, wheel anchor transforms and gizmos.

## What does NOT work yet

- No tire contact, suspension forces, drivetrain, brakes, steering or movement.
- No real mud deformation, articulated tractor-trailer, physical damage or gameplay input.
- A 20-wheel single chassis is not an 18-wheel articulated truck.
- No Editor compilation or Play Mode verification was possible outside Unity.

## Quick use

1. Create a Unity **6.3 LTS (6000.3.x)** `Universal 3D` project.
2. Extract the full starter ZIP into the **root** of that Unity project,
   merging but not replacing its existing `Packages/` directory.
3. Return to Unity and wait for package import/compilation.
4. Choose `Tools > KAVRA > Vehicle Setup Wizard`.
5. Pick `Passenger Car 4`, click `Create VehicleDefinition Asset`.
6. Click `Build non-drivable scene scaffold` and inspect wheel anchors.
7. Save the scene under `Assets/VehiclePhysicsLab/Scenes/` and commit `.meta` files.

For the full project plan and technical decisions see `../../docs/` at repo root.
