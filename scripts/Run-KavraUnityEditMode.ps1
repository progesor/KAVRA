<#
.SYNOPSIS
    Run KAVRA EditMode acceptance with the same Unity Editor as the host project.
.DESCRIPTION
    Run in PowerShell with the Unity project CLOSED in the GUI.
    No Unity installs/licensing are bundled with the repo; supply a local editor.
    Results and log go to a NEW folder under Windows TEMP by default,
    NOT into a tracked user scene, prefab or docs/Telemetry directory.
    A PASS requires nonempty NUnit results and every required test suite.
.EXAMPLE
    .\scripts\Run-KavraUnityEditMode.ps1
.EXAMPLE
    .\scripts\Run-KavraUnityEditMode.ps1 -UnityEditorPath 'D:\Unity\Editor\Unity.exe'
#>
[CmdletBinding()]
param(
    [string] $UnityEditorPath = "",
    [string] $ProjectPath = "",
    [string] $OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $PSScriptRoot ".."
}
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$projectVersionFile = Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"
if (-not (Test-Path -LiteralPath $projectVersionFile -PathType Leaf)) {
    throw "KAVRA: Not a Unity project: $ProjectPath"
}
$projectVersion = Get-Content -LiteralPath $projectVersionFile -Raw
if ($projectVersion -notmatch '(?m)^m_EditorVersion:\s*6000\.3\.18f1\s*$') {
    throw "KAVRA: This acceptance script targets Unity 6000.3.18f1. Check ProjectVersion.txt before proceeding."
}

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = $env:KAVRA_UNITY_EDITOR
}
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw "KAVRA: Unity.exe not found at '$UnityEditorPath'. Use -UnityEditorPath or set KAVRA_UNITY_EDITOR."
}
$UnityEditorPath = (Resolve-Path -LiteralPath $UnityEditorPath).Path

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "KAVRA-Unity-Acceptance"
}
$runId = (Get-Date -Format "yyyyMMdd-HHmmss") + "-" +
    ([System.Guid]::NewGuid().ToString("N").Substring(0, 8))
$runDirectory = Join-Path $OutputDirectory $runId
New-Item -Path $runDirectory -ItemType Directory -Force | Out-Null
$xmlPath = Join-Path $runDirectory "EditMode-results.xml"
$logPath = Join-Path $runDirectory "Unity-Editor.log"

$arguments = @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", $ProjectPath,
    "-runTests", "-testPlatform", "EditMode",
    "-assemblyNames", "Progesor.Kavra.Lab.EditModeTests",
    "-testResults", $xmlPath,
    "-logFile", $logPath
)
Write-Host "KAVRA Unity test project: $ProjectPath"
Write-Host "Unity Editor: $UnityEditorPath"
Write-Host "Results: $xmlPath"
Write-Host "Editor log: $logPath"
Write-Host "Run only after closing the same project in the Unity GUI."

& $UnityEditorPath @arguments
$unityExit = $LASTEXITCODE

if (-not (Test-Path -LiteralPath $xmlPath -PathType Leaf)) {
    throw "KAVRA: Unity exited ($unityExit) without EditMode XML. Inspect '$logPath' for compiler/package/license errors."
}

[xml] $testReport = Get-Content -LiteralPath $xmlPath -Raw
$run = $testReport.SelectSingleNode("/test-run")
if ($null -eq $run) {
    throw "KAVRA: Results did not contain an NUnit <test-run>. Inspect '$xmlPath'."
}
[int] $total = 0
[int] $passed = 0
[int] $failed = 0
[int] $skipped = 0
if (-not [int]::TryParse($run.GetAttribute("total"), [ref] $total) -or
    -not [int]::TryParse($run.GetAttribute("passed"), [ref] $passed) -or
    -not [int]::TryParse($run.GetAttribute("failed"), [ref] $failed)) {
    throw "KAVRA: Unable to parse NUnit test counts from '$xmlPath'."
}
[void] [int]::TryParse($run.GetAttribute("skipped"), [ref] $skipped)

$expectedSuites = @(
    "KavraPhysicsEditModeTests",
    "KavraAssemblyEditModeTests",
    "KavraHitchLifecycleEditModeTests",
    "KavraAssemblyChainEditModeTests",
    "KavraDefinitionValidationEditModeTests",
    "KavraPrefabRoundtripEditModeTests"
)
$missing = New-Object "System.Collections.Generic.List[string]"
foreach ($suite in $expectedSuites) {
    $fullname = "Progesor.Kavra.Lab.Tests." + $suite
    $node = $testReport.SelectSingleNode(
        "//test-suite[@fullname='" + $fullname + "']")
    if ($null -eq $node) {
        $missing.Add($suite)
    }
}

Write-Host ("KAVRA EditMode: {0} total, {1} passed, {2} failed, {3} skipped (Unity exit {4})." -f
    $total, $passed, $failed, $skipped, $unityExit)
if ($missing.Count -gt 0) {
    Write-Warning ("Test suites not found: " + ($missing -join ", "))
}
if ($unityExit -ne 0 -or $total -le 0 -or $failed -gt 0 -or
    $run.GetAttribute("result") -ne "Passed" -or $missing.Count -gt 0) {
    throw "KAVRA: Unity EditMode acceptance NOT PASSED. Results '$xmlPath'; log '$logPath'."
}
Write-Host "PASS: KAVRA EditMode Test Runner (actual Unity execution)."
