#!/usr/bin/env python3
"""Source-only KAVRA preflight. Does NOT compile Unity or run Play Mode."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PKG = ROOT / "Packages/com.progesor.kavra"
LAB = ROOT / "Assets/VehiclePhysicsLab"


def check(value, why):
    if not value:
        raise AssertionError(why)


def load(path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    required = (
        "README.md", "docs/00_SCOPE_AND_GOALS.md",
        "docs/08_ROADMAP_AND_MILESTONES.md",
        "docs/25_2026-10-17_CURRENT_STATE_AND_HANDOVER.md",
        "Packages/com.progesor.kavra/Runtime/Components/IVehicleAssemblyController.cs",
        "Assets/VehiclePhysicsLab/Tests/EditMode/KavraHitchLifecycleEditModeTests.cs",
        "Packages/com.progesor.kavra/Editor/KavraWizardWindow.cs",
    )
    for path in required:
        check((ROOT / path).is_file(), "Missing " + path)

    manifest = load(PKG / "package.json")
    check(manifest["name"] == "com.progesor.kavra", "UPM package name changed")
    check(manifest["unity"] == "6000.3", "UPM editor target changed")
    # The previous checker was obsolete: it pinned version to 0.0.1.
    check(re.fullmatch(r"\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?",
                       str(manifest["version"])) is not None,
          "UPM version is not semantic versioning")
    check(manifest["dependencies"] == {}, "UPM runtime gained a hard dependency")
    project_version = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text(
        encoding="utf-8")
    check("6000.3." in project_version, "Host Unity version differs from 6.3")
    for folder in ("Library", "Temp", "Obj"):
        check(not (ROOT / folder).exists(),
              "Unity-generated folder must not be committed: " + folder)

    assemblies = {}
    for path in ROOT.rglob("*.asmdef"):
        data = load(path)
        check(data["name"] not in assemblies,
              "Duplicate asmdef name: " + data["name"])
        assemblies[data["name"]] = (path, data)

    runtime = "Progesor.Kavra.Runtime"
    editor = "Progesor.Kavra.Editor"
    visuals = "Progesor.Kavra.Lab.HitchVisuals"
    tests = "Progesor.Kavra.Lab.EditModeTests"
    for name in (runtime, editor, visuals, tests):
        check(name in assemblies, "Missing asmdef: " + name)
    for name, (_, data) in assemblies.items():
        for reference in data.get("references", []):
            if reference.startswith("Progesor.Kavra."):
                check(reference in assemblies,
                      "Unresolved asmdef reference " + name + " -> " + reference)
    check(assemblies[runtime][0].parent == PKG / "Runtime",
          "Runtime code assembly moved")
    check(not assemblies[runtime][1]["references"],
          "Core runtime must not reference host/Editor")
    check(runtime in assemblies[editor][1]["references"],
          "Editor missing core reference")
    check(assemblies[editor][1]["includePlatforms"] == ["Editor"],
          "Package Editor may only compile in Editor")
    check(assemblies[visuals][0].parent == LAB / "Scripts/HitchVisuals",
          "Hitch visual guide is no longer in its own assembly")
    check(assemblies[visuals][1]["references"] == [runtime],
          "HitchVisuals should reference only the portable runtime")
    check(assemblies[visuals][1]["autoReferenced"] is True,
          "Predefined host scripts cannot access HitchVisuals")
    check({runtime, editor, visuals}.issubset(
        set(assemblies[tests][1]["references"])),
        "EditMode asmdef is missing explicit dependencies")
    check(assemblies[tests][1]["includePlatforms"] == ["Editor"],
          "EditMode tests must be Editor-only")
    check("TestAssemblies" in
          assemblies[tests][1].get("optionalUnityReferences", []),
          "EditMode assembly missing TestAssemblies reference")

    guide = LAB / "Scripts/HitchVisuals/VehicleLabHitchGuide.cs"
    check(guide.is_file(), "Missing moved hitch guide C# file")
    check("guid: ca1c5b2c99ab46b6b141000000000002" in
          (guide.parent / (guide.name + ".meta")).read_text(encoding="utf-8"),
          "Saved scene/prefab hitch MonoScript GUID changed")
    check(not (LAB / "Scripts/VehicleLabHitchGuide.cs").exists(),
          "Duplicate hitch guide compiled into Assembly-CSharp")
    source = guide.read_text(encoding="utf-8")
    parts = source.split("public void Setup(", 1)
    check(len(parts) == 2, "Hitch visual Setup missing")
    setup = parts[1].split("private void Awake(", 1)[0]
    check("EnsureArrays();" in setup and
          setup.index("EnsureArrays();") < setup.index("StyleMarker("),
          "Hitch guide tints marker before initializing property block")
    check("if (colorBlock == null)" in source,
          "Hitch guide lazy MaterialPropertyBlock guard missing")

    iface = (PKG / "Runtime/Components/IVehicleAssemblyController.cs"
             ).read_text(encoding="utf-8")
    check("interface IVehicleAssemblyController" in iface,
          "Shared external host API is missing")
    for component in ("VehicleAssemblyRoot", "VehicleAssemblyChainRoot"):
        source = (PKG / ("Runtime/Components/" + component + ".cs")
                  ).read_text(encoding="utf-8")
        check(": MonoBehaviour, IVehicleAssemblyController" in source,
              component + " does not expose shared control API")
        check("AssessAttachLink(" in source,
              component + " missing read-only attachment assessment")

    for path in ROOT.rglob("*.json"):
        load(path)
    print("PASS: UPM, asmdef graph, hitch GUID, P1 control API and JSON source")
    print("LIMIT: Unity compile, EditMode tests, PhysX and visuals NOT verified")


if __name__ == "__main__":
    main()
