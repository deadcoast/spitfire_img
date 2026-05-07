using ImageViewer.Core.Models;

namespace ImageViewer.Core.Services;

public sealed record CommandRegistration(
    CommandItem Command,
    Func<Task<Result>> ExecuteAsync);
