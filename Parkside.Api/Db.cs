using Microsoft.Data.Sqlite;

namespace Parkside.Api;

public static class Db
{
    // Resolve the db file relative to the repo root, not the process cwd.
    public static readonly string DbPath = Path.Combine(RepoRoot(), "parkside.db");

    public static SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        return connection;
    }

    // Runs a query and maps each row. Parameters are named, e.g. ("$date", date).
    public static List<T> Query<T>(
        this SqliteConnection db,
        string sql,
        Func<SqliteDataReader, T> map,
        params (string Name, object Value)[] parameters)
    {
        using var command = db.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        using var reader = command.ExecuteReader();
        var rows = new List<T>();
        while (reader.Read())
        {
            rows.Add(map(reader));
        }
        return rows;
    }

    // Walks up from the build output until it finds the folder holding Parkside.sln.
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Parkside.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repo root (no Parkside.sln above the build output).");
    }
}

public record SessionRow(
    int Id,
    string Activity,
    string Date,
    string StartTime,
    int DurationMinutes,
    int Capacity);

public record BookingRow(
    int Id,
    int SessionId,
    string GuestName,
    int PartySize,
    string Status,
    string CreatedAt);
