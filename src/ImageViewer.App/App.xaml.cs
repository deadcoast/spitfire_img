using Microsoft.Extensions.DependencyInjection;
using ImageViewer.App.Services;
using Microsoft.UI.Xaml;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.App;

public partial class App : Application
{
    private readonly ServiceProvider _services;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = _services.GetRequiredService<MainWindow>();
        window.Activate();

        var filePath = ParseInitialFilePath(args.Arguments, Environment.GetCommandLineArgs());
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _ = window.OpenInitialFileAsync(filePath);
        }
    }

    public static string? ParseInitialFilePath(string? activationArguments, IReadOnlyList<string> commandLineArguments)
    {
        if (!string.IsNullOrWhiteSpace(activationArguments))
        {
            return activationArguments.Trim().Trim('"');
        }

        return commandLineArguments.Count > 1
            ? commandLineArguments[1].Trim().Trim('"')
            : null;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var localDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "spitfire.img");

        services.AddSingleton<IFileOpenService, FileOpenService>();
        services.AddSingleton<IImageDecodeService, ImageDecodeService>();
        services.AddSingleton<IFolderPlaylistService, FolderPlaylistService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISettingsService>(_ => new SettingsService(localDataFolder));
        services.AddSingleton<IErrorLogService>(_ => new ErrorLogService(localDataFolder));
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IThumbnailService, WindowsThumbnailService>();
        services.AddSingleton<IRecentFilesService>(_ => new RecentFilesService(localDataFolder));
        services.AddSingleton<WindowsImageProcessingService>();
        services.AddSingleton<IExportService>(provider => provider.GetRequiredService<WindowsImageProcessingService>());
        services.AddSingleton<ITransformService>(provider => provider.GetRequiredService<WindowsImageProcessingService>());
        services.AddSingleton<IMetadataService, WindowsMetadataService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }
}
