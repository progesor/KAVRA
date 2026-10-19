# KAVRA — 19 October 2026: Unity native crash during Test Runner Undo rollback

**State: probable trigger corrected in source; Unity crash-free rerun pending.**
Active branch: `feat/m15-multitrailer-chain`, package **v0.1.6**.
Unity Editor: **6000.3.18f1**, Windows 11 project.

## Evidence: user-uploaded Unity crash report

Attached source: `CrashReport2026-10-19_074234331.json.gz`.
The `.json.meta` is a binary sidecar and supplies no additional readable
test failure evidence.

The gzip report is valid JSON. Key facts from its contents:

- `is_editor: true`
- `has_native_crash: true`
- `has_managed_exception: false`
- 246 threads, main thread marked `crashed: true`
- `2026-10-19 07:42:25.262 UTC`: `Saving results to:
  C:/Users/proge/AppData/LocalLow/DefaultCompany/KAVRA/TestResults.xml`
- `07:42:25.420 UTC`: `IPostBuildCleanup` for
  `Unity.PerformanceTesting.Editor.TestRunBuilder`
- `07:42:25.854 UTC`: `Dereferencing NULL ImmediatePtr!`

The **crashing thread** (stack positions 21–25 in the native report)
contains this managed call path:

```
UnityEditor.EditorApplication.Internal_CallUpdateFunctions
  -> UnityEditor.TestTools.TestRunner.TestRun.TestJobRunner.ExecuteCallback
  -> UnityEditor.TestTools.TestRunner.TestRun.TestJobRunner.ExecuteStep
  -> <Execute>d__6.MoveNext
  -> UnityEditor.Undo.RevertAllDownToGroup
  -> Unity native (no available public function symbols)
```

The test results were **saved before** the crash. The user reports all
test cases passed in this run; the new result XML was not attached, so
its exact count is not independently verified here. The report DOES
prove a native Unity crash during Test Runner Undo rollback, not a
normal batch-mode exit and not a `-quit` issue.

## Likely KAVRA trigger / scope of fix

Editor builders had used `Undo.RegisterCreatedObjectUndo`,
`Undo.AddComponent`, `Undo.SetTransformParent` and
`Undo.DestroyObjectImmediate` on disposable hierarchies. The
NUnit `[TearDown]` paths then used `Object.DestroyImmediate` to
delete those objects before Test Runner invoked
`Undo.RevertAllDownToGroup`. This combination plausibly leaves
stale Undo handles, making the Test Runner's rollback vulnerable to
the observed native null pointer. **The crash report does not identify
the exact stale object**, and a Unity engine defect remains possible.

Source changes:
1. Editor builders accept `recordUndo = true` (backwards-compatible).
2. **Normal Editor menu/Wizard scene creation** retains Undo.
3. **All temporary test/prefab creates** explicitly pass
   `recordUndo = false`, using regular `GameObject.AddComponent`
   rather than Undo APIs.
4. Nested wheel pivot rigs, demonstration shocks, hitch markers and
   member components inherit the same flag. Temporary builders also
   do not change the active selection.
5. Temporary prefab stage objects are cleaned using
   `Object.DestroyImmediate`, no Undo registration.
6. Static preflight checks that test sources use the no-Undo pathway.

Importantly **no global `Undo.ClearAll()`** call was introduced:
user Editor undo history must not be discarded.

## Verification

- GitHub Actions source-only Python/PowerShell preflight is NOT
  a native Unity crash reproduction or Editor compiler acceptance.
- Pull the feature branch, open Unity 6000.3.18f1, run the same six
  EditMode suites, and confirm both **all NUnit pass** and the Editor
  remains running afterward.
- Prefer also checking batch mode with
  `scripts/Run-KavraUnityEditMode.ps1` with GUI Editor closed,
  which captures a unique Editor log and test XML.
- If it still crashes, attach latest native report and corresponding
  Editor.log; isolate individual fixtures to distinguish project code
  from Unity Test Runner package behavior.

## Invariants

- Original M0/M1.4/M1.5 scenes and user `docs/Telemetry` unchanged.
- PhysX, recovery/rollover behavior, `H` hitch, manual `R`,
  ScriptableObject assets, GUIDs and UPM version unchanged.
- PR #16 stays Draft / issue #17 open until real evidence.

Official Unity Undo documentation:
https://docs.unity.com/en-us/engine/6000.7/script-reference/unityeditor/undo/registercreatedobjectundo
https://docs.unity.cn/6000.0/Documentation/ScriptReference/Undo.RevertAllDownToGroup.html
