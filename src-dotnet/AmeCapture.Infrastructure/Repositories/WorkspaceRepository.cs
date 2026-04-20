using AmeCapture.Domain.Entities;

namespace AmeCapture.Infrastructure.Repositories;

public class WorkspaceRepository : AmeCapture.Application.Interfaces.IWorkspaceRepository
{
    public Task<IReadOnlyList<WorkspaceItem>> GetAllAsync()
    {
        IReadOnlyList<WorkspaceItem> result = Array.Empty<WorkspaceItem>();
        return Task.FromResult(result);
    }

    public Task<WorkspaceItem?> GetByIdAsync(string id)
    {
        return Task.FromResult<WorkspaceItem?>(null);
    }

    public Task AddAsync(WorkspaceItem item)
    {
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkspaceItem item)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        return Task.CompletedTask;
    }
}
