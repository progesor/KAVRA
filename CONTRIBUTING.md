# KAVRA contribution and engineering notes

## Development flow

- Keep physics changes small and demonstrably testable.
- Use issue checklists and include exact Unity Editor version, device and
  video/telemetry for non-obvious physics issues.
- For serialized fields, preserve Unity `.meta` files and plan migration.
- Keep `Runtime` assemblies separate from editor and render/input adapters.
- Mark approximations and arcade interventions explicitly.
- Commit Unity `ProjectSettings/` and `Packages/packages-lock.json` after import.
- Do not commit generated `Library/`, cache or logs.
- Do not copy code or shaders from commercial vehicle plugins.

## Suggested naming

- Branches: `feat/m0-wheel-contact`, `fix/m0-wizard-import`, etc.
- Issues: `M0 | ...`, `M1 | ...`
- Commits: `feat:`, `fix:`, `docs:`, `test:`, `chore:`.

## Licensing

No project license has been selected; settle public/private and licensing
before accepting external code contributions.
