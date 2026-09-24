# Roadmap

## Foundation
- [x] Initialize repository
- [x] Define runtime architecture
- [x] Define module architecture
- [x] Define initial module manifest
- [x] Establish .NET solution
- [x] Establish web API/operator console
- [x] Establish Windows desktop shell
- [x] Establish SQLite persistence
- [x] Establish test infrastructure

## Execution engine
- [x] Windows process provider
- [x] WSL2 process provider
- [ ] Container provider
- [ ] Remote agent provider
- [x] Job lifecycle/state
- [x] Cancellation/timeouts
- [x] Structured stdout/stderr/evidence capture
- [ ] Interactive PowerShell terminal sessions
- [ ] Interactive CMD terminal sessions
- [ ] Interactive Bash/WSL terminal sessions

## Module ecosystem
- [x] Module manifest validator
- [x] Filesystem module registry
- [x] Declarative execution templates
- [x] GitHub repository inspector
- [x] Safe GitHub module importer
- [ ] Dependency resolver
- [ ] Tool installer/health manager
- [ ] Module enable/disable/remove UI
- [ ] Module update/rollback
- [ ] Third-party module signing/trust policy
- [ ] SBOM/license inventory UI

## Assessment
- [x] Engagement model
- [x] Exact-target authorization gate
- [ ] CIDR/IP range scope engine
- [ ] DNS/domain scope engine
- [ ] Asset inventory
- [x] Findings model
- [x] Evidence model
- [ ] Report model
- [ ] Remediation/workflow model
- [ ] Assessment templates

## Initial integrations
- [x] Nmap manifest
- [x] Nuclei manifest
- [x] PowerShell reconnaissance manifest
- [x] Metasploit manifest
- [x] Greenbone manifest
- [x] Hashcat manifest
- [x] Aircrack-ng manifest
- [x] Wifite2 manifest
- [x] Binwalk manifest
- [x] BloodHound manifest
- [x] Impacket manifest
- [x] Masscan manifest
- [x] Wireshark/tshark manifest
- [ ] Tool-specific parsers and normalized findings for each integration

## Wireless
- [ ] USB wireless adapter inventory
- [ ] WSL2 wireless capability detection
- [ ] Monitor-mode capability detection
- [ ] Channel discovery workflow
- [ ] Capture workflow
- [ ] Injection capability detection
- [ ] Wireless evidence normalization
- [ ] Explicit disruption safeguards

## Distribution
- [ ] Windows installer
- [x] Explicit WSL runtime bootstrap
- [ ] Runtime/tool bootstrapper
- [ ] Signed releases
- [ ] Automatic update service
- [ ] Atomic rollback
- [ ] Enterprise deployment mode
