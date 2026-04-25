using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces
{
    public interface ISettingsRepository
    {
        public Task<AppSettings> GetAsync();
        public Task SaveAsync(AppSettings settings);
    }
}
