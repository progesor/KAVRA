# KAVRA P2.0 — Imported Model Binding (19 October 2026)

**Source implementation:** `feat/m15-multitrailer-chain`, UPM remains **v0.1.6**.
**Acceptance status:** GitHub static checks PASS; new Unity compile, NUnit,
Scene/Play Mode test on this P2 code **not yet run**. User confirmed the
previous Test Runner Undo crash is resolved.

## Goal

Turn a user-supplied FBX or an already-imported model prefab into a KAVRA
vehicle without changing physical wheel/gearbox/hitch logic or moving
original user assets. The model supplies **visuals only**. KAVRA still
owns one Rigidbody per chassis, its WheelAnchors and the physically
breakable ConfigurableJoint in articulated configurations.

P2.0 focuses on *one chassis at a time*. A later step will apply this
visual binding to each body of an existing multi-trailer assembly.

## Model authoring conventions

- Unity/local chassis **+X right, +Y up, +Z forward**, meters.
- Separate visible chassis/body mesh from individual wheel meshes.
- Give each wheel a named child Transform whose **pivot is at wheel center**.
  The source's wheel mesh rotation is retained. The physical KAVRA wheel
  mount is not rewritten.
- Example model hierarchy:

```
CarModel (Unity FBX / prefab asset)
  body_shell
  front-L       [mesh or child mesh group]
  front-R
  rear-L
  rear-R
```

The P2 binder does NOT require these literal names. The Wizard provides a
node path picker and conservative name suggestions. Manual corrections
are expected for unusual naming.

Use Unity's built-in FBX import first. GLB can be used after an *optional*
importer (e.g. Unity glTFast `com.unity.cloud.gltfast`) exposes it as
a Unity GameObject asset. Neither Unity glTFast nor Blender, Input System,
URP/HDRP, and any particular model library become KAVRA core dependencies.

## Wizard usage

1. Create/select a `VehicleDefinition` with existing wheel IDs. This can
   be 3W, 4W, 6x6, 8x8, 20W or a custom validated configuration.
2. Import the vehicle model into the **host Unity project** as an FBX /
   prefab. Inspect the asset's child nodes. Do not add model files to
   the KAVRA public source repo if their license forbids redistribution.
3. Open `Tools > KAVRA > Vehicle Setup Wizard`, then the step 5
   `Open Imported Model Binding Wizard` button; or open the model
   Wizard directly via `Tools > KAVRA > Imported Model Binding Wizard`.
4. Create a **new** `VehicleVisualBindingDefinition` asset, which keeps
   `VehicleDefinition` and the original model unchanged.
5. Assign the source GameObject asset. Adjust `ModelLocalOffset`,
   `ModelLocalEuler` and positive `ModelScale`. Mismatched horizontal
   wheel pivots (>0.5m) generate warnings rather than silently moving
   wheel anchors or changing FBX importer settings.
6. Click `Add missing wheel rows`; select a source node for each stable
   wheel ID using `Browse hierarchy` or type its relative path.
   `Suggest matches from names` is optional and requires confirmation.
7. Resolve errors: missing wheel nodes, repeated/overlapping subtrees,
   invalid model scale, missing MeshFilter/sharedMesh, unsupported
   skinned geometry, invalid VehicleDefinition etc. Unmapped wheels
   deliberately keep their procedural KAVRA preview cylinders.
8. Use `Build model-bound vehicle in scene` or
   `Save reusable model-bound prefab` (refuses to overwrite files).

## Produced hierarchy

```
KAVRA_Vehicle (Rigidbody; VehicleRoot; VehicleSimulationRoot)
  VehicleImportedVisualRoot  [references separate mapping profile]
  __CHASSIS_TEST_COLLIDER    [BoxCollider kept; Renderer hidden]
  WheelAnchors_M0
    Wheel_front-L
      VehicleWheelAnchor / VehicleWheelVisualRig
      TravelPivot
        SteeringPivot
          SpinPivot
            __KAVRA_IMPORTED_WHEEL_front-L  [renderer-only clone]
            __WHEEL_PREVIEW  [Renderer disabled for mapped wheels]
  __KAVRA_IMPORTED_BODY
    CarModel
      body_shell             [renderer-only clone]
```

The builder clones **Transform + MeshFilter + MeshRenderer** geometry,
retaining `sharedMesh` and `sharedMaterials` references to imported
source assets. It excludes mapped wheel branches from the static body,
cloning each instead under its rotating KAVRA `SpinPivot`.

The source model prefab and its settings are not edited. The generated
vehicle contains no model-source Rigidbody/Collider/MonoBehaviour:
only the existing KAVRA physics body and its original test collider.
The added `recordUndo=false` temporary prefab/export path preserves the
resolved Test Runner Undo native crash fix. Interactive scene builds
retain Undo.

## Deliberate P2.0 limitations

- Physical chassis collision remains the original temporary box, not
  geometry-fitted. Do not treat this as a production physics collider.
- MeshFilter/MeshRenderer only. SkinnedMeshRenderer, skeletal animations,
  destructible model components, tire deformation and per-mesh animations
  are rejected/not copied. Nonuniform/sheared imported transforms may
  require fixing the source transform hierarchy first.
- Wheel mesh pivot must be *at the wheel center*. Generated KAVRA
  Travel/Steering/Spin motion remains as before; no mechanical suspension
  arms/rod/differential shaft animation in this initial milestone.
- Source wheel subtrees must not overlap or be reused for two wheel IDs.
- Dynamic-wheel compatibility and one chassis for each Rigidbody are
  preserved. Chain-level imported visuals follow after one-chassis QA.
- No model vendors/licensed geometry added to the repository.

## Regression evidence and acceptance

- Existing M1.5.1: user-confirmed all tests passing and crash resolved
  after the no-Undo Editor fix. User did not attach the final XML.
- New source test fixture `KavraImportedVisualEditModeTests`: creates
  synthetic imported-like GameObject prefabs with 4W, 6W and 20W layouts,
  validates WheelId mapping, collider isolation, Mesh/Material references,
  prefab save/reimport/reload, model source immutability, missing or
  duplicate paths, partial mappings, invalid scale and alignment warning.
  Test-owned `Assets/__KAVRA_ImportedVisual_<GUID>` folders are deleted
  by TearDown; no pre-existing user assets are touched.
- The Windows EditMode runner now expects seven test suites including
  this fixture. Static CI is **not** Unity compile or Play Mode acceptance.

**Next local acceptance:** Unity 6000.3.18f1 Console 0 C# errors, all seven
EditMode suites PASS without crash. Import one real FBX with separate
wheels; verify materials, proportions, steering, wheel spin/suspension
and saved/reloaded prefab. No repeat CSV telemetry is needed unless a
runtime regression appears.

**Planned P2.1:** wheel pivot visualization, collision fitting, real
FBX model smoke acceptance, optional per-wheel spin-axis calibration.
**Planned P2.2:** suspension rods and articulated visual modules
without forcing hard render pipeline or third-party dependencies.

References:
- Unity PrefabUtility 6.3:
  https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/prefabutility
- Unity glTFast Editor import:
  https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/ImportEditor.md
