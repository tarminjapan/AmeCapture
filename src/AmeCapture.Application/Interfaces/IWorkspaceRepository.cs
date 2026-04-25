using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces
{
    public interface IWorkspaceRepository
    {
        public Task<IReadOnlyList<WorkspaceItem>> GetAllAsync();
        public Task<WorkspaceItem?> GetByIdAsync(string id);
        public Task AddAsync(WorkspaceItem item);
        public Task UpdateAsync(WorkspaceItem item);
        public Task DeleteAsync(string id);
        public Task<IReadOnlyList<WorkspaceItem>> GetByIdsAsync(IEnumerable<string> ids);
    }
}
