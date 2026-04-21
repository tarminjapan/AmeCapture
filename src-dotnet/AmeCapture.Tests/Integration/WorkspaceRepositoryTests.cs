using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using AmeCapture.Infrastructure.Database;
using AmeCapture.Infrastructure.Repositories;

namespace AmeCapture.Tests.Integration;

public class WorkspaceRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly WorkspaceRepository _repo;

    public WorkspaceRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        _connectionFactory = new SqliteConnectionFactory(_dbPath);
        _repo = new WorkspaceRepository(_connectionFactory);
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

    private static WorkspaceItem CreateSampleItem(string id)
    {
        return new WorkspaceItem
        {
            Id = id,
            ItemType = WorkspaceItemType.Image,
            OriginalPath = "/original/test.png",
            CurrentPath = "/current/test.png",
            ThumbnailPath = "/thumb/test_thumb.png",
            Title = "Test Item",
            CreatedAt = "2026-01-01T00:00:00Z",
            UpdatedAt = "2026-01-01T00:00:00Z",
            IsFavorite = false,
            MetadataJson = null
        };
    }

    [Fact]
    public async Task AddAndGetById_ReturnsItem()
    {
        var item = CreateSampleItem("id-1");
        await _repo.AddAsync(item);

        var found = await _repo.GetByIdAsync("id-1");

        Assert.NotNull(found);
        Assert.Equal("id-1", found.Id);
        Assert.Equal("Test Item", found.Title);
        Assert.Equal(WorkspaceItemType.Image, found.ItemType);
        Assert.Equal("/original/test.png", found.OriginalPath);
        Assert.Equal("/current/test.png", found.CurrentPath);
        Assert.Equal("/thumb/test_thumb.png", found.ThumbnailPath);
        Assert.False(found.IsFavorite);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        var items = await _repo.GetAllAsync();
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAll_ReturnsMultipleItems()
    {
        await _repo.AddAsync(CreateSampleItem("id-1"));
        await _repo.AddAsync(CreateSampleItem("id-2"));
        await _repo.AddAsync(CreateSampleItem("id-3"));

        var items = await _repo.GetAllAsync();
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task Update_ModifiesItem()
    {
        var item = CreateSampleItem("id-1");
        await _repo.AddAsync(item);

        item.Title = "Updated Title";
        item.IsFavorite = true;
        item.UpdatedAt = "2026-01-02T00:00:00Z";
        await _repo.UpdateAsync(item);

        var found = await _repo.GetByIdAsync("id-1");
        Assert.NotNull(found);
        Assert.Equal("Updated Title", found.Title);
        Assert.True(found.IsFavorite);
        Assert.Equal("2026-01-02T00:00:00Z", found.UpdatedAt);
    }

    [Fact]
    public async Task Delete_RemovesItem()
    {
        await _repo.AddAsync(CreateSampleItem("id-1"));
        Assert.NotNull(await _repo.GetByIdAsync("id-1"));

        await _repo.DeleteAsync("id-1");
        Assert.Null(await _repo.GetByIdAsync("id-1"));
    }

    [Fact]
    public async Task Delete_Nonexistent_IsOk()
    {
        await _repo.DeleteAsync("nonexistent");
    }

    [Fact]
    public async Task VideoType_Roundtrips()
    {
        var item = CreateSampleItem("id-v1");
        item.ItemType = WorkspaceItemType.Video;
        await _repo.AddAsync(item);

        var found = await _repo.GetByIdAsync("id-v1");
        Assert.NotNull(found);
        Assert.Equal(WorkspaceItemType.Video, found.ItemType);
    }

    [Fact]
    public async Task NullOptionalFields_Roundtrip()
    {
        var item = new WorkspaceItem
        {
            Id = "id-opt",
            ItemType = WorkspaceItemType.Image,
            OriginalPath = "/original/test.png",
            CurrentPath = "/current/test.png",
            ThumbnailPath = null,
            Title = "No Thumbnail",
            CreatedAt = "2026-01-01T00:00:00Z",
            UpdatedAt = "2026-01-01T00:00:00Z",
            IsFavorite = false,
            MetadataJson = null
        };
        await _repo.AddAsync(item);

        var found = await _repo.GetByIdAsync("id-opt");
        Assert.NotNull(found);
        Assert.Null(found.ThumbnailPath);
        Assert.Null(found.MetadataJson);
    }

    [Fact]
    public async Task GetByIds_ReturnsMatchingItems()
    {
        await _repo.AddAsync(CreateSampleItem("id-1"));
        await _repo.AddAsync(CreateSampleItem("id-2"));
        await _repo.AddAsync(CreateSampleItem("id-3"));

        var items = await _repo.GetByIdsAsync(["id-1", "id-3"]);
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.True(i.Id == "id-1" || i.Id == "id-3"));
    }

    [Fact]
    public async Task GetByIds_Empty_ReturnsEmptyList()
    {
        var items = await _repo.GetByIdsAsync([]);
        Assert.Empty(items);
    }
}
