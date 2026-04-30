using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ICommandPaletteService
{
    IReadOnlyList<CommandItem> GetRegisteredCommands();

    IReadOnlyList<CommandSearchResult> SearchCommands(string query);
}
