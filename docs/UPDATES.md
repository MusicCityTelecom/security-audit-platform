# Update and import model

The platform treats GitHub as a source, not a trust boundary.

## Module import

1. Operator supplies a GitHub repository URL.
2. The inspector retrieves repository metadata and a bounded file tree.
3. License, manifest, dependency, workflow, and install-script signals are displayed.
4. No repository script, workflow, build hook, or package installer is executed during inspection.
5. Operator approves an installation plan.
6. Installation occurs into an isolated module directory and is validated before registration.
7. The installed revision is pinned and recorded for rollback.

## Core updates

Core releases will use signed/pinned release artifacts, preserve local configuration, validate the package before replacement, and keep the previous version available for rollback.

Module updates are independent of core updates. A module update cannot silently change the core runtime, Windows security settings, WSL networking, firewall state, or installed credentials.
