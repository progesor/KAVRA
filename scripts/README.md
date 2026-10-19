# Source-only preflight

Run `python3 scripts/validate_bootstrap.py` from the repository root.
No third-party Python packages are required.

Checks UPM version syntax (not the obsolete 0.0.1 value), package
dependencies, host Unity version, asmdef names/references and Editor-only
boundaries, HitchVisuals moved-script GUID, hitch MaterialPropertyBlock
initialization ordering, shared P1 assembly control API presence, JSON
syntax and accidental generated Unity folders.

**Static checks are not Unity compiler tests.** This script cannot execute
NUnit/Test Runner, PhysX joints or the visual PhysicsLab acceptance. Those
results must come from Unity 6000.3.18f1 and remain open on issue #17.

## Actual Unity acceptance (Windows / PowerShell)

Close the Unity Editor GUI on the **same project** first, then run:

```powershell
git switch feat/m15-multitrailer-chain
git pull --ff-only origin feat/m15-multitrailer-chain
.\scripts\Run-KavraUnityEditMode.ps1
```

If Unity Hub is elsewhere, pass `-UnityEditorPath "D:\Unity\Editor\Unity.exe"`
or set the environment variable `KAVRA_UNITY_EDITOR`.
The script requires `ProjectVersion.txt` to target **6000.3.18f1**.

Unlike the Python check, this launches a REAL Unity Editor test run. It
filters `Progesor.Kavra.Lab.EditModeTests` and rejects absent XML, zero
tests, failed tests, missing expected suite classes, or an unsuccessful
Unity process. The NUnit XML and Unity Editor log are written to
new uniquely named folders under the Windows Temp directory, not into
your project's saved scenes, prefabs, or `docs/Telemetry/`.

Manual Play Mode (checker/hitch graphics, H/J/L/K/R and physics solver
joint breaks) is intentionally not claimed by this EditMode runner.
No Unity installation or license is embedded in the repository.

### 19 October 2026 troubleshooting update

The previous runner mistakenly combined `-runTests` with `-quit`.
Unity 6.3's official Test Framework command-line reference states they
must **not** be combined: the test runner shuts down the Editor after
completion. The updated script omits `-quit`, reports exact failed test
names and writes `Acceptance-summary.txt` beside the XML and Editor log.

If Unity genuinely crashes (rather than shutting down as batch mode
normally does), upload the generated `Unity-Editor.log` and the
`Acceptance-summary.txt`. For tests run **inside** the GUI, capture
`%LOCALAPPDATA%\\Unity\\Editor\\Editor.log` immediately after the crash;
native reports may be under `%TMP%\\Unity\\Editor\\Crashes`.
A completed NUnit XML does not prove that Unity exited normally.

## 19 October — native Unity Test Runner Undo rollback crash

A user-supplied Unity native crash report shows test results saved and
`IPostBuildCleanup` finished before `Dereferencing NULL ImmediatePtr!`.
The crashing managed stack includes `UnityEditor.Undo.RevertAllDownToGroup`
inside `TestJobRunner.ExecuteStep`.

KAVRA's older Editor builders registered `Undo` creations and components
for the same temporary test/prefab objects later destroyed with
`Object.DestroyImmediate`. This is a plausible source of stale Undo
handles, not a proven Unity-engine root cause.

Builders now accept `recordUndo` (defaults to **true**, preserving the
interactive scene tools). All disposable EditMode test rigs and temporary
prefab builds explicitly use `recordUndo: false`; nested visual rigs
and hitch markers inherit the same option. No `Undo.ClearAll()`
or forceful undo-stack removal is used.

Re-run Test Runner on the updated branch. If **all NUnit cases PASS**
but Unity still crashes, save both results and the newest Unity native
crash report. The next step is to isolate suites / report the Unity
native crash; **do not** delete Library, existing assets, or telemetry.
See `docs/29_2026-10-19_NATIVE_EDITOR_UNDO_CRASH.md`.


### P2 imported model tests

The Unity EditMode acceptance script now requires **seven** suites;
`KavraImportedVisualEditModeTests` covers synthetic imported model
GameObject assets for 4W, 6x6 and 20W configurations, one-Rigidbody
invariance, wheel visual binding and source mesh/material preservation,
error handling, scale warnings and prefab save/reimport.

The tests use unique temporary Assets folders and `recordUndo=false`;
they do **not** install FBX or glTF importers, add vendor models to
the repository, or replace a real external FBX scene inspection.
