using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SecurityAuditPlatform.Infrastructure.Audit;

public sealed record AuditEvent(Guid Id, DateTimeOffset Timestamp, string Action, string? Actor, string? Target, string Outcome, string? Details);
public sealed class AuditLogService
{
    private readonly Data.PlatformDatabase _database;
    public AuditLogService(Data.PlatformDatabase database) => _database = database;

    public void Write(string action, string outcome, string? actor = null, string? target = null, object? details = null)
    {
        using var c = _database.OpenConnection(); using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO audit_events(id,timestamp,action,actor,target,outcome,details) VALUES($id,$ts,$action,$actor,$target,$outcome,$details);";
        cmd.Parameters.AddWithValue("$id",Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("$ts",DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$action",action);
        cmd.Parameters.AddWithValue("$actor",(object?)actor??DBNull.Value);
        cmd.Parameters.AddWithValue("$target",(object?)target??DBNull.Value);
        cmd.Parameters.AddWithValue("$outcome",outcome);
        cmd.Parameters.AddWithValue("$details",(object?)(details is null?null:JsonSerializer.Serialize(details))??DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<AuditEvent> List(int limit=500)
    {
        using var c=_database.OpenConnection();using var cmd=c.CreateCommand();cmd.CommandText="SELECT id,timestamp,action,actor,target,outcome,details FROM audit_events ORDER BY timestamp DESC LIMIT $limit;";cmd.Parameters.AddWithValue("$limit",Math.Clamp(limit,1,5000));using var r=cmd.ExecuteReader();var result=new List<AuditEvent>();
        while(r.Read()) result.Add(new AuditEvent(Guid.Parse(r.GetString(0)),DateTimeOffset.Parse(r.GetString(1)),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.IsDBNull(4)?null:r.GetString(4),r.GetString(5),r.IsDBNull(6)?null:r.GetString(6)));
        return result;
    }
}
