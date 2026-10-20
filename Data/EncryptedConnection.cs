using Microsoft.Data.Sqlite;

namespace SQLiteEncrypted.MauiMaintenanceLog.Data;

internal sealed class SeeActivationException : InvalidOperationException
{
    internal SeeActivationException()
        : base(
            "SEE activation failed. Verify that the activation key is valid for " +
            "this platform, then restart the application before trying again.")
    {
    }
}

internal static class EncryptedConnection
{
    internal static SqliteConnection Open(
        string path,
        string activationKey,
        string databaseKey)
    {
        if (string.IsNullOrWhiteSpace(activationKey))
        {
            throw new ArgumentException("The SQLite.Encrypted activation key is required.");
        }
        if (activationKey.StartsWith("see-", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Enter the activation key without the see- PRAGMA prefix.");
        }
        if (string.IsNullOrWhiteSpace(databaseKey))
        {
            throw new ArgumentException("The database passphrase is required.");
        }

        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString());
        try
        {
            connection.Open();
            ExecuteNonQuery(connection, "PRAGMA busy_timeout = 5000");
            ExecuteNonQuery(
                connection,
                $"PRAGMA activate_extensions = {SqlLiteral($"see-{activationKey}")}");
            var keyResult = ExecuteScalar(
                connection,
                $"PRAGMA key = {SqlLiteral(databaseKey)}") as string;
            if (!string.Equals(keyResult, "ok", StringComparison.Ordinal))
            {
                throw new SeeActivationException();
            }

            _ = ExecuteScalar(connection, "SELECT count(*) FROM sqlite_master");
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static object? ExecuteScalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static string SqlLiteral(string value) =>
        $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}

