using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface IFileOpenService
{
    Result<string> ValidateFilePath(string filePath);
}
