using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(string id);
    Task<Tag?> FindByNameAsync(string name);
    Task AddAsync(Tag tag);
    Task DeleteAsync(string id);
    Task<IReadOnlyList<Tag>> GetTagsForItemAsync(string itemId);
    Task AddTagToItemAsync(string itemId, string tagId);
    Task RemoveTagFromItemAsync(string itemId, string tagId);
    Task SetTagsForItemAsync(string itemId, IEnumerable<string> tagIds);
    Task<IReadOnlyList<string>> GetItemIdsByTagAsync(string tagId);
    Task<IReadOnlyDictionary<string, IReadOnlyList<Tag>>> GetAllTagsForItemsAsync(IEnumerable<string> itemIds);
}
