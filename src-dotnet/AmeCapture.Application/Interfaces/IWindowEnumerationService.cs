using AmeCapture.Application.Models;

namespace AmeCapture.Application.Interfaces;

public interface IWindowEnumerationService
{
    Task<IReadOnlyList<WindowInfo>> EnumerateWindowsAsync();
}
