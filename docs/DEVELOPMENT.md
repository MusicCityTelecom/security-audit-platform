# Development

## Prerequisites

- Windows 11 or a supported Windows environment
- .NET 10 SDK
- Visual Studio 2026 or VS Code with C# tooling
- WebView2 Runtime for the desktop shell
- WSL2 for Linux-native modules

## Run the API

From the repository root:

    dotnet run --project src/web/SecurityAuditPlatform.Web

The API and local operator console listen on the ASP.NET Core development address. The console exposes health, module, engagement, job, and evidence endpoints.

## Run the desktop shell

The desktop shell expects the published web host beside it. Use scripts/Publish-Windows.ps1 for the intended bundle layout.

## Add a module

Create a directory below modules/ with a module.yaml. Start from module-schema/module.yaml. Run the module validator tests before adding the module to the official catalog.

## Import from GitHub

Use the API inspection endpoint first. Review repository/license/install-script signals. The importer does not execute repository scripts. Imported modules are validated before registration.

## CI

The repository includes a Windows GitHub Actions build/test workflow. If Actions are disabled for the repository, run the same restore/build/test commands locally on Windows.
