using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using AmeCapture.Infrastructure.Database;
using AmeCapture.Infrastructure.Repositories;

namespace AmeCapture.Tests.Integration;

public class TagRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly TagRepository _repo;
    private readonly WorkspaceRepository _workspaceRepo;

    public TagRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        _connectionFactory = new SqliteConnectionFactory(_dbPath);
        _repo = new TagRepository(_connectionFactory);
        _workspaceRepo = new WorkspaceRepository(_connectionFactory);
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

    private async Task InsertWorkspaceItem(string id)
    {
        await _workspaceRepo.AddAsync(new WorkspaceItem
        {
            Id = id,
            ItemType = WorkspaceItemType.Image,
            OriginalPath = "/a.png",
            CurrentPath = "/a.png",
            Title = "test",
            CreatedAt = "2026-01-01",
            UpdatedAt = "2026-01-01"
        });
    }

    [Fact]
    public async Task AddAndGetAll_ReturnsTags()
    {
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _repo.AddAsync(new Tag { Id = "t2", Name = "tag2" });

        var tags = await _repo.GetAllAsync();
        Assert.Equal(2, tags.Count);
        Assert.Equal("tag1", tags[0].Name);
        Assert.Equal("tag2", tags[1].Name);
    }

    [Fact]
    public async Task GetById_ReturnsTag()
    {
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });

        var found = await _repo.GetByIdAsync("t1");
        Assert.NotNull(found);
        Assert.Equal("tag1", found.Name);

        Assert.Null(await _repo.GetByIdAsync("nonexistent"));
    }

    [Fact]
    public async Task FindByName_ReturnsTag()
    {
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });

        var found = await _repo.FindByNameAsync("tag1");
        Assert.NotNull(found);

        Assert.Null(await _repo.FindByNameAsync("nonexistent"));
    }

    [Fact]
    public async Task Delete_RemovesTag()
    {
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _repo.DeleteAsync("t1");
        Assert.Null(await _repo.GetByIdAsync("t1"));
    }

    [Fact]
    public async Task AddTagToItem_And_GetTagsForItem()
    {
        await InsertWorkspaceItem("w1");
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _repo.AddAsync(new Tag { Id = "t2", Name = "tag2" });

        await _repo.AddTagToItemAsync("w1", "t1");
        await _repo.AddTagToItemAsync("w1", "t2");

        var tags = await _repo.GetTagsForItemAsync("w1");
        Assert.Equal(2, tags.Count);
    }

    [Fact]
    public async Task RemoveTagFromItem()
    {
        await InsertWorkspaceItem("w1");
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _repo.AddTagToItemAsync("w1", "t1");

        Assert.Single(await _repo.GetTagsForItemAsync("w1"));

        await _repo.RemoveTagFromItemAsync("w1", "t1");
        Assert.Empty(await _repo.GetTagsForItemAsync("w1"));
    }

    [Fact]
    public async Task SetTagsForItem_ReplacesExistingTags()
    {
        await InsertWorkspaceItem("w1");
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _repo.AddAsync(new Tag { Id = "t2", Name = "tag2" });
        await _repo.AddAsync(new Tag { Id = "t3", Name = "tag3" });

        await _repo.SetTagsForItemAsync("w1", ["t1", "t2"]);
        var tags = await _repo.GetTagsForItemAsync("w1");
        Assert.Equal(2, tags.Count);

        await _repo.SetTagsForItemAsync("w1", ["t3"]);
        tags = await _repo.GetTagsForItemAsync("w1");
        Assert.Single(tags);
        Assert.Equal("tag3", tags[0].Name);
    }

    [Fact]
    public async Task GetItemIdsByTag()
    {
        await InsertWorkspaceItem("w1");
        await InsertWorkspaceItem("w2");
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _repo.AddTagToItemAsync("w1", "t1");
        await _repo.AddTagToItemAsync("w2", "t1");

        var ids = await _repo.GetItemIdsByTagAsync("t1");
        Assert.Equal(2, ids.Count);
        Assert.Contains("w1", ids);
        Assert.Contains("w2", ids);
    }

    [Fact]
    public async Task GetAllTagsForItems()
    {
        await InsertWorkspaceItem("w1");
        await InsertWorkspaceItem("w2");
        await _repo.AddAsync(new Tag { Id = "t1", Name = "tag1" });
        await _repo.AddAsync(new Tag { Id = "t2", Name = "tag2" });
        await _repo.AddTagToItemAsync("w1", "t1");
        await _repo.AddTagToItemAsync("w2", "t2");

        var map = await _repo.GetAllTagsForItemsAsync(["w1", "w2"]);
        Assert.Equal(2, map.Count);
        Assert.Single(map["w1"]);
        Assert.Single(map["w2"]);
    }

    [Fact]
    public async Task GetAllTagsForItems_EmptyInput_ReturnsEmpty()
    {
        var map = await _repo.GetAllTagsForItemsAsync([]);
        Assert.Empty(map);
    }
}
