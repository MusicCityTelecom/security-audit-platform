# Security architecture

## Execution boundary

The operator UI is not the security boundary. Every executable operation flows through a runtime provider and a registered module manifest.

Modules declare runtime, privileges, network behavior, dependencies, and execution templates. The scheduler rejects invalid modules and requires an active engagement scope for jobs.

## Scope enforcement

A job must name an engagement and a target. The target must be explicitly included in the engagement scope and must not be explicitly excluded. Expired engagements are rejected.

The first implementation uses exact target matching. CIDR, hostname pattern, asset-group, and DNS-derived scope rules belong in the next scope-engine layer and must preserve explicit exclusions.

## Import boundary

GitHub repositories are untrusted input. Inspection never executes repository code. Import downloads source into a staging directory, rejects unsafe archive paths, requires a valid module manifest, validates the manifest, and only then copies the module into the installed module tree. Install scripts, CI workflows, and package hooks are never executed by the importer.

## Evidence integrity

Execution stdout/stderr is retained in SQLite with a SHA-256 digest. Future evidence storage will move large artifacts to content-addressed object storage while preserving hashes in the database.

## Privilege model

The desktop shell runs unelevated by default. Privileged operations will use explicit brokered elevation rather than running the entire operator console as Administrator.
