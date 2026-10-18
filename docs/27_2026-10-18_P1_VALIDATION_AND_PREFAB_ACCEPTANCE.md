# KAVRA P1 — Configuration / prefab acceptance (18 October 2026)

**Status:** Source committed; Unity Editor and actual Play Mode acceptance
still pending. Active branch: `feat/m15-multitrailer-chain`; package v0.1.6.
This continues [docs/26](26_P1_COMMON_ASSEMBLY_CONTROL_API.md) and
does NOT override open [issue #17](https://github.com/progesor/KAVRA/issues/17).

## Why this milestone

A Unity inspector, imported asset, or text/YAML edit can introduce NaN or
Infinity values that ordinary `<= 0` checks do not reject. Those values
would otherwise flow into Rigidbody mass, wheel mount geometry, steering
and drivetrain parameters. The `VehicleConfigurationValidator` now
rejects non-finite values and malformed stable component IDs *before*
builders create physical components. Validation remains pure: it never
auto-fixes a user's serialized wheel/axle IDs, definition assets or
powertrain. Assembly and chain validators propagate underlying errors.

Supported original 3W/4W/6x6/8x8/20W layouts remain untouched. The
existing assembly hitch migration is unchanged. M0 assets with no
powertrain are intentionally still valid.

## New Unity EditMode test sources

- `KavraDefinitionValidationEditModeTests`: legacy presets accepted,
  NaN/Infinity mass, CoM, wheel dimensions and mounts rejected;
  invalid routing enums / whitespace IDs rejected; invalid engine/gear
  values rejected; errors flow through two-body and chain validators.
- `KavraPrefabRoundtripEditModeTests`: makes **unique temporary**
  `Assets/__KAVRA_Roundtrip_<GUID>` folders; saves ScriptableObjects
  for 4W and 20W powered cars, 6x6 + 4W M1.4, 6x6 + 4W + 4W M1.5;
  calls `PrefabUtility.SaveAsPrefabAsset`, forces an import, and
  reopens through `PrefabUtility.LoadPrefabContents`.
  Tests check wheel counts and wheel ID refs, ScriptableObject refs,
  count of Rigidbodies, serialized ConfigurableJoints, connectedBody
  references and assembly control link states. Teardown destroys only
  the generated scene objects and its own disposable folder.

The test measures whether Unity actually preserves the prefab's
serialized references. It does not simulate runtime physics or
certify joint stress under PhysX.

## One-command Windows Unity acceptance

Close the open GUI Editor instance for the project, then run:

```powershell
git switch feat/m15-multitrailer-chain
git pull --ff-only origin feat/m15-multitrailer-chain
.\scripts\Run-KavraUnityEditMode.ps1
```

If Unity is outside the default Unity Hub directory:

```powershell
.\scripts\Run-KavraUnityEditMode.ps1 -UnityEditorPath "D:\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe"
```

Target is exactly Unity **6000.3.18f1**. Script runs the
`Progesor.Kavra.Lab.EditModeTests` assembly and requires the four
existing suites PLUS both new suites. XML failures, no tests, missing
suite, Unity error exit code or no XML is a **FAIL**, not a false PASS.
Result XML and Editor logs are in a fresh Windows TEMP directory and
must be checked before claiming acceptance.

The GitHub Actions `source-only preflight` only parses source and
PowerShell syntax; it cannot execute Unity Editor, NUnit or PhysX.
The host still needs Play Mode checking of checker/markers, 0.4–0.5m
H capture, high-speed/yaw refusal, J/L selection, K **real joint**
break and manual R recovery of the connected lead group. Old M0/M1.4
scenes should be opened to spot regressions; older CSV need no rerun.

## Nonnegotiable safety decisions

- **No automatic anti-flip, auto-upright or reconnect.** R remains manual.
- No changes to `.unity`, `.prefab`, `docs/Telemetry/` or existing
  user asset value migration in this milestone.
- Only source tests and guards until actual Unity 6.3 evidence exists.
- PR #16 remains Draft, issue #17 open, `main` untouched.

Useful Unity API references:
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/prefabutility
- https://docs.unity.cn/Packages/com.unity.test-framework%401.3/manual/reference-command-line.html
