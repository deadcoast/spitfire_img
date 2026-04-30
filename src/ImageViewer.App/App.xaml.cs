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
        _ = InitializeStartupServicesAsync(_services);

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

    public static async Task InitializeStartupServicesAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var codecDetectionService = services.GetService<ICodecDetectionService>();
        if (codecDetectionService is null)
        {
            return;
        }

        var result = await codecDetectionService.DetectCodecsAsync(cancellationToken).ConfigureAwait(false);
        if (result.IsFailure && services.GetService<IErrorLogService>() is { } errorLogService)
        {
            await errorLogService.LogErrorAsync(result.Error, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var localDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "spitfire.img");

        services.AddSingleton<ICodecRegistryProbe, WindowsCodecRegistryProbe>();
        services.AddSingleton<ICodecDetectionService, WindowsCodecDetectionService>();
        services.AddSingleton<IWindowsImageMetadataReader, WindowsImageMetadataReader>();
        services.AddSingleton<IFileOpenService>(provider =>
            new FileOpenService(provider.GetRequiredService<ICodecDetectionService>()));
        services.AddSingleton<IImageDecodeService>(provider =>
            new WindowsImageDecodeService(
                provider.GetRequiredService<ICodecDetectionService>(),
                provider.GetRequiredService<IWindowsImageMetadataReader>()));
        services.AddSingleton<IFolderPlaylistService>(provider =>
            new FolderPlaylistService(provider.GetRequiredService<ICodecDetectionService>()));
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISettingsService>(_ => new SettingsService(localDataFolder));
        services.AddSingleton<IErrorLogService>(_ => new ErrorLogService(localDataFolder));
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IThumbnailService, WindowsThumbnailService>();
        services.AddSingleton<IRecentFilesService>(_ => new RecentFilesService(localDataFolder));
        services.AddSingleton<ISlideshowService, SlideshowService>();
        services.AddSingleton<IComparisonService>(provider =>
            new ComparisonService(provider.GetRequiredService<IImageDecodeService>()));
        services.AddSingleton<ICommandPaletteService, CommandPaletteService>();
        services.AddSingleton<IAnimationService, WindowsAnimationService>();
        services.AddSingleton<WindowsImageProcessingService>();
        services.AddSingleton<IExportService>(provider => provider.GetRequiredService<WindowsImageProcessingService>());
        services.AddSingleton<ITransformService>(provider => provider.GetRequiredService<WindowsImageProcessingService>());
        services.AddSingleton<IMetadataService, WindowsMetadataService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }
}
