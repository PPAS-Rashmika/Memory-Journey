using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;
using MemoryTimeline.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace MemoryTimeline.ViewModels;

public partial class MilestonesViewModel : ObservableObject
{
    private readonly IMemoryRepository _memoryRepo;
    private readonly IGoogleDriveService _driveService;

    [ObservableProperty] private ObservableCollection<Memory> _milestones = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Memory? _selectedMilestone;

    public event EventHandler<Memory>? MilestoneEditRequested;

    public MilestonesViewModel(IMemoryRepository memoryRepo, IGoogleDriveService driveService)
    {
        _memoryRepo = memoryRepo;
        _driveService = driveService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var milestones = await _memoryRepo.GetMilestonesAsync();
            Milestones = new ObservableCollection<Memory>(milestones);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void OpenGoogleDrive(Memory memory)
    {
        if (!string.IsNullOrWhiteSpace(memory.GoogleDriveUrl) &&
            _driveService.IsValidDriveUrl(memory.GoogleDriveUrl))
            _driveService.OpenInBrowser(memory.GoogleDriveUrl);
    }

    [RelayCommand]
    private void EditMilestone(Memory memory) => MilestoneEditRequested?.Invoke(this, memory);
}

public partial class FavoritesViewModel : ObservableObject
{
    private readonly IMemoryRepository _memoryRepo;
    private readonly IMediaRepository _mediaRepo;

    [ObservableProperty] private List<Memory> _favoriteMemories = new();
    [ObservableProperty] private List<MediaItem> _favoriteMedia = new();
    [ObservableProperty] private bool _isLoading;

    public FavoritesViewModel(IMemoryRepository memoryRepo, IMediaRepository mediaRepo)
    {
        _memoryRepo = memoryRepo;
        _mediaRepo = mediaRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            FavoriteMemories = await _memoryRepo.GetFavoritesAsync();
            FavoriteMedia = await _mediaRepo.GetFavoritesAsync();
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task UnfavoriteMemoryAsync(Memory memory)
    {
        memory.IsFavorite = false;
        await _memoryRepo.UpdateAsync(memory);
        FavoriteMemories = await _memoryRepo.GetFavoritesAsync();
    }
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly IMediaService _mediaService;
    private readonly IMemoryRepository _memoryRepo;

    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private string _mediaFolder = string.Empty;
    [ObservableProperty] private string _databasePath = string.Empty;
    [ObservableProperty] private string _appVersion = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _cacheSizeDisplay = "0 KB";

    public SettingsViewModel(
        ISettingsService settings,
        IThemeService themeService,
        IMediaService mediaService,
        IMemoryRepository memoryRepo)
    {
        _settings = settings;
        _themeService = themeService;
        _mediaService = mediaService;
        _memoryRepo = memoryRepo;
        Load();
    }

    private void Load()
    {
        IsDarkTheme = _settings.IsDarkTheme;
        MediaFolder = _settings.MediaFolder;
        DatabasePath = _settings.DatabasePath;
        AppVersion = _settings.AppVersion;
        UpdateCacheSize();
    }

    private void UpdateCacheSize()
    {
        var bytes = _mediaService.GetThumbnailCacheSizeBytes();
        if (bytes < 1024)
            CacheSizeDisplay = $"{bytes} bytes";
        else if (bytes < 1024 * 1024)
            CacheSizeDisplay = $"{bytes / 1024.0:F1} KB";
        else
            CacheSizeDisplay = $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _settings.IsDarkTheme = IsDarkTheme;
        _themeService.ApplyTheme(IsDarkTheme ? ThemeName.Dark : ThemeName.Light);
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await _mediaService.ClearThumbnailCacheAsync();
        UpdateCacheSize();
        StatusMessage = "✅ Thumbnail cache cleared successfully.";
    }

    [RelayCommand]
    private async Task ExportMemoriesAsync()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Memory Archive",
            Filter = "JSON Archive|*.json",
            FileName = $"MemoryTimeline_Export_{DateTime.Now:yyyyMMdd}.json"
        };
        if (dlg.ShowDialog() == true)
        {
            var all = await _memoryRepo.GetAllAsync();
            await _settings.ExportMemoriesJsonAsync(dlg.FileName, all);
            StatusMessage = $"✅ Exported {all.Count} memories to {dlg.FileName}";
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Backup Database & App Media",
            Filter = "Full Backup ZIP Archive|*.zip|SQLite Database Only|*.db",
            FileName = $"MemoryTimeline_FullBackup_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
        };
        if (dlg.ShowDialog() == true)
        {
            if (dlg.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await _settings.BackupFullArchiveAsync(dlg.FileName);
                StatusMessage = $"✅ Full backup (Database + App Media) saved to {dlg.FileName}";
            }
            else
            {
                await _settings.BackupAsync(dlg.FileName);
                StatusMessage = $"✅ Database backup saved to {dlg.FileName}";
            }
        }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Restore Backup",
            Filter = "Supported Backups (*.zip;*.db)|*.zip;*.db|Full Backup ZIP|*.zip|SQLite Database|*.db"
        };
        if (dlg.ShowDialog() == true)
        {
            if (dlg.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await _settings.RestoreFullArchiveAsync(dlg.FileName);
                StatusMessage = "✅ Full restore complete (Database + Media). Please restart the application.";
            }
            else
            {
                await _settings.RestoreAsync(dlg.FileName);
                StatusMessage = "✅ Database restore complete. Please restart the application.";
            }
        }
    }

    [RelayCommand]
    private void OpenMediaFolder() => _settings.OpenMediaFolder();
}
