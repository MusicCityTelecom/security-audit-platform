using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace SecurityAuditPlatform.Infrastructure.Data;

public sealed record ExecutionEvidence(Guid JobId, string StandardOutput, string StandardError, DateTimeOffset CollectedAt, string OutputSha256);

public sealed class ExecutionEvidenceStore
{
    private readonly PlatformDatabase _database;
    public ExecutionEvidenceStore(PlatformDatabase database) => _database = database;

    public void Save(Guid jobId, string stdout, string stderr, DateTimeOffset collectedAt)
    {
        var bytes = Encoding.UTF8.GetBytes(stdout + "\n" + stderr);
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO execution_evidence(job_id,stdout,stderr,collected_at,sha256)
VALUES($job,$stdout,$stderr,$collected,$sha)
ON CONFLICT(job_id) DO UPDATE SET stdout=$stdout,stderr=$stderr,collected_at=$collected,sha256=$sha;";
        command.Parameters.AddWithValue("$job", jobId.ToString());
        command.Parameters.AddWithValue("$stdout", stdout);
        command.Parameters.AddWithValue("$stderr", stderr);
        command.Parameters.AddWithValue("$collected", collectedAt.ToString("O"));
        command.Parameters.AddWithValue("$sha", sha);
        command.ExecuteNonQuery();
    }

    public ExecutionEvidence? Get(Guid jobId)
    {
        using var connection = _database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT stdout,stderr,collected_at,sha256 FROM execution_evidence WHERE job_id=$job;";
        command.Parameters.AddWithValue("$job", jobId.ToString());
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new ExecutionEvidence(jobId, reader.GetString(0), reader.GetString(1), DateTimeOffset.Parse(reader.GetString(2)), reader.GetString(3));
    }
}
