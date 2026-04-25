using AmeCapture.Domain.Entities;

namespace AmeCapture.Tests.Domain;

public class WorkspaceItemTests
{
    [Fact]
    public void WorkspaceItem_DefaultValues_AreSet()
    {
        var item = new WorkspaceItem();

        Assert.Equal(string.Empty, item.Id);
        Assert.Equal(WorkspaceItemType.Image, item.ItemType);
        Assert.Equal(string.Empty, item.OriginalPath);
        Assert.Equal(string.Empty, item.CurrentPath);
        Assert.Null(item.ThumbnailPath);
        Assert.Equal(string.Empty, item.Title);
        Assert.Equal(string.Empty, item.CreatedAt);
        Assert.Equal(string.Empty, item.UpdatedAt);
        Assert.False(item.IsFavorite);
        Assert.Null(item.MetadataJson);
    }

    [Fact]
    public void Tag_DefaultValues_AreSet()
    {
        var tag = new Tag();

        Assert.Equal(string.Empty, tag.Id);
        Assert.Equal(string.Empty, tag.Name);
    }

    [Fact]
    public void AppSettings_DefaultValues_AreSet()
    {
        var settings = new AppSettings();

        Assert.Equal(string.Empty, settings.SavePath);
        Assert.Equal("png", settings.ImageFormat);
        Assert.False(settings.StartMinimized);
        Assert.Equal("Ctrl+Shift+S", settings.HotkeyCaptureRegion);
        Assert.Equal("Ctrl+Shift+F", settings.HotkeyCaptureFullscreen);
        Assert.Equal("Ctrl+Shift+W", settings.HotkeyCaptureWindow);
    }
}
