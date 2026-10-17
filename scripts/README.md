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
