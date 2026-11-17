using System.IO;
using System.Windows.Media.Imaging;
using MemoryTimeline.Models;

namespace MemoryTimeline.Services;

public interface IMediaService
{
    string MediaFolder { get; }
    string ThumbnailFolder { get; }
    Task<MediaItem> ImportMediaAsync(string sourcePath, Guid memoryId, StorageMode mode);
    Task<MediaItem> ImportCloudMediaAsync(string cloudUrl, string fileName, Guid memoryId, MediaType mediaType);
    Task DeleteMediaFileAsync(MediaItem item);
    Task<string?> GenerateThumbnailAsync(string filePath, MediaType mediaType);
    BitmapImage? LoadImage(string? path);
    bool FileExists(string? path);
    void OpenFile(string path);
    string GetEffectivePath(MediaItem item);
    Task<bool> RelinkMediaAsync(MediaItem item, string newPath);
    Task ClearThumbnailCacheAsync();
    long GetThumbnailCacheSizeBytes();
}
