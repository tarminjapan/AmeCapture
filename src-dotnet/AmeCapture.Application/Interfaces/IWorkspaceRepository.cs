using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<WorkspaceItem>> GetAllAsync();
    Task<WorkspaceItem?> GetByIdAsync(string id);
    Task AddAsync(WorkspaceItem item);
    Task UpdateAsync(WorkspaceItem item);
    Task DeleteAsync(string id);
    Task<IReadOnlyList<WorkspaceItem>> GetByIdsAsync(IEnumerable<string> ids);
}
