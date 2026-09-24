# Architecture

## Core principles

1. The platform is an orchestrator, not a rewrite of every security tool.
2. Windows is the primary operator environment.
3. WSL2 provides Linux-native tooling and reproducible Linux runtimes.
4. Every tool is represented by metadata and an adapter rather than hard-coded into the UI.
5. Modules are independently installable, updateable, configurable, and removable.
6. Assessments are scope-bound and produce durable evidence.
7. Raw tool output is retained where appropriate; normalized results feed the findings engine.
8. Third-party code is isolated from the core and tracked for license/SBOM purposes.

## Runtime layers

### Host layer

- Windows desktop application
- Windows APIs
- PowerShell
- Windows networking and security APIs
- supported USB/network hardware

### Linux layer

- WSL2 distributions
- Bash
- Python
- Go/Rust toolchains as required
- Linux-specific security utilities

### Optional isolation layer

- containers
- disposable VMs/labs
- dedicated remote scanners

## Module model

A module should describe:

- stable module ID
- name and version
- author/vendor
- description
- capabilities
- runtime (`windows`, `wsl`, `container`, `remote`)
- required tools
- required privileges
- required hardware
- network behavior classification
- input schema
- output schema
- evidence types
- license metadata
- update source

The module should expose a small execution contract while keeping implementation details private to the module.

## Tool adapters

Adapters translate normalized platform jobs into tool-specific invocations and translate tool output back into normalized results. A tool may execute natively on Windows, inside WSL2, in a container, or on a configured remote scanner.

## GitHub integration

The platform should support importing compatible projects from GitHub through an explicit module/tool manifest. GitHub is a source, not a trust boundary: imported projects must be inspected, version-pinned where possible, license metadata recorded, and installed in an appropriate runtime.

## Updates

The application will have separate update channels for:

- platform core
- official modules
- third-party modules
- tool definitions
- Linux runtime images

Core updates should be atomic and rollback-capable. User-created modules and configuration must not be overwritten by platform updates.

## Future distributed architecture

```text
Operator Desktop
      |
      +---- Local Execution Engine
      |
      +---- Remote Agent(s)
      |
      +---- Central API
               |
               +---- PostgreSQL
               +---- Object/Evidence Storage
               +---- Job Queue
               +---- Web Console
```


## Operator services

The local operator API now includes:

- encrypted settings/credential storage;
- tool discovery and health checks;
- WSL distribution discovery;
- managed PowerShell/CMD/WSL terminal sessions;
- operator audit logging;
- GitHub release update checking;
- transactional Windows update staging;
- findings and HTML report generation.

The API is explicitly loopback-only. The desktop shell launches the API on a loopback port and embeds the operator UI through WebView2.

## Configuration model

Settings are key/value records grouped by category. Secrets are encrypted before being stored. Directory and tool-path settings are intentionally generic so new integrations can add configuration without a database migration. Module directories can be applied at runtime; application data migration and evidence/report storage relocation remain future migration work.

## Execution and evidence

Assessment jobs are distinct from terminal sessions. Assessment jobs are scope-bound and produce structured job/evidence records. Terminal sessions are operator consoles and are audited separately. This separation prevents arbitrary terminal commands from being mistaken for scoped assessment jobs while still providing the Windows-first operator workflow.
