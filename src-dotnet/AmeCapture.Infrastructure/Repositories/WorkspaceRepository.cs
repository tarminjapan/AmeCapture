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

        return items.AsReadOnly();
    }

    public async Task<WorkspaceItem?> GetByIdAsync(string id)
    {
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
            return ReadWorkspaceItem(reader);
        }

        return null;
    }

    public async Task AddAsync(WorkspaceItem item)
    {
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
    }

    public async Task UpdateAsync(WorkspaceItem item)
    {
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
    }

    public async Task DeleteAsync(string id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM workspace_items WHERE id = @id";
        AddParameter(command, "@id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<WorkspaceItem>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<WorkspaceItem>();
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();

        var placeholders = new List<string>();
        for (var i = 0; i < idList.Count; i++)
        {
            var paramName = $"@id{i}";
            placeholders.Add(paramName);
            AddParameter(command, paramName, idList[i]);
        }

        command.CommandText = $@"
            SELECT id, type, original_path, current_path, thumbnail_path,
                   title, created_at, updated_at, is_favorite, metadata_json
            FROM workspace_items WHERE id IN ({string.Join(", ", placeholders)})";

        var items = new List<WorkspaceItem>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadWorkspaceItem(reader));
        }

        return items.AsReadOnly();
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
