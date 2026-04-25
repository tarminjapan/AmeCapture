using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces
{
    public interface ITagRepository
    {
        public Task<IReadOnlyList<Tag>> GetAllAsync();
        public Task<Tag?> GetByIdAsync(string id);
        public Task<Tag?> FindByNameAsync(string name);
        public Task AddAsync(Tag tag);
        public Task DeleteAsync(string id);
        public Task<IReadOnlyList<Tag>> GetTagsForItemAsync(string itemId);
        public Task AddTagToItemAsync(string itemId, string tagId);
        public Task RemoveTagFromItemAsync(string itemId, string tagId);
        public Task SetTagsForItemAsync(string itemId, IEnumerable<string> tagIds);
        public Task<IReadOnlyList<string>> GetItemIdsByTagAsync(string tagId);
        public Task<IReadOnlyDictionary<string, IReadOnlyList<Tag>>> GetAllTagsForItemsAsync(IEnumerable<string> itemIds);
    }
}
