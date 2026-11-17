using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MemoryTimeline.Data;
using MemoryTimeline.Repositories;
using MemoryTimeline.Services;
using MemoryTimeline.ViewModels;
using MemoryTimeline.Views;

namespace MemoryTimeline;

public partial class App : Application
{
    private IHost? _host;
    public IServiceProvider? ServiceProvider => _host?.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
            .Build();

        await _host.StartAsync();

        var dbInit = _host.Services.GetRequiredService<DatabaseInitializer>();
        await dbInit.InitializeAsync();

        var settings = _host.Services.GetRequiredService<ISettingsService>();
        var themeService = _host.Services.GetRequiredService<IThemeService>();
        themeService.ApplyTheme(settings.IsDarkTheme ? ThemeName.Dark : ThemeName.Light);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemoryTimeline");
        Directory.CreateDirectory(appData);
        var dbPath = Path.Combine(appData, "memorytimeline.db");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Transient);

        services.AddTransient<DatabaseInitializer>();

        // Repositories
        services.AddTransient<IMemoryRepository, MemoryRepository>();
        services.AddTransient<IMediaRepository, MediaRepository>();
        services.AddTransient<ITagRepository, TagRepository>();

        // Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IMediaService, MediaService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<TimelineViewModel>();
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<GalleryViewModel>();
        services.AddTransient<MilestonesViewModel>();
        services.AddTransient<FavoritesViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AddEditMemoryViewModel>();

        // Views
        services.AddSingleton<DashboardView>();
        services.AddSingleton<TimelineView>();
        services.AddSingleton<CalendarView>();
        services.AddSingleton<GalleryView>();
        services.AddSingleton<MilestonesView>();
        services.AddSingleton<FavoritesView>();
        services.AddSingleton<SettingsView>();
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
