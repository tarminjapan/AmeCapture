using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync();
    Task SaveAsync(AppSettings settings);
}
