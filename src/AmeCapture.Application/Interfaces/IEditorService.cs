using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces;

public interface IEditorService
{
    Task ApplyAnnotationsAsync(string sourcePath, string outputPath, IReadOnlyList<Annotation> annotations);
}
