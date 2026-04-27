using Microsoft.Extensions.DependencyInjection;
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

        if (!string.IsNullOrWhiteSpace(args.Arguments))
        {
            var filePath = args.Arguments.Trim().Trim('"');
            if (File.Exists(filePath))
            {
                _ = window.OpenInitialFileAsync(filePath);
            }
        }
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
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }
}
