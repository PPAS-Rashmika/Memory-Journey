namespace MemoryTimeline.Models;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#4A90D9";

    // Navigation properties
    public ICollection<MemoryTag> Memories { get; set; } = new List<MemoryTag>();
}
