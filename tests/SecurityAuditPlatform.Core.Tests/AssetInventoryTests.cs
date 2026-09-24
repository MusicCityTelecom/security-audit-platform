using SecurityAuditPlatform.Core.Assets;
using SecurityAuditPlatform.Infrastructure.Assets;
using SecurityAuditPlatform.Infrastructure.Data;

namespace SecurityAuditPlatform.Core.Tests;

public sealed class AssetInventoryTests
{
    [Fact]
    public void Upsert_MergesRepeatObservations()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sap-assets-{Guid.NewGuid():N}.db");
        try
        {
            var db = new PlatformDatabase(path);
            db.InitializeAssetSchema();
            var service = new AssetInventoryService(db);

            var first = service.Upsert(new AssetObservation("10.10.10.5", AssetKind.Host, Hostname: "server01", Source: "nmap"));
            var second = service.Upsert(new AssetObservation("10.10.10.5", AssetKind.Host, OperatingSystem: "Windows", Source: "nmap"));

            Assert.Equal(first.Id, second.Id);
            Assert.Equal("server01", second.Hostname);
            Assert.Equal("Windows", second.OperatingSystem);
            Assert.Single(service.List());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
