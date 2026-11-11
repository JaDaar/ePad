using Microsoft.Data.Sqlite;

namespace ePad;

public sealed record NoteSummary(long Id, string Title, DateTime ModifiedUtc)
{
    public override string ToString() => string.IsNullOrWhiteSpace(Title) ? Strings.T("EmptyNote") : Title;
}

public sealed record Note(long Id, string Body, DateTime CreatedUtc, DateTime ModifiedUtc);

/// <summary>
/// Stores notes as flat plain-text rows in a local SQLite database.
/// </summary>
public sealed class NoteStore
{
    private readonly string _connectionString;

    public string DatabasePath { get; }

    public NoteStore(string databasePath)
    {
        DatabasePath = databasePath;
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        EnsureSchema();
    }

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ePad", "notes.db");

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void EnsureSchema()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS notes (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                title        TEXT NOT NULL DEFAULT '',
                body         TEXT NOT NULL DEFAULT '',
                created_utc  TEXT NOT NULL,
                modified_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_notes_modified ON notes(modified_utc DESC);
            CREATE TABLE IF NOT EXISTS settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public string? GetSetting(string key)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $k";
        cmd.Parameters.AddWithValue("$k", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value) VALUES ($k, $v)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.ExecuteNonQuery();
    }

    public List<NoteSummary> List(string? search = null)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, title, modified_utc FROM notes";
        if (!string.IsNullOrWhiteSpace(search))
        {
            cmd.CommandText += " WHERE body LIKE $q ESCAPE '\\'";
            var escaped = search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            cmd.Parameters.AddWithValue("$q", $"%{escaped}%");
        }
        cmd.CommandText += " ORDER BY modified_utc DESC";

        var result = new List<NoteSummary>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new NoteSummary(reader.GetInt64(0), reader.GetString(1), ParseUtc(reader.GetString(2))));
        return result;
    }

    public Note? Get(long id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, body, created_utc, modified_utc FROM notes WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Note(reader.GetInt64(0), reader.GetString(1), ParseUtc(reader.GetString(2)), ParseUtc(reader.GetString(3)));
    }

    public long Create(string body = "")
    {
        var now = Now();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notes (title, body, created_utc, modified_utc) VALUES ($t, $b, $now, $now);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$t", TitleFrom(body));
        cmd.Parameters.AddWithValue("$b", body);
        cmd.Parameters.AddWithValue("$now", now);
        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(long id, string body)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET title = $t, body = $b, modified_utc = $now WHERE id = $id";
        cmd.Parameters.AddWithValue("$t", TitleFrom(body));
        cmd.Parameters.AddWithValue("$b", body);
        cmd.Parameters.AddWithValue("$now", Now());
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM notes WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>The title is the first non-blank line of the note, trimmed.</summary>
    public static string TitleFrom(string body)
    {
        foreach (var line in body.Split('\n'))
        {
            var t = line.Trim();
            if (t.Length > 0) return t.Length > 80 ? t[..80] : t;
        }
        return string.Empty;
    }

    private static string Now() => DateTime.UtcNow.ToString("o");

    private static DateTime ParseUtc(string s) =>
        DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
}
