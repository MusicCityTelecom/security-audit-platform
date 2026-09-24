using Microsoft.Data.Sqlite;
using SecurityAuditPlatform.Core.Jobs;

namespace SecurityAuditPlatform.Infrastructure.Data;

public sealed class PlatformDatabase
{
    private readonly string _connectionString;
    public PlatformDatabase(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared, ForeignKeys = true }.ToString();
        Initialize();
    }
    public SqliteConnection OpenConnection() { var c = new SqliteConnection(_connectionString); c.Open(); return c; }
    private void Initialize()
    {
        using var c = OpenConnection(); using var cmd = c.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS engagements (id TEXT PRIMARY KEY, name TEXT NOT NULL, created_at TEXT NOT NULL, expires_at TEXT NULL);
CREATE TABLE IF NOT EXISTS scope_targets (engagement_id TEXT NOT NULL, value TEXT NOT NULL, excluded INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (engagement_id, value, excluded), FOREIGN KEY (engagement_id) REFERENCES engagements(id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS jobs (id TEXT PRIMARY KEY, module_id TEXT NOT NULL, engagement_id TEXT NULL, target TEXT NOT NULL, state INTEGER NOT NULL, created_at TEXT NOT NULL, started_at TEXT NULL, finished_at TEXT NULL, exit_code INTEGER NULL, error TEXT NULL);
CREATE TABLE IF NOT EXISTS execution_evidence (job_id TEXT PRIMARY KEY, stdout TEXT NOT NULL, stderr TEXT NOT NULL, collected_at TEXT NOT NULL, sha256 TEXT NOT NULL, FOREIGN KEY (job_id) REFERENCES jobs(id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS findings (id TEXT PRIMARY KEY, title TEXT NOT NULL, description TEXT NOT NULL, severity INTEGER NOT NULL, asset TEXT NULL, remediation TEXT NULL, evidence_ids TEXT NOT NULL, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, category TEXT NOT NULL, value TEXT NOT NULL, is_secret INTEGER NOT NULL DEFAULT 0, description TEXT NULL, updated_at TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_jobs_created_at ON jobs(created_at);
CREATE INDEX IF NOT EXISTS ix_findings_created_at ON findings(created_at);
CREATE INDEX IF NOT EXISTS ix_settings_category ON settings(category);
CREATE TABLE IF NOT EXISTS audit_events (id TEXT PRIMARY KEY, timestamp TEXT NOT NULL, action TEXT NOT NULL, actor TEXT NULL, target TEXT NULL, outcome TEXT NOT NULL, details TEXT NULL);
CREATE INDEX IF NOT EXISTS ix_audit_events_timestamp ON audit_events(timestamp);";
        cmd.ExecuteNonQuery();
    }
    public void SaveJob(Job job)
    {
        using var c = OpenConnection(); using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO jobs(id,module_id,engagement_id,target,state,created_at,started_at,finished_at,exit_code,error) VALUES($id,$module,$engagement,$target,$state,$created,$started,$finished,$exit,$error) ON CONFLICT(id) DO UPDATE SET state=$state,started_at=$started,finished_at=$finished,exit_code=$exit,error=$error;";
        cmd.Parameters.AddWithValue("$id",job.Id.ToString());cmd.Parameters.AddWithValue("$module",job.ModuleId);cmd.Parameters.AddWithValue("$engagement",(object?)job.EngagementId?.ToString()??DBNull.Value);cmd.Parameters.AddWithValue("$target",job.Target);cmd.Parameters.AddWithValue("$state",(int)job.State);cmd.Parameters.AddWithValue("$created",job.CreatedAt.ToString("O"));cmd.Parameters.AddWithValue("$started",(object?)job.StartedAt?.ToString("O")??DBNull.Value);cmd.Parameters.AddWithValue("$finished",(object?)job.FinishedAt?.ToString("O")??DBNull.Value);cmd.Parameters.AddWithValue("$exit",(object?)job.ExitCode??DBNull.Value);cmd.Parameters.AddWithValue("$error",(object?)job.Error??DBNull.Value);cmd.ExecuteNonQuery();
    }
    public IReadOnlyList<Job> ListJobs(int limit=100)
    {
        using var c=OpenConnection();using var cmd=c.CreateCommand();cmd.CommandText="SELECT id,module_id,engagement_id,target,state,created_at,started_at,finished_at,exit_code,error FROM jobs ORDER BY created_at DESC LIMIT $limit;";cmd.Parameters.AddWithValue("$limit",Math.Clamp(limit,1,1000));using var reader=cmd.ExecuteReader();var jobs=new List<Job>();
        while(reader.Read()) jobs.Add(new Job(Guid.Parse(reader.GetString(0)),reader.GetString(1),reader.IsDBNull(2)?null:Guid.Parse(reader.GetString(2)),reader.GetString(3),(JobState)reader.GetInt32(4),DateTimeOffset.Parse(reader.GetString(5)),reader.IsDBNull(6)?null:DateTimeOffset.Parse(reader.GetString(6)),reader.IsDBNull(7)?null:DateTimeOffset.Parse(reader.GetString(7)),reader.IsDBNull(8)?null:reader.GetInt32(8),reader.IsDBNull(9)?null:reader.GetString(9)));
        return jobs;
    }
}
