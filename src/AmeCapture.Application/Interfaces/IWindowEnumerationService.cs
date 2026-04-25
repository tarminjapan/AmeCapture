using AmeCapture.Application.Models;

namespace AmeCapture.Application.Interfaces
{
    public interface IWindowEnumerationService
    {
        public Task<IReadOnlyList<WindowInfo>> EnumerateWindowsAsync();
    }
}
