# Windows Security Audit Platform

A Windows-first, modular security assessment platform designed to orchestrate Windows-native and Linux/WSL2 security tooling from a unified operator experience.

## Current foundation

The repository now contains a .NET 10 solution with:

- native WPF desktop shell using WebView2
- local ASP.NET Core operator/API host
- YAML module manifest loader and validator
- filesystem module registry
- declarative command execution templates
- Windows and WSL2 execution providers using argument-safe process invocation
- engagement and exact-target authorization enforcement
- background job scheduler with cancellation/timeout handling
- persistent SQLite job history
- persistent stdout/stderr evidence with SHA-256 integrity digests
- GitHub repository inspection and safe module import
- explicit WSL2 bootstrap script
- initial official module catalog for Nmap, Nuclei, Metasploit, Greenbone, Aircrack-ng, Wifite2, Binwalk, Hashcat, BloodHound, Impacket, Masscan, Wireshark/tshark, and PowerShell reconnaissance

The project targets .NET 10, the current LTS release.

## Architecture

Windows Desktop
├── WebView2 operator UI
├── ASP.NET Core local API
├── Engagement / scope engine
├── Job scheduler
├── Module registry
├── Evidence / findings store
└── Execution providers
    ├── Windows process
    └── WSL2

The next major layers are structured result parsers, asset inventory, richer CIDR/domain scope rules, reporting, terminal sessions, runtime/tool installers, isolated containers/VMs, remote agents, centralized PostgreSQL/object storage, and release/update signing.

## Security boundary

The platform is an orchestrator rather than a replacement for every upstream security project. Modules declare runtime, privileges, dependencies, network impact, execution templates, evidence, and license metadata.

Jobs require an active engagement and an explicitly authorized target. Disruptive/destructive module classes require explicit confirmation. GitHub import is inspection-first and never executes repository install scripts or CI workflows.

Third-party software remains subject to its own licenses. License metadata in the catalog is deliberately conservative where the exact upstream terms have not yet been verified for redistribution.
