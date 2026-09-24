# Module System

Modules are first-class extensions. The core platform should not need to be modified to add another scanner, enumeration utility, parser, wireless workflow, exploitation framework, or reporting integration.

## Manifest

A module declares:

- stable ID
- version
- category
- runtime
- entrypoint
- capabilities
- privileges
- required tools/hardware/runtimes
- network behavior
- execution template
- input/output/evidence metadata
- license
- upstream source

Execution templates use argument arrays instead of a single shell command string. {target} is replaced by the target selected by the job and is passed as a discrete process argument.

## Import

The GitHub importer:

1. inspects repository metadata and file tree;
2. reports license and install-script signals;
3. downloads source into a temporary staging directory;
4. rejects absolute and traversal archive paths;
5. locates and parses module.yaml;
6. validates the manifest;
7. installs only validated module files;
8. refreshes the registry.

It does not execute repository install scripts, workflow files, build hooks, or package installers.

## Runtime classes

- Windows: PowerShell, CMD, native Windows tools, Windows security/network APIs.
- WSL2: Linux security tooling such as Nmap, Nuclei, Metasploit, Aircrack-ng, Wifite2, Hashcat, Binwalk, Impacket, Masscan, and other compatible projects.
- Container: planned isolated services and scanners.
- Remote: planned authenticated agents for distributed assessments.

## High-impact controls

Passive, active, disruptive, and destructive behavior are explicit metadata. Active modules still require engagement scope. Disruptive/destructive modules additionally require explicit operator confirmation.

## Customization

Operators will be able to clone an installed module, edit its manifest and command templates, add parsers/evidence mappings, test it in a disposable runtime, and publish the module to a private or public Git repository without changing the core platform.
