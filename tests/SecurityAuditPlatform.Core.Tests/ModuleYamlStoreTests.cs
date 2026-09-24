using SecurityAuditPlatform.Core.Modules;
using SecurityAuditPlatform.Infrastructure.Modules;

namespace SecurityAuditPlatform.Core.Tests;

public sealed class ModuleYamlStoreTests
{
    [Fact]
    public void Parses_platform_module_schema()
    {
        const string yaml = """
schema_version: 1
id: test.network.echo
name: Echo
version: 1.2.3
category: network
runtime: wsl
entrypoint: echo
capabilities:
  - test
privileges:
  - network
requires:
  tools:
    - echo
  hardware: []
  runtimes:
    - wsl2
network:
  behavior: active
execution:
  executable: echo
  arguments:
    - "{target}"
  timeout_seconds: 42
license:
  spdx: MIT
source:
  type: github
  repository: https://github.com/example/echo
  ref: main
evidence:
  - stdout
""";

        var manifest = new ModuleManifestYamlStore().Deserialize(yaml);

        Assert.Equal("test.network.echo", manifest.Id);
        Assert.Equal(ModuleRuntime.Wsl, manifest.Runtime);
        Assert.Equal(NetworkBehavior.Active, manifest.NetworkBehavior);
        Assert.Equal("echo", manifest.Execution!.Executable);
        Assert.Equal(42, manifest.Execution.TimeoutSeconds);
        Assert.Contains("{target}", manifest.Execution.Arguments);
    }
}
