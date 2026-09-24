# Tool Catalog

The repository uses module definitions to catalog upstream security tools without copying their entire codebases into the platform.

Current catalog families include:

- Network: Nmap, Masscan, Wireshark/tshark, hping3, tcpdump.
- Recon: Amass, Subfinder, httpx, PowerShell system reconnaissance.
- Web: Nuclei, ffuf, Feroxbuster, Gobuster, Nikto, sqlmap.
- Identity/AD: BloodHound, NetExec, enum4linux-ng, Impacket, Certipy, Responder.
- Exploitation: Metasploit.
- Vulnerability management: Greenbone.
- Credential auditing: Hashcat, John the Ripper.
- Wireless: Aircrack-ng, Wifite2.
- Firmware: Binwalk, Firmware Mod Kit.
- Forensics: Volatility 3, Plaso.
- Malware analysis: YARA, capa.

Catalog entries are not a claim that every upstream tool is installed, supported, or redistribution-compatible. The platform's tool health layer determines whether a local dependency is available, and each module's license metadata must be verified before distribution.

The next integration step for each catalog item is a tested adapter plus output parser, evidence mapping, and disposable-runtime integration test.
