namespace ImageViewer.Core.Models;

public sealed record RecentFile(string FilePath, DateTimeOffset LastOpened)
{
    public string Filename => Path.GetFileName(FilePath);

    public string FolderPath => Path.GetDirectoryName(FilePath) ?? string.Empty;
}
