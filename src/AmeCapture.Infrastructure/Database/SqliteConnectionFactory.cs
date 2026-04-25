using System.Data.Common;
using AmeCapture.Application.Interfaces;
using Microsoft.Data.Sqlite;

namespace AmeCapture.Infrastructure.Database
{
    public class SqliteConnectionFactory(string databasePath) : IDbConnectionFactory
    {
        private readonly string _connectionString = $"Data Source={databasePath};Pooling=False";

        public string DatabasePath { get; } = databasePath;

        public async Task<DbConnection> CreateConnectionAsync()
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            _ = await cmd.ExecuteNonQueryAsync();

            return connection;
        }
    }
}
