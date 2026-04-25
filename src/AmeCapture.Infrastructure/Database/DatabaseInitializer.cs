using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Database;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IDbConnectionFactory connectionFactory)
    {
        using var connection = await connectionFactory.CreateConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS workspace_items (
                id TEXT PRIMARY KEY,
                type TEXT NOT NULL DEFAULT 'image',
                original_path TEXT NOT NULL,
                current_path TEXT NOT NULL,
                thumbnail_path TEXT,
                title TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                is_favorite INTEGER NOT NULL DEFAULT 0,
                metadata_json TEXT
            );

            CREATE TABLE IF NOT EXISTS tags (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS workspace_item_tags (
                workspace_item_id TEXT NOT NULL,
                tag_id TEXT NOT NULL,
                PRIMARY KEY (workspace_item_id, tag_id),
                FOREIGN KEY (workspace_item_id) REFERENCES workspace_items(id) ON DELETE CASCADE,
                FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS app_settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_workspace_items_created_at
                ON workspace_items(created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_workspace_items_is_favorite
                ON workspace_items(is_favorite);";
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
}
