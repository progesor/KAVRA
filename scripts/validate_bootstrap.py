#!/usr/bin/env python3
"""Static (non-Unity) preflight for this starter tree."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PACKAGE = ROOT / "Packages/com.progesor.kavra"
REQUIRED = [
    "README.md", "docs/00_SCOPE_AND_GOALS.md",
    "docs/01_MUDRUNNER_RESEARCH.md", "docs/07_UNITY_SETUP.md",
    "Packages/com.progesor.kavra/package.json",
    "Packages/com.progesor.kavra/Runtime/Progesor.Kavra.Runtime.asmdef",
    "Packages/com.progesor.kavra/Editor/Progesor.Kavra.Editor.asmdef",
    "Packages/com.progesor.kavra/Editor/KavraWizardWindow.cs",
    "Packages/com.progesor.kavra/Runtime/Validation/VehicleConfigurationValidator.cs",
]

def main():
    for entry in REQUIRED:
        assert (ROOT / entry).is_file(), f"Missing {entry}"
    package = json.loads((PACKAGE / "package.json").read_text(encoding="utf-8"))
    assert package["name"] == "com.progesor.kavra"
    assert package["unity"] == "6000.3"
    assert package["version"] == "0.0.1"
    assert package["dependencies"] == {}
    rt = json.loads((PACKAGE / "Runtime/Progesor.Kavra.Runtime.asmdef").read_text(encoding="utf-8"))
    ed = json.loads((PACKAGE / "Editor/Progesor.Kavra.Editor.asmdef").read_text(encoding="utf-8"))
    assert rt["name"] in ed["references"]
    assert ed["includePlatforms"] == ["Editor"]
    assert not (ROOT / "Library").exists()
    for item in ROOT.rglob("*.json"):
        json.loads(item.read_text(encoding="utf-8"))
    print("PASS: static bootstrap manifest, asmdefs, docs and file structure")
    print("LIMIT: Unity C# compilation and Play Mode are NOT tested here")

if __name__ == "__main__":
    main()
