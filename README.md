# Security Audit Platform

A modular Windows-first security assessment and penetration-testing platform integrating native Windows tooling, WSL2/Linux security tooling, wireless assessment capabilities, evidence collection, reporting, and extensible third-party modules.

> **Authorization:** This platform is intended for authorized security assessments, defensive validation, laboratories, and systems owned or explicitly authorized for testing.

## Project goals

- Windows-native desktop experience with a web-based operator UI
- Integrated PowerShell, CMD, Bash/WSL, Python, and tool terminals
- WSL2-backed Linux security tooling where Windows cannot provide equivalent capabilities
- Modular tool/plugin architecture so users can add or customize modules
- Easy integration of compatible open-source security projects from GitHub
- Tool/runtime/license metadata and SBOM support from the beginning
- Engagement scoping, evidence collection, findings, and professional reporting
- Local-first operation with a path to centralized/enterprise deployments
- Safe-by-default execution controls and explicit authorization scope

## Planned architecture

```text
Windows Desktop
  ├─ Web UI
  ├─ Assessment Engine
  ├─ Job Scheduler
  ├─ Scope/Authorization Engine
  ├─ Evidence & Findings Store
  ├─ Tool/Module Manager
  ├─ PowerShell/CMD Terminal
  └─ WSL2 Runtime Manager
       └─ Linux Security Environment
```

## Repository status

The repository is intentionally starting as an architecture-first foundation. Core contracts, module manifests, tool adapters, execution boundaries, and update/install mechanisms will be established before large tool integrations are added.

## Security model

The platform must preserve clear authorization boundaries. Modules should declare capabilities, runtime requirements, required privileges, network impact, and whether they are passive, active, disruptive, or destructive. High-impact operations should require explicit operator confirmation and applicable engagement scope.

## License

Project licensing and third-party component licensing will be finalized as the architecture and dependency model are established. Third-party projects must retain their original licenses and notices where required.
