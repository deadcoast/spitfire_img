using Microsoft.Extensions.DependencyInjection;
using ImageViewer.App.Services;
using ImageViewer.App.Services.Motion;
using Microsoft.UI.Xaml;
using ImageViewer.Core.Models;
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
        _ = DetectCodecsOnStartupAsync(_services);

        var window = _services.GetRequiredService<MainWindow>();
        window.Activate();

        var filePath = ParseInitialFilePath(args.Arguments, Environment.GetCommandLineArgs(), Environment.CommandLine);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _ = window.OpenInitialFileAsync(filePath);
        }
    }

    public static Task<Result<CodecAvailability>> DetectCodecsOnStartupAsync(IServiceProvider services)
    {
        return services.GetRequiredService<ICodecDetectionService>().DetectCodecsAsync();
    }

    public static string? ParseInitialFilePath(
        string? activationArguments,
        IReadOnlyList<string> commandLineArguments,
        string? rawCommandLine = null)
    {
        if (!string.IsNullOrWhiteSpace(activationArguments))
        {
            return ParseActivationFilePath(activationArguments, commandLineArguments, rawCommandLine);
        }

        return GetCommandLineFilePath(commandLineArguments) ?? GetRawCommandLineFilePath(rawCommandLine);
    }

    private static string? ParseActivationFilePath(
        string activationArguments,
        IReadOnlyList<string> commandLineArguments,
        string? rawCommandLine)
    {
        var trimmed = activationArguments.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '"')
        {
            if (IsExecutableToken(trimmed.Trim('"'), commandLineArguments))
            {
                return GetCommandLineFilePath(commandLineArguments) ?? GetRawCommandLineFilePath(rawCommandLine);
            }

            return trimmed.Trim('"');
        }

        var tokens = ParseQuotedActivationTokens(trimmed);
        if (tokens.Count == 0)
        {
            return trimmed.Trim('"');
        }

        if (IsExecutableToken(tokens[0], commandLineArguments))
        {
            return tokens.Count > 1
                ? tokens[1]
                : GetCommandLineFilePath(commandLineArguments) ?? GetRawCommandLineFilePath(rawCommandLine);
        }

        return tokens[0];
    }

    private static string? GetCommandLineFilePath(IReadOnlyList<string> commandLineArguments)
    {
        return commandLineArguments.Count > 1
            ? commandLineArguments[1].Trim().Trim('"')
            : null;
    }

    private static string? GetRawCommandLineFilePath(string? rawCommandLine)
    {
        if (string.IsNullOrWhiteSpace(rawCommandLine))
        {
            return null;
        }

        var tokens = ParseQuotedActivationTokens(rawCommandLine.Trim());
        return tokens.Count > 1 ? tokens[1] : null;
    }

    private static IReadOnlyList<string> ParseQuotedActivationTokens(string activationArguments)
    {
        var tokens = new List<string>();
        var index = 0;

        while (index < activationArguments.Length)
        {
            while (index < activationArguments.Length && char.IsWhiteSpace(activationArguments[index]))
            {
                index++;
            }

            if (index >= activationArguments.Length)
            {
                break;
            }

            if (activationArguments[index] == '"')
            {
                var start = ++index;
                while (index < activationArguments.Length && activationArguments[index] != '"')
                {
                    index++;
                }

                tokens.Add(activationArguments[start..index]);
                if (index < activationArguments.Length && activationArguments[index] == '"')
                {
                    index++;
                }
            }
            else
            {
                var start = index;
                while (index < activationArguments.Length && !char.IsWhiteSpace(activationArguments[index]))
                {
                    index++;
                }

                tokens.Add(activationArguments[start..index]);
            }
        }

        return tokens;
    }

    private static bool IsExecutableToken(string token, IReadOnlyList<string> commandLineArguments)
    {
        if (commandLineArguments.Count > 0 &&
            string.Equals(token, commandLineArguments[0].Trim().Trim('"'), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(Path.GetExtension(token), ".exe", StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var localDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "spitfire.img");

        services.AddSingleton<IWindowsCodecRegistry, WindowsCodecRegistry>();
        services.AddSingleton<ICodecDetectionService, WindowsCodecDetectionService>();
        services.AddSingleton<IPlatformImageDecodeService, WindowsPlatformImageDecodeService>();
        services.AddSingleton<ImageDecodeService>();
        services.AddSingleton<IImageDecodeService>(provider =>
            EvidenceDelayImageDecodeService.CreateIfConfigured(provider.GetRequiredService<ImageDecodeService>()));
        services.AddSingleton<IFileOpenService, FileOpenService>();
        services.AddSingleton<IFolderPlaylistService, FolderPlaylistService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ILayoutService, LayoutService>();
        services.AddSingleton<ISettingsService>(_ => new SettingsService(localDataFolder));
        services.AddSingleton<IErrorLogService>(_ => new ErrorLogService(localDataFolder));
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IThumbnailService, WindowsThumbnailService>();
        services.AddSingleton<IViewerMotionPreferenceProvider, ReducedMotionService>();
        services.AddSingleton<IViewerMotionService, ViewerMotionService>();
        services.AddSingleton<IRecentFilesService>(_ => new RecentFilesService(localDataFolder));
        services.AddSingleton<WindowsImageProcessingService>();
        services.AddSingleton<IExportService>(provider => provider.GetRequiredService<WindowsImageProcessingService>());
        services.AddSingleton<ITransformService>(provider => provider.GetRequiredService<WindowsImageProcessingService>());
        services.AddSingleton<IMetadataService, WindowsMetadataService>();
        services.AddSingleton<IAnimationService, WindowsAnimationService>();
        services.AddTransient<IAnimationPlaybackTimer, DispatcherAnimationPlaybackTimer>();
        services.AddTransient<ISlideshowTimer, DispatcherSlideshowTimer>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }
}
