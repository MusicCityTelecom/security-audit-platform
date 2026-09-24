using SecurityAuditPlatform.Core.Assets;
using SecurityAuditPlatform.Infrastructure.Parsers;

namespace SecurityAuditPlatform.Core.Tests;

public sealed class ParserAssetTests
{
    [Fact]
    public void Nmap_ProducesHostAndOpenServiceAssets()
    {
        const string xml = """
        <nmaprun><host><status state="up"/><address addr="10.0.0.5" addrtype="ipv4"/>
        <hostnames><hostname name="server01.example.test"/></hostnames>
        <ports><port protocol="tcp" portid="443"><state state="open"/><service name="https" product="nginx" version="1.26"/></port></ports>
        </host></nmaprun>
        """;

        var assets = new NmapXmlParser().ParseAssets(xml);

        Assert.Contains(assets, x => x.Kind == AssetKind.Host && x.Value == "10.0.0.5");
        Assert.Contains(assets, x => x.Kind == AssetKind.Service && x.Port == 443 && x.Service == "https");
    }

    [Fact]
    public void Nuclei_ProducesDomainAndWebApplicationAssets()
    {
        const string json = """{"host":"app.example.test","ip":"10.0.0.20","matched-at":"https://app.example.test:8443/login"}""";

        var assets = new NucleiJsonlParser().ParseAssets(json);

        Assert.Contains(assets, x => x.Kind == AssetKind.Domain && x.Value == "app.example.test");
        Assert.Contains(assets, x => x.Kind == AssetKind.WebApplication && x.Port == 8443 && x.Protocol == "https");
    }
}
