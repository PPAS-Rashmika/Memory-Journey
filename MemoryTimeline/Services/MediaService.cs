using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using MemoryTimeline.Models;

namespace MemoryTimeline.Services;

public class MediaService : IMediaService
{
    private readonly string _appDataRoot;

    public string MediaFolder { get; }
    public string ThumbnailFolder { get; }

    public MediaService()
    {
        _appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemoryTimeline");
        MediaFolder = Path.Combine(_appDataRoot, "Media");
        ThumbnailFolder = Path.Combine(_appDataRoot, "Thumbnails");
        Directory.CreateDirectory(MediaFolder);
        Directory.CreateDirectory(ThumbnailFolder);
    }

    public string GetEffectivePath(MediaItem item)
    {
        if (item.StorageMode == StorageMode.Cloud ||
            item.OriginalFilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            item.OriginalFilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return item.OriginalFilePath;
        }

        return item.StorageMode == StorageMode.Copied
            ? (item.StoredFilePath ?? item.OriginalFilePath)
            : item.OriginalFilePath;
    }

    public bool FileExists(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return true;
        return File.Exists(path);
    }

    public async Task<MediaItem> ImportMediaAsync(string sourcePath, Guid memoryId, StorageMode mode)
    {
        // If it's a web/cloud URL
        if (sourcePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            sourcePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var detectedType = GetMediaType(sourcePath);
            return await ImportCloudMediaAsync(sourcePath, GetCloudFileName(sourcePath), memoryId, detectedType);
        }

        var fileName = Path.GetFileName(sourcePath);
        var mediaType = GetMediaType(sourcePath);
        string? storedPath = null;

        if (mode == StorageMode.Copied)
        {
            var destFolder = Path.Combine(MediaFolder, memoryId.ToString());
            Directory.CreateDirectory(destFolder);
            var destPath = Path.Combine(destFolder, fileName);
            if (File.Exists(destPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                destPath = Path.Combine(destFolder, $"{nameWithoutExt}_{Guid.NewGuid():N}{ext}");
            }
            await Task.Run(() => File.Copy(sourcePath, destPath, overwrite: false));
            storedPath = destPath;
        }

        var thumbnailPath = await GenerateThumbnailAsync(storedPath ?? sourcePath, mediaType);

        return new MediaItem
        {
            Id = Guid.NewGuid(),
            MemoryId = memoryId,
            FileName = fileName,
            OriginalFilePath = sourcePath,
            StoredFilePath = storedPath,
            MediaType = mediaType,
            StorageMode = mode,
            ThumbnailPath = thumbnailPath,
            DateAdded = DateTime.UtcNow
        };
    }

    public async Task<MediaItem> ImportCloudMediaAsync(string cloudUrl, string fileName, Guid memoryId, MediaType mediaType)
    {
        await Task.CompletedTask;

        var cleanFileName = string.IsNullOrWhiteSpace(fileName) ? GetCloudFileName(cloudUrl) : fileName.Trim();
        var thumbnail = Converters.CloudUrlHelper.ResolveImageUrl(cloudUrl);

        return new MediaItem
        {
            Id = Guid.NewGuid(),
            MemoryId = memoryId,
            FileName = cleanFileName,
            OriginalFilePath = cloudUrl.Trim(),
            StoredFilePath = null,
            MediaType = mediaType,
            StorageMode = StorageMode.Cloud,
            ThumbnailPath = thumbnail,
            DateAdded = DateTime.UtcNow
        };
    }

    public async Task DeleteMediaFileAsync(MediaItem item)
    {
        if (item.StorageMode == StorageMode.Copied && !string.IsNullOrEmpty(item.StoredFilePath))
        {
            await Task.Run(() =>
            {
                if (File.Exists(item.StoredFilePath))
                    File.Delete(item.StoredFilePath);
            });
        }
        if (!string.IsNullOrEmpty(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
            await Task.Run(() => File.Delete(item.ThumbnailPath));
    }

    public async Task<string?> GenerateThumbnailAsync(string filePath, MediaType mediaType)
    {
        if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Converters.CloudUrlHelper.ResolveImageUrl(filePath);
        }

        if (!File.Exists(filePath)) return null;
        try
        {
            if (mediaType == MediaType.Photo)
                return await GeneratePhotoThumbnailAsync(filePath);
            else
                return await GenerateVideoThumbnailAsync(filePath);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GeneratePhotoThumbnailAsync(string sourcePath)
    {
        var thumbName = $"{Path.GetFileNameWithoutExtension(sourcePath)}_{Guid.NewGuid():N}_thumb.jpg";
        var thumbPath = Path.Combine(ThumbnailFolder, thumbName);

        await Task.Run(() =>
        {
            var src = new BitmapImage();
            src.BeginInit();
            src.UriSource = new Uri(sourcePath);
            src.DecodePixelWidth = 300;
            src.CacheOption = BitmapCacheOption.OnLoad;
            src.EndInit();
            src.Freeze();

            var encoder = new JpegBitmapEncoder { QualityLevel = 80 };
            encoder.Frames.Add(BitmapFrame.Create(src));
            using var stream = File.OpenWrite(thumbPath);
            encoder.Save(stream);
        });

        return thumbPath;
    }

    private async Task<string?> GenerateVideoThumbnailAsync(string sourcePath)
    {
        await Task.CompletedTask;
        return null;
    }

    public BitmapImage? LoadImage(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var resolved = Converters.CloudUrlHelper.ResolveImageUrl(path) ?? path;

        try
        {
            if (resolved.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                resolved.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(resolved);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.DecodePixelWidth = 500;
                img.EndInit();
                return img;
            }

            if (!File.Exists(resolved)) return null;

            var local = new BitmapImage();
            local.BeginInit();
            local.UriSource = new Uri(resolved);
            local.CacheOption = BitmapCacheOption.OnLoad;
            local.EndInit();
            local.Freeze();
            return local;
        }
        catch { return null; }
    }

    public void OpenFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return;
        }

        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    public async Task<bool> RelinkMediaAsync(MediaItem item, string newPath)
    {
        if (newPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            newPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            item.OriginalFilePath = newPath;
            item.FileName = GetCloudFileName(newPath);
            item.StorageMode = StorageMode.Cloud;
            item.ThumbnailPath = Converters.CloudUrlHelper.ResolveImageUrl(newPath);
            return true;
        }

        if (!File.Exists(newPath)) return false;

        item.OriginalFilePath = newPath;
        item.FileName = Path.GetFileName(newPath);
        item.StorageMode = StorageMode.Original;

        var thumb = await GenerateThumbnailAsync(newPath, item.MediaType);
        if (!string.IsNullOrEmpty(thumb))
        {
            item.ThumbnailPath = thumb;
        }
        return true;
    }

    public async Task ClearThumbnailCacheAsync()
    {
        await Task.Run(() =>
        {
            if (Directory.Exists(ThumbnailFolder))
            {
                var files = Directory.GetFiles(ThumbnailFolder);
                foreach (var file in files)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        });
    }

    public long GetThumbnailCacheSizeBytes()
    {
        if (!Directory.Exists(ThumbnailFolder)) return 0;
        var info = new DirectoryInfo(ThumbnailFolder);
        return info.EnumerateFiles().Sum(f => f.Length);
    }

    private static MediaType GetMediaType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext.Contains('?')) ext = ext.Split('?')[0];

        return ext switch
        {
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".wmv" or ".m4v" or ".webm" => MediaType.Video,
            _ => MediaType.Photo
        };
    }

    private static string GetCloudFileName(string url)
    {
        try
        {
            var uri = new Uri(url);
            var lastSegment = uri.Segments.LastOrDefault()?.Trim('/');
            if (!string.IsNullOrWhiteSpace(lastSegment) && lastSegment.Length < 60)
            {
                return Uri.UnescapeDataString(lastSegment);
            }
            if (uri.Host.Contains("drive.google.com")) return "Google Drive Cloud Media";
            if (uri.Host.Contains("photos.google.com")) return "Google Photos Cloud Media";
            return $"{uri.Host} Media";
        }
        catch
        {
            return "Cloud Media";
        }
    }
}
