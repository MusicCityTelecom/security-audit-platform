# Settings

Settings are stored in the local SQLite configuration store. Secret values such as API keys and tokens are encrypted using Windows DPAPI under the current Windows user context.

## Categories

Recommended settings include:

- API Keys — Shodan, Censys, SecurityTrails, VirusTotal, NVD, cloud providers, ticketing systems.
- Directories — data, modules, evidence, reports, tools, runtimes.
- Tools — explicit executable paths when a tool is not discoverable.
- Runtime — WSL distribution, container runtime, remote-agent defaults.
- Updates — update channel and release preferences.
- Reporting — report templates and output defaults.
- Network — proxy and DNS settings.
- UI — theme, confirmation behavior, table density, terminal preferences.

Secret settings are never returned by the settings GET endpoint; the response only indicates that a secret is configured.

A setting key is intentionally generic so new integrations do not require a core database schema change merely to add another credential.
