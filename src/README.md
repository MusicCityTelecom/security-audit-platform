# Source Layout

The application will be organized into independently testable layers:

- `core/` — domain contracts and execution abstractions
- `modules/` — official modules
- `tools/` — tool definitions/adapters
- `desktop/` — Windows host
- `web/` — operator web interface
- `runtimes/` — WSL/container runtime definitions
- `tests/` — unit/integration/contract tests

The initial implementation language for the platform core is planned to be C#/.NET. UI and runtime choices remain replaceable behind stable interfaces.
