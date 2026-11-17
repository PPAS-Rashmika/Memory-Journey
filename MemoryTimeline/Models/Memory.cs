namespace MemoryTimeline.Models;

public class Memory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTime MemoryDate { get; set; }
    public string? Description { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GoogleDriveUrl { get; set; }
    public bool IsMilestone { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<MemoryTag> Tags { get; set; } = new List<MemoryTag>();
    public ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();
}
