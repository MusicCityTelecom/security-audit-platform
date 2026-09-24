using System.Xml.Linq;
using SecurityAuditPlatform.Core.Findings;
using SecurityAuditPlatform.Core.Assets;

namespace SecurityAuditPlatform.Infrastructure.Parsers;

public sealed class NmapXmlParser
{
    public IReadOnlyList<AssetObservation> ParseAssets(string xml, string source = "nmap")
    {
        var document = XDocument.Parse(xml, LoadOptions.None);
        var observations = new List<AssetObservation>();

        foreach (var host in document.Descendants("host"))
        {
            var address = host.Element("address")?.Attribute("addr")?.Value;
            if (string.IsNullOrWhiteSpace(address)) continue;

            var hostname = host.Descendants("hostname").FirstOrDefault()?.Attribute("name")?.Value;
            var os = host.Descendants("osmatch").FirstOrDefault()?.Attribute("name")?.Value;
            observations.Add(new AssetObservation(address, AssetKind.Host, hostname, os, Source: source));

            foreach (var port in host.Descendants("port"))
            {
                if (!string.Equals(port.Element("state")?.Attribute("state")?.Value, "open", StringComparison.OrdinalIgnoreCase))
                    continue;

                var service = port.Element("service");
                observations.Add(new AssetObservation(
                    address, AssetKind.Service, hostname, os,
                    Port: int.TryParse(port.Attribute("portid")?.Value, out var p) ? p : null,
                    Protocol: port.Attribute("protocol")?.Value,
                    Service: service?.Attribute("name")?.Value,
                    Version: string.Join(" ", new[] { service?.Attribute("product")?.Value, service?.Attribute("version")?.Value }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    Source: source));
            }
        }

        return observations;
    }

    public IReadOnlyList<Finding> Parse(string xml)
    {
        var document = XDocument.Parse(xml, LoadOptions.None);
        var findings = new List<Finding>();
        foreach (var host in document.Descendants("host"))
        {
            var address = host.Element("address")?.Attribute("addr")?.Value;
            foreach (var port in host.Descendants("port"))
            {
                var state = port.Element("state")?.Attribute("state")?.Value;
                if (!string.Equals(state, "open", StringComparison.OrdinalIgnoreCase)) continue;
                var portId = port.Attribute("portid")?.Value ?? "?";
                var protocol = port.Attribute("protocol")?.Value ?? "tcp";
                var service = port.Element("service");
                var name = service?.Attribute("name")?.Value;
                var product = service?.Attribute("product")?.Value;
                var version = service?.Attribute("version")?.Value;
                var detail = string.Join(" ", new[] { name, product, version }.Where(x => !string.IsNullOrWhiteSpace(x)));
                findings.Add(new Finding(Guid.NewGuid(), $"Open {protocol}/{portId}", $"Nmap reported an open {protocol} port. {detail}".Trim(),
                    FindingSeverity.Informational, address, "Review whether this service is required and appropriately exposed.", [], DateTimeOffset.UtcNow));
            }
        }
        return findings;
    }
}
