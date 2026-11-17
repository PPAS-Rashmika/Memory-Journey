using CommunityToolkit.Mvvm.ComponentModel;
using MemoryTimeline.Models;
using System.Collections.Generic;

namespace MemoryTimeline.ViewModels;

public class TimelineItemViewModel : ObservableObject
{
    public Memory Memory { get; set; } = null!;
    public int Index { get; set; }
    public bool IsLeft => Index % 2 == 0;
    public bool IsRight => !IsLeft;
    public bool IsFirst { get; set; }
    public bool IsLast { get; set; }

    /// <summary>
    /// Up to 3 preview thumbnails to show across the top row of the card.
    /// </summary>
    public List<MediaItem> PreviewMedia { get; set; } = new();

    /// <summary>
    /// Remaining count of media items beyond the preview limit (e.g. 5+).
    /// </summary>
    public int RemainingMediaCount { get; set; }
    public bool HasRemainingMedia => RemainingMediaCount > 0;
    public string RemainingMediaBadge => $"{RemainingMediaCount}+";

    public bool HasMedia => Memory?.MediaItems != null && Memory.MediaItems.Count > 0;
    public int TotalMediaCount => Memory?.MediaItems?.Count ?? 0;
}
