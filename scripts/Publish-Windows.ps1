[CmdletBinding()]
param(
  [string]$Configuration = "Release",
  [string]$Output = "$PSScriptRoot/../artifacts/windows"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path "$PSScriptRoot/..").Path
$out = (Resolve-Path (New-Item -ItemType Directory -Force -Path $Output)).Path

Write-Host "Publishing Security Audit Platform..." -ForegroundColor Cyan

dotnet restore "$root/SecurityAuditPlatform.sln"
dotnet publish "$root/src/web/SecurityAuditPlatform.Web/SecurityAuditPlatform.Web.csproj" -c $Configuration -r win-x64 --self-contained false -o "$out/web"
dotnet publish "$root/src/desktop/SecurityAuditPlatform.Desktop/SecurityAuditPlatform.Desktop.csproj" -c $Configuration -r win-x64 --self-contained false -o "$out/desktop"

Copy-Item "$out/web/*" "$out/desktop/" -Recurse -Force

Write-Host "Published desktop bundle: $out/desktop" -ForegroundColor Green
Write-Host "The desktop shell starts the local web host on loopback and embeds it with WebView2."
