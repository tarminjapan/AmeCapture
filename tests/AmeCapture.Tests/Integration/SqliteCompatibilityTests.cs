using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using AmeCapture.Infrastructure.Database;
using AmeCapture.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace AmeCapture.Tests.Integration;

public class SqliteCompatibilityTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly WorkspaceRepository _workspaceRepo;
    private readonly TagRepository _tagRepo;
    private readonly SettingsRepository _settingsRepo;

    public SqliteCompatibilityTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        _connectionFactory = new SqliteConnectionFactory(_dbPath);
        _workspaceRepo = new WorkspaceRepository(_connectionFactory);
        _tagRepo = new TagRepository(_connectionFactory);
        _settingsRepo = new SettingsRepository(_connectionFactory);
    }

    public async Task InitializeAsync()
    {
        await DatabaseInitializer.InitializeAsync(_connectionFactory);
    }

    public Task DisposeAsync()
    {
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DatabaseInitializer_CreatesAllTables()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

        var tables = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("workspace_items", tables);
        Assert.Contains("tags", tables);
        Assert.Contains("workspace_item_tags", tables);
        Assert.Contains("app_settings", tables);
    }

    [Fact]
    public async Task DatabaseInitializer_CreatesIndexes()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type='index' AND name LIKE 'idx_%' ORDER BY name";

        var indexes = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        Assert.Contains("idx_workspace_items_created_at", indexes);
        Assert.Contains("idx_workspace_items_is_favorite", indexes);
    }

    [Fact]
    public async Task DatabaseInitializer_IsIdempotent()
    {
        await DatabaseInitializer.InitializeAsync(_connectionFactory);
        await DatabaseInitializer.InitializeAsync(_connectionFactory);

        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        var count = Convert.ToInt64(await command.ExecuteScalarAsync());
        Assert.Equal(4L, count);
    }

    private static WorkspaceItem CreateSampleItem(string id) => new()
    {
        Id = id,
        ItemType = WorkspaceItemType.Image,
        OriginalPath = "/a.png",
        CurrentPath = "/a.png",
        Title = "test",
        CreatedAt = "2026-01-01",
        UpdatedAt = "2026-01-01"
    };

    [Fact]
    public async Task ForeignKey_CascadeDelete_RemovesTagAssociations()
    {
        await _workspaceRepo.AddAsync(CreateSampleItem("w1"));
        await _tagRepo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _tagRepo.AddTagToItemAsync("w1", "t1");

        var tags = await _tagRepo.GetTagsForItemAsync("w1");
        Assert.Single(tags);

        await _workspaceRepo.DeleteAsync("w1");

        var tagsAfterDelete = await _tagRepo.GetTagsForItemAsync("w1");
        Assert.Empty(tagsAfterDelete);
    }

    [Fact]
    public async Task ForeignKey_PreventsInvalidTagReference()
    {
        await _workspaceRepo.AddAsync(CreateSampleItem("w1"));

        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO workspace_item_tags (workspace_item_id, tag_id) VALUES ('w1', 'nonexistent')";

        await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync());
    }
}
