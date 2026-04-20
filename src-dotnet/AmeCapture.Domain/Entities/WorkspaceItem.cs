namespace AmeCapture.Domain.Entities;

public enum WorkspaceItemType
{
    Image,
    Video
}

public class WorkspaceItem
{
    public string Id { get; set; } = string.Empty;
    public WorkspaceItemType ItemType { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public string CurrentPath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsFavorite { get; set; }
    public string? MetadataJson { get; set; }
}
