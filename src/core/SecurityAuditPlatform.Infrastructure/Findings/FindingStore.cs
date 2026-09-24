using System.Text.Json;
using Microsoft.Data.Sqlite;
using SecurityAuditPlatform.Core.Findings;

namespace SecurityAuditPlatform.Infrastructure.Findings;

public sealed class FindingStore
{
    private readonly Data.PlatformDatabase _database;
    public FindingStore(Data.PlatformDatabase database) => _database = database;

    public Finding Save(Finding finding)
    {
        using var c = _database.OpenConnection(); using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO findings(id,title,description,severity,asset,remediation,evidence_ids,created_at)
VALUES($id,$title,$description,$severity,$asset,$remediation,$evidence,$created)
ON CONFLICT(id) DO UPDATE SET title=$title,description=$description,severity=$severity,asset=$asset,remediation=$remediation,evidence_ids=$evidence;";
        cmd.Parameters.AddWithValue("$id", finding.Id.ToString());
        cmd.Parameters.AddWithValue("$title", finding.Title);
        cmd.Parameters.AddWithValue("$description", finding.Description);
        cmd.Parameters.AddWithValue("$severity", (int)finding.Severity);
        cmd.Parameters.AddWithValue("$asset", (object?)finding.Asset ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$remediation", (object?)finding.Remediation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$evidence", JsonSerializer.Serialize(finding.EvidenceIds));
        cmd.Parameters.AddWithValue("$created", finding.CreatedAt.ToString("O"));
        cmd.ExecuteNonQuery();
        return finding;
    }

    public IReadOnlyList<Finding> List()
    {
        using var c = _database.OpenConnection(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT id,title,description,severity,asset,remediation,evidence_ids,created_at FROM findings ORDER BY created_at DESC;";
        using var reader = cmd.ExecuteReader();
        var results = new List<Finding>();
        while(reader.Read())
        {
            results.Add(new Finding(Guid.Parse(reader.GetString(0)),reader.GetString(1),reader.GetString(2),(FindingSeverity)reader.GetInt32(3),
                reader.IsDBNull(4)?null:reader.GetString(4),reader.IsDBNull(5)?null:reader.GetString(5),
                JsonSerializer.Deserialize<List<Guid>>(reader.GetString(6)) ?? [],DateTimeOffset.Parse(reader.GetString(7))));
        }
        return results;
    }
}
