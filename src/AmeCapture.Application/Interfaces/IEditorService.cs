using AmeCapture.Domain.Entities;

namespace AmeCapture.Application.Interfaces
{
    public interface IEditorService
    {
        public Task ApplyAnnotationsAsync(string sourcePath, string outputPath, IReadOnlyList<Annotation> annotations);
    }
}
