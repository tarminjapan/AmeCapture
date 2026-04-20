using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync();
    Task<Tag> CreateAsync(Tag tag);
    Task DeleteAsync(string id);
    Task<IReadOnlyList<Tag>> GetTagsForItemAsync(string itemId);
    Task SetTagsForItemAsync(string itemId, IEnumerable<string> tagIds);
}
