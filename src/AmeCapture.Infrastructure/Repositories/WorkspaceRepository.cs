using System.Data.Common;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Infrastructure.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WorkspaceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<WorkspaceItem>> GetAllAsync()
    {
        Serilog.Log.Debug("WorkspaceRepository.GetAllAsync");
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, type, original_path, current_path, thumbnail_path,
                   title, created_at, updated_at, is_favorite, metadata_json
            FROM workspace_items
            ORDER BY created_at DESC";

        var items = new List<WorkspaceItem>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadWorkspaceItem(reader));
        }

        Serilog.Log.Debug("WorkspaceRepository.GetAllAsync: returned {Count} items", items.Count);
        return items.AsReadOnly();
    }

    public async Task<WorkspaceItem?> GetByIdAsync(string id)
    {
        Serilog.Log.Debug("WorkspaceRepository.GetByIdAsync: {ItemId}", id);
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, type, original_path, current_path, thumbnail_path,
                   title, created_at, updated_at, is_favorite, metadata_json
            FROM workspace_items WHERE id = @id";
        AddParameter(command, "@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var item = ReadWorkspaceItem(reader);
            Serilog.Log.Debug("WorkspaceRepository.GetByIdAsync: found item {ItemId}", id);
            return item;
        }

        Serilog.Log.Debug("WorkspaceRepository.GetByIdAsync: item not found {ItemId}", id);
        return null;
    }

    public async Task AddAsync(WorkspaceItem item)
    {
        Serilog.Log.Debug("WorkspaceRepository.AddAsync: {ItemId}, title={Title}", item.Id, item.Title);
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO workspace_items
                (id, type, original_path, current_path, thumbnail_path,
                 title, created_at, updated_at, is_favorite, metadata_json)
            VALUES (@id, @type, @original_path, @current_path, @thumbnail_path,
                    @title, @created_at, @updated_at, @is_favorite, @metadata_json)";

        AddParameter(command, "@id", item.Id);
        AddParameter(command, "@type", ItemTypeToString(item.ItemType));
        AddParameter(command, "@original_path", item.OriginalPath);
        AddParameter(command, "@current_path", item.CurrentPath);
        AddParameter(command, "@thumbnail_path", (object?)item.ThumbnailPath ?? DBNull.Value);
        AddParameter(command, "@title", item.Title);
        AddParameter(command, "@created_at", item.CreatedAt);
        AddParameter(command, "@updated_at", item.UpdatedAt);
        AddParameter(command, "@is_favorite", item.IsFavorite ? 1 : 0);
        AddParameter(command, "@metadata_json", (object?)item.MetadataJson ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
        Serilog.Log.Debug("WorkspaceRepository.AddAsync: item {ItemId} inserted", item.Id);
    }

    public async Task UpdateAsync(WorkspaceItem item)
    {
        Serilog.Log.Debug("WorkspaceRepository.UpdateAsync: {ItemId}", item.Id);
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE workspace_items SET
                type = @type, original_path = @original_path, current_path = @current_path,
                thumbnail_path = @thumbnail_path, title = @title, updated_at = @updated_at,
                is_favorite = @is_favorite, metadata_json = @metadata_json
            WHERE id = @id";

        AddParameter(command, "@type", ItemTypeToString(item.ItemType));
        AddParameter(command, "@original_path", item.OriginalPath);
        AddParameter(command, "@current_path", item.CurrentPath);
        AddParameter(command, "@thumbnail_path", (object?)item.ThumbnailPath ?? DBNull.Value);
        AddParameter(command, "@title", item.Title);
        AddParameter(command, "@updated_at", item.UpdatedAt);
        AddParameter(command, "@is_favorite", item.IsFavorite ? 1 : 0);
        AddParameter(command, "@metadata_json", (object?)item.MetadataJson ?? DBNull.Value);
        AddParameter(command, "@id", item.Id);

        await command.ExecuteNonQueryAsync();
        Serilog.Log.Debug("WorkspaceRepository.UpdateAsync: item {ItemId} updated", item.Id);
    }

    public async Task DeleteAsync(string id)
    {
        Serilog.Log.Debug("WorkspaceRepository.DeleteAsync: {ItemId}", id);
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM workspace_items WHERE id = @id";
        AddParameter(command, "@id", id);

        await command.ExecuteNonQueryAsync();
        Serilog.Log.Debug("WorkspaceRepository.DeleteAsync: item {ItemId} deleted", id);
    }

    public async Task<IReadOnlyList<WorkspaceItem>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<WorkspaceItem>();
        }

        var allItems = new List<WorkspaceItem>();
        const int chunkSize = 500;

        for (var chunkStart = 0; chunkStart < idList.Count; chunkStart += chunkSize)
        {
            var chunk = idList.Skip(chunkStart).Take(chunkSize).ToList();

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();

            var placeholders = new List<string>();
            for (var i = 0; i < chunk.Count; i++)
            {
                var paramName = $"@id{i}";
                placeholders.Add(paramName);
                AddParameter(command, paramName, chunk[i]);
            }

            command.CommandText = $@"
            SELECT id, type, original_path, current_path, thumbnail_path,
                   title, created_at, updated_at, is_favorite, metadata_json
            FROM workspace_items WHERE id IN ({string.Join(", ", placeholders)})";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                allItems.Add(ReadWorkspaceItem(reader));
            }
        }

        return allItems.AsReadOnly();
    }

    private static WorkspaceItem ReadWorkspaceItem(DbDataReader reader)
    {
        var typeStr = reader.GetString(1);
        return new WorkspaceItem
        {
            Id = reader.GetString(0),
            ItemType = typeStr == "video" ? WorkspaceItemType.Video : WorkspaceItemType.Image,
            OriginalPath = reader.GetString(2),
            CurrentPath = reader.GetString(3),
            ThumbnailPath = reader.IsDBNull(4) ? null : reader.GetString(4),
            Title = reader.GetString(5),
            CreatedAt = reader.GetString(6),
            UpdatedAt = reader.GetString(7),
            IsFavorite = reader.GetInt32(8) != 0,
            MetadataJson = reader.IsDBNull(9) ? null : reader.GetString(9)
        };
    }

    private static string ItemTypeToString(WorkspaceItemType type)
    {
        return type == WorkspaceItemType.Video ? "video" : "image";
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}
