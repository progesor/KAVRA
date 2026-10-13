# M0 — First physics slice & acceptance

See the complete original technical design in the preceding commit.
This addendum documents the 2026-10-13 laboratory configuration hotfix.

## Lab scene configuration resolution

The host Editor menu `Tools > KAVRA > Create M0 Physics Lab Scene`
must never pass a null VehicleDefinition to `VehicleScaffoldBuilder.Create`.

An `Assets/KAVRA_PassengerCar4.asset` file existing on disk does **not**
guarantee it was correctly imported into Unity. It can fail to load as a
VehicleDefinition when its script reference is unresolved. The generator now:

1. Looks up the starter `.asset` and validates its type, axes, wheel layout,
   wheel IDs and required positive suspension travel.
2. Tries the host lab fallback `.asset` if the starter is unavailable/invalid.
3. Creates a new PassengerCar4 VehicleDefinition at a unique fallback path,
   persists it with AssetDatabase, and verifies it can be loaded back.
4. Checks again before replacing the current Editor scene.
5. Reacquires the persistent asset after scene creation and aborts with a
   useful error if Unity fails to reload it.

Existing assets are not deleted or overwritten.

**Validation:** GitHub source/static inspection only; Unity Editor compiler
and local Play Mode test must still be performed before merging PR #2.

## Original physics acceptance

- [ ] Unity compiles
- [ ] EditMode tests pass
- [ ] Lab scene spawns a 4W dynamic vehicle
- [ ] Static wheel load roughly matches m*g on flat ground
- [ ] Braking and steering work without divergent oscillations
- [ ] 3W/8W smoke checks
