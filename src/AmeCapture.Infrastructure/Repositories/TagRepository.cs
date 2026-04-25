using System.Data.Common;
using System.Text.Json;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Infrastructure.Repositories
{
    public class TagRepository(IDbConnectionFactory connectionFactory) : ITagRepository
    {
        private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

        public async Task<IReadOnlyList<Tag>> GetAllAsync()
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT id, name FROM tags ORDER BY name";

            var tags = new List<Tag>();
            using DbDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(ReadTag(reader));
            }

            return tags.AsReadOnly();
        }

        public async Task<Tag?> GetByIdAsync(string id)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT id, name FROM tags WHERE id = @id";
            AddParameter(command, "@id", id);

            using DbDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadTag(reader) : null;
        }

        public async Task<Tag?> FindByNameAsync(string name)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT id, name FROM tags WHERE name = @name";
            AddParameter(command, "@name", name);

            using DbDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadTag(reader) : null;
        }

        public async Task AddAsync(Tag tag)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO tags (id, name) VALUES (@id, @name)";
            AddParameter(command, "@id", tag.Id);
            AddParameter(command, "@name", tag.Name);

            _ = await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string id)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM tags WHERE id = @id";
            AddParameter(command, "@id", id);

            _ = await command.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<Tag>> GetTagsForItemAsync(string itemId)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = @"
            SELECT t.id, t.name FROM tags t
            INNER JOIN workspace_item_tags wit ON t.id = wit.tag_id
            WHERE wit.workspace_item_id = @itemId
            ORDER BY t.name";
            AddParameter(command, "@itemId", itemId);

            var tags = new List<Tag>();
            using DbDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(ReadTag(reader));
            }

            return tags.AsReadOnly();
        }

        public async Task AddTagToItemAsync(string itemId, string tagId)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = @"
            INSERT OR IGNORE INTO workspace_item_tags (workspace_item_id, tag_id) VALUES (@itemId, @tagId)";
            AddParameter(command, "@itemId", itemId);
            AddParameter(command, "@tagId", tagId);

            _ = await command.ExecuteNonQueryAsync();
        }

        public async Task RemoveTagFromItemAsync(string itemId, string tagId)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM workspace_item_tags WHERE workspace_item_id = @itemId AND tag_id = @tagId";
            AddParameter(command, "@itemId", itemId);
            AddParameter(command, "@tagId", tagId);

            _ = await command.ExecuteNonQueryAsync();
        }

        public async Task SetTagsForItemAsync(string itemId, IEnumerable<string> tagIds)
        {
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbTransaction transaction = await connection.BeginTransactionAsync();

            try
            {
                using (DbCommand deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.Transaction = transaction;
                    deleteCommand.CommandText =
                        "DELETE FROM workspace_item_tags WHERE workspace_item_id = @itemId";
                    AddParameter(deleteCommand, "@itemId", itemId);
                    _ = await deleteCommand.ExecuteNonQueryAsync();
                }

                using (DbCommand insertCommand = connection.CreateCommand())
                {
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = @"
                    INSERT OR IGNORE INTO workspace_item_tags (workspace_item_id, tag_id) VALUES (@itemId, @tagId)";
                    AddParameter(insertCommand, "@itemId", itemId);
                    DbParameter tagParam = insertCommand.CreateParameter();
                    tagParam.ParameterName = "@tagId";
                    _ = insertCommand.Parameters.Add(tagParam);

                    foreach (string tagId in tagIds)
                    {
                        tagParam.Value = tagId;
                        _ = await insertCommand.ExecuteNonQueryAsync();
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
            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT workspace_item_id FROM workspace_item_tags WHERE tag_id = @tagId";
            AddParameter(command, "@tagId", tagId);

            var ids = new List<string>();
            using DbDataReader reader = await command.ExecuteReaderAsync();
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

            var map = new Dictionary<string, List<Tag>>();
            foreach (string? id in idList)
            {
                map[id] = [];
            }

            using DbConnection connection = await _connectionFactory.CreateConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = @"
            SELECT wit.workspace_item_id, t.id, t.name
            FROM workspace_item_tags wit
            INNER JOIN tags t ON t.id = wit.tag_id
            WHERE wit.workspace_item_id IN (SELECT value FROM json_each(@ids))
            ORDER BY t.name";
            AddParameter(command, "@ids", JsonSerializer.Serialize(idList));

            using DbDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string itemId = reader.GetString(0);
                var tag = new Tag { Id = reader.GetString(1), Name = reader.GetString(2) };
                map[itemId].Add(tag);
            }

            return map.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<Tag>)kvp.Value);
        }

        private static Tag ReadTag(DbDataReader reader)
        {
            return new Tag { Id = reader.GetString(0), Name = reader.GetString(1) };
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            DbParameter param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            _ = command.Parameters.Add(param);
        }
    }
}
