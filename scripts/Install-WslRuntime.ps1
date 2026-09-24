[CmdletBinding(SupportsShouldProcess)]
param(
  [string]$Distribution = "Ubuntu-24.04",
  [switch]$Install,
  [switch]$SetDefault
)

$ErrorActionPreference = "Stop"

Write-Host "Security Audit Platform - WSL2 runtime bootstrap" -ForegroundColor Cyan
wsl.exe --status

if (-not $Install) {
  Write-Host ""
  Write-Host "Discovery only. Re-run with -Install to request installation of the selected WSL distribution."
  wsl.exe --list --online
  exit 0
}

if ($PSCmdlet.ShouldProcess($Distribution, "Install WSL distribution")) {
  wsl.exe --install -d $Distribution
}

if ($SetDefault -and $PSCmdlet.ShouldProcess($Distribution, "Set WSL default distribution")) {
  wsl.exe --set-default $Distribution
}

Write-Host "WSL runtime bootstrap complete. Reboot may be required by Windows."
