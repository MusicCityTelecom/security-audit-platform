using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace SecurityAuditPlatform.Infrastructure.Settings;

public sealed record SettingDescriptor(string Key, string Category, string Description, bool IsSecret, string? Value, DateTimeOffset UpdatedAt);
public sealed record SetSettingRequest(string Key, string Category, string? Value, bool IsSecret = false, string? Description = null);

public sealed class SettingsService
{
    private readonly Data.PlatformDatabase _database;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SecurityAuditPlatform/settings/v1");

    public SettingsService(Data.PlatformDatabase database) => _database = database;

    public IReadOnlyList<SettingDescriptor> List()
    {
        using var c = _database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT key,category,value,is_secret,description,updated_at FROM settings ORDER BY category,key;";
        using var r = cmd.ExecuteReader();
        var result = new List<SettingDescriptor>();
        while (r.Read())
        {
            var secret = r.GetInt32(3) != 0;
            result.Add(new SettingDescriptor(r.GetString(0), r.GetString(1),
                r.IsDBNull(4) ? "" : r.GetString(4), secret, secret ? null : Unprotect(r.GetString(2)),
                DateTimeOffset.Parse(r.GetString(5))));
        }
        return result;
    }

    public SettingDescriptor Set(SetSettingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || request.Key.Length > 200)
            throw new ArgumentException("A setting key between 1 and 200 characters is required.");
        var value = request.Value ?? "";
        var stored = Protect(value);
        using var c = _database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO settings(key,category,value,is_secret,description,updated_at)
VALUES($key,$category,$value,$secret,$description,$updated)
ON CONFLICT(key) DO UPDATE SET category=$category,value=$value,is_secret=$secret,description=$description,updated_at=$updated;";
        cmd.Parameters.AddWithValue("$key", request.Key);
        cmd.Parameters.AddWithValue("$category", string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category);
        cmd.Parameters.AddWithValue("$value", stored);
        cmd.Parameters.AddWithValue("$secret", request.IsSecret ? 1 : 0);
        cmd.Parameters.AddWithValue("$description", (object?)request.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
        return new SettingDescriptor(request.Key, request.Category, request.Description ?? "", request.IsSecret, request.IsSecret ? null : value, DateTimeOffset.UtcNow);
    }

    public bool Delete(string key)
    {
        using var c = _database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM settings WHERE key=$key;";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteNonQuery() > 0;
    }

    public string? GetValue(string key)
    {
        using var c = _database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT value,is_secret FROM settings WHERE key=$key;";
        cmd.Parameters.AddWithValue("$key", key);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return Unprotect(r.GetString(0));
    }

    private static string Protect(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser));
        }
        catch (CryptographicException)
        {
            return "";
        }
    }
}
