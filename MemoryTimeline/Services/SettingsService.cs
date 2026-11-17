using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using MemoryTimeline.Models;

namespace MemoryTimeline.Services;

public interface ISettingsService
{
    string MediaFolder { get; set; }
    string DatabasePath { get; }
    bool IsDarkTheme { get; set; }
    void Save();
    void Load();
    Task BackupAsync(string destinationPath);
    Task RestoreAsync(string backupPath);
    Task BackupFullArchiveAsync(string zipFilePath);
    Task RestoreFullArchiveAsync(string zipFilePath);
    Task ExportMemoriesJsonAsync(string destinationPath, List<Memory> memories);
    void OpenMediaFolder();
    string AppVersion { get; }
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsFile;
    private readonly string _dbPath;
    private readonly string _appData;

    public string AppVersion => "1.0.0";

    public string DatabasePath => _dbPath;

    private string _mediaFolder;
    public string MediaFolder
    {
        get => _mediaFolder;
        set { _mediaFolder = value; Save(); }
    }

    private bool _isDarkTheme = true;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set { _isDarkTheme = value; Save(); }
    }

    public SettingsService()
    {
        _appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemoryTimeline");
        Directory.CreateDirectory(_appData);
        _settingsFile = Path.Combine(_appData, "settings.ini");
        _dbPath = Path.Combine(_appData, "memorytimeline.db");
        _mediaFolder = Path.Combine(_appData, "Media");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_settingsFile)) return;
        foreach (var line in File.ReadAllLines(_settingsFile))
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;
            switch (parts[0].Trim())
            {
                case "MediaFolder": _mediaFolder = parts[1].Trim(); break;
                case "IsDarkTheme": _isDarkTheme = parts[1].Trim() == "true"; break;
            }
        }
    }

    public void Save()
    {
        File.WriteAllLines(_settingsFile, new[]
        {
            $"MediaFolder={_mediaFolder}",
            $"IsDarkTheme={(_isDarkTheme ? "true" : "false")}"
        });
    }

    public async Task BackupAsync(string destinationPath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(_dbPath))
                File.Copy(_dbPath, destinationPath, overwrite: true);
        });
    }

    public async Task RestoreAsync(string backupPath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(backupPath))
                File.Copy(backupPath, _dbPath, overwrite: true);
        });
    }

    public async Task BackupFullArchiveAsync(string zipFilePath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);

            using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
            if (File.Exists(_dbPath))
            {
                archive.CreateEntryFromFile(_dbPath, "memorytimeline.db");
            }

            if (Directory.Exists(_mediaFolder))
            {
                var files = Directory.GetFiles(_mediaFolder, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(_mediaFolder, file);
                    archive.CreateEntryFromFile(file, Path.Combine("Media", relativePath));
                }
            }
        });
    }

    public async Task RestoreFullArchiveAsync(string zipFilePath)
    {
        await Task.Run(() =>
        {
            if (!File.Exists(zipFilePath)) return;

            var tempDir = Path.Combine(_appData, "TempRestore_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                ZipFile.ExtractToDirectory(zipFilePath, tempDir);

                var tempDb = Path.Combine(tempDir, "memorytimeline.db");
                if (File.Exists(tempDb))
                {
                    File.Copy(tempDb, _dbPath, overwrite: true);
                }

                var tempMedia = Path.Combine(tempDir, "Media");
                if (Directory.Exists(tempMedia))
                {
                    Directory.CreateDirectory(_mediaFolder);
                    foreach (var file in Directory.GetFiles(tempMedia, "*.*", SearchOption.AllDirectories))
                    {
                        var rel = Path.GetRelativePath(tempMedia, file);
                        var target = Path.Combine(_mediaFolder, rel);
                        var targetDir = Path.GetDirectoryName(target);
                        if (!string.IsNullOrEmpty(targetDir)) Directory.CreateDirectory(targetDir);
                        File.Copy(file, target, overwrite: true);
                    }
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        });
    }

    public async Task ExportMemoriesJsonAsync(string destinationPath, List<Memory> memories)
    {
        var exportData = memories.Select(m => new
        {
            m.Id,
            m.Title,
            m.MemoryDate,
            m.Description,
            m.LocationName,
            m.Latitude,
            m.Longitude,
            m.GoogleDriveUrl,
            m.IsMilestone,
            m.IsFavorite,
            m.CreatedAt,
            Tags = m.Tags?.Select(t => t.Tag?.Name).Where(n => n != null).ToList(),
            Media = m.MediaItems?.Select(mi => new
            {
                mi.FileName,
                mi.MediaType,
                mi.StorageMode,
                mi.OriginalFilePath,
                mi.DateAdded
            }).ToList()
        }).ToList();

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(destinationPath, json);
    }

    public void OpenMediaFolder()
    {
        Directory.CreateDirectory(_mediaFolder);
        Process.Start(new ProcessStartInfo("explorer.exe", _mediaFolder) { UseShellExecute = true });
    }
}
