using System.Data.Common;
using AmeCapture.Application.Interfaces;
using Microsoft.Data.Sqlite;

namespace AmeCapture.Infrastructure.Database;

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly string _databasePath;

    public SqliteConnectionFactory(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = $"Data Source={databasePath};Pooling=False";
    }

    public string DatabasePath => _databasePath;

    public async Task<DbConnection> CreateConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        await cmd.ExecuteNonQueryAsync();

        return connection;
    }
}
