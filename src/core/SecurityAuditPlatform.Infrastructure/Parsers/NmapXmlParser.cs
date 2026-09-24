using System.Xml.Linq;
using SecurityAuditPlatform.Core.Findings;

namespace SecurityAuditPlatform.Infrastructure.Parsers;

public sealed class NmapXmlParser
{
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
