[CmdletBinding(SupportsShouldProcess)]
param(
  [string]$Repository = "MusicCityTelecom/security-audit-platform",
  [string]$Ref = "main",
  [string]$InstallRoot = "$env:ProgramFiles/SecurityAuditPlatform"
)
$ErrorActionPreference = "Stop"
if (-not $IsWindows) { throw "This updater is intended for Windows." }
$stage = Join-Path $env:TEMP ("sap-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $stage | Out-Null
try {
  $zip = Join-Path $stage "source.zip"
  Invoke-WebRequest -Uri "https://codeload.github.com/$Repository/zip/refs/heads/$Ref" -OutFile $zip
  Expand-Archive -LiteralPath $zip -DestinationPath $stage -Force
  $source = Get-ChildItem $stage -Directory | Where-Object { $_.FullName -ne $stage } | Select-Object -First 1
  if (-not $source) { throw "GitHub archive did not contain a source directory." }
  $solution = Join-Path $source.FullName "SecurityAuditPlatform.sln"
  if (-not (Test-Path $solution)) { throw "Downloaded source does not contain the expected solution." }
  $publish = Join-Path $stage "publish"
  dotnet publish (Join-Path $source.FullName "src/web/SecurityAuditPlatform.Web/SecurityAuditPlatform.Web.csproj") -c Release -r win-x64 --self-contained false -o (Join-Path $publish "web")
  dotnet publish (Join-Path $source.FullName "src/desktop/SecurityAuditPlatform.Desktop/SecurityAuditPlatform.Desktop.csproj") -c Release -r win-x64 --self-contained false -o (Join-Path $publish "desktop")
  Copy-Item (Join-Path $publish "web/*") (Join-Path $publish "desktop/") -Recurse -Force
  if ($PSCmdlet.ShouldProcess($InstallRoot, "Replace application binaries")) {
    $backup = "$InstallRoot.backup-" + (Get-Date -Format "yyyyMMdd-HHmmss")
    if (Test-Path $InstallRoot) { Move-Item $InstallRoot $backup }
    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
    Copy-Item (Join-Path $publish "desktop/*") $InstallRoot -Recurse -Force
    Write-Host "Update staged at $InstallRoot" -ForegroundColor Green
    Write-Host "Previous installation backup: $backup"
  }
}
finally { Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue }
