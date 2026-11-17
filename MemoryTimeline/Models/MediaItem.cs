namespace MemoryTimeline.Models;

public class MediaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemoryId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFilePath { get; set; } = string.Empty;
    public string? StoredFilePath { get; set; }
    public MediaType MediaType { get; set; }
    public StorageMode StorageMode { get; set; }
    public string? ThumbnailPath { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Memory Memory { get; set; } = null!;

    // Helper properties
    public bool IsCloud => StorageMode == StorageMode.Cloud ||
                           (!string.IsNullOrEmpty(OriginalFilePath) &&
                            (OriginalFilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                             OriginalFilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));

    public string EffectiveDisplayPath => !string.IsNullOrEmpty(ThumbnailPath)
        ? ThumbnailPath
        : (!string.IsNullOrEmpty(StoredFilePath) ? StoredFilePath : OriginalFilePath);
}
