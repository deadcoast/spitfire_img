using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public interface ICommandPaletteService
{
    Result<IReadOnlyList<CommandItem>> GetAllCommands();

    Result<IReadOnlyList<CommandPaletteMatch>> FilterCommands(string query);

    Result<IReadOnlyList<CommandItem>> GetRecentCommands();

    Task<Result> ExecuteCommandAsync(string commandId);
}
