namespace MemoryTimeline.Models;

public class MemoryTag
{
    public Guid MemoryId { get; set; }
    public Guid TagId { get; set; }

    // Navigation properties
    public Memory Memory { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
