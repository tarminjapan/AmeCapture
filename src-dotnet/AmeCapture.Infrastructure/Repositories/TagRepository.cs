using System.Data.Common;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TagRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM tags ORDER BY name";

        var tags = new List<Tag>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tags.Add(ReadTag(reader));
        }

        return tags.AsReadOnly();
    }

    public async Task<Tag?> GetByIdAsync(string id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM tags WHERE id = @id";
        AddParameter(command, "@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadTag(reader);
        }

        return null;
    }

    public async Task<Tag?> FindByNameAsync(string name)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM tags WHERE name = @name";
        AddParameter(command, "@name", name);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadTag(reader);
        }

        return null;
    }

    public async Task AddAsync(Tag tag)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO tags (id, name) VALUES (@id, @name)";
        AddParameter(command, "@id", tag.Id);
        AddParameter(command, "@name", tag.Name);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tags WHERE id = @id";
        AddParameter(command, "@id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<Tag>> GetTagsForItemAsync(string itemId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT t.id, t.name FROM tags t
            INNER JOIN workspace_item_tags wit ON t.id = wit.tag_id
            WHERE wit.workspace_item_id = @itemId
            ORDER BY t.name";
        AddParameter(command, "@itemId", itemId);

        var tags = new List<Tag>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tags.Add(ReadTag(reader));
        }

        return tags.AsReadOnly();
    }

    public async Task AddTagToItemAsync(string itemId, string tagId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR IGNORE INTO workspace_item_tags (workspace_item_id, tag_id) VALUES (@itemId, @tagId)";
        AddParameter(command, "@itemId", itemId);
        AddParameter(command, "@tagId", tagId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveTagFromItemAsync(string itemId, string tagId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM workspace_item_tags WHERE workspace_item_id = @itemId AND tag_id = @tagId";
        AddParameter(command, "@itemId", itemId);
        AddParameter(command, "@tagId", tagId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task SetTagsForItemAsync(string itemId, IEnumerable<string> tagIds)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText =
                    "DELETE FROM workspace_item_tags WHERE workspace_item_id = @itemId";
                AddParameter(deleteCommand, "@itemId", itemId);
                await deleteCommand.ExecuteNonQueryAsync();
            }

            using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
                    INSERT OR IGNORE INTO workspace_item_tags (workspace_item_id, tag_id) VALUES (@itemId, @tagId)";
                AddParameter(insertCommand, "@itemId", itemId);
                var tagParam = insertCommand.CreateParameter();
                tagParam.ParameterName = "@tagId";
                insertCommand.Parameters.Add(tagParam);

                foreach (var tagId in tagIds)
                {
                    tagParam.Value = tagId;
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetItemIdsByTagAsync(string tagId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT workspace_item_id FROM workspace_item_tags WHERE tag_id = @tagId";
        AddParameter(command, "@tagId", tagId);

        var ids = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetString(0));
        }

        return ids.AsReadOnly();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<Tag>>> GetAllTagsForItemsAsync(
        IEnumerable<string> itemIds)
    {
        var idList = itemIds.ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<Tag>>();
        }

        var map = new Dictionary<string, IReadOnlyList<Tag>>();
        foreach (var id in idList)
        {
            map[id] = new List<Tag>();
        }

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
            SELECT wit.workspace_item_id, t.id, t.name
            FROM workspace_item_tags wit
            INNER JOIN tags t ON t.id = wit.tag_id
            WHERE wit.workspace_item_id IN ({string.Join(", ", placeholders)})
            ORDER BY t.name";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var itemId = reader.GetString(0);
                var tag = new Tag { Id = reader.GetString(1), Name = reader.GetString(2) };
                ((List<Tag>)map[itemId]).Add(tag);
            }
        }

        return map;
    }

    private static Tag ReadTag(DbDataReader reader)
    {
        return new Tag { Id = reader.GetString(0), Name = reader.GetString(1) };
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}
