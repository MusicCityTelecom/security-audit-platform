# Module System

The module system is a first-class extension point. The core platform must not need to be modified merely to add a new security tool or workflow.

## Module categories

- `recon`
- `network`
- `web`
- `wireless`
- `identity`
- `active-directory`
- `vulnerability`
- `exploitation`
- `post-exploitation`
- `credential-audit`
- `forensics`
- `malware-analysis`
- `firmware`
- `reporting`
- `utility`

## Module sources

1. Official modules maintained in this repository.
2. Community modules installed locally or from approved Git repositories.
3. Organization/private modules for customer-specific workflows.

## Required isolation

Modules must declare what they execute and where. The UI should surface runtime, privilege, network impact, and required hardware before execution.

A module must never silently elevate privileges, change host security controls, alter network configuration, or perform destructive actions.

## Suggested manifest

```yaml
id: example.network.nmap
name: Nmap Network Scanner
version: 0.1.0
category: network
runtime: wsl
entrypoint: module.py
capabilities:
  - host-discovery
  - port-scanning
  - service-enumeration
privileges:
  - network
requires:
  tools:
    - nmap
license:
  spdx: GPL-2.0-or-later
source:
  type: builtin
```

The manifest format is intentionally versioned. Future schema revisions must support migration rather than silently changing meaning.

## User customization

Users should be able to:

- clone an existing module
- edit its manifest
- change its command templates
- add parameters and validation
- add custom parsers
- add evidence mappings
- create custom workflows
- publish a module to GitHub
- import a module from a GitHub repository

## GitHub project import

A future importer should inspect a repository and require an explicit manifest or generate a reviewable proposed manifest. It should never blindly execute arbitrary repository install scripts.

Recommended flow:

```text
GitHub URL
  -> Repository inspection
  -> License/dependency inspection
  -> Runtime detection
  -> Proposed module manifest
  -> Operator approval
  -> Isolated installation
  -> Health check
  -> Available module
```
