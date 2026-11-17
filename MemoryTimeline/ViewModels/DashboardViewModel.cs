using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;

namespace MemoryTimeline.ViewModels;

public class YearMemoryCount
{
    public int Year { get; set; }
    public int Count { get; set; }
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly IMemoryRepository _memoryRepo;
    private readonly IMediaRepository _mediaRepo;

    [ObservableProperty] private int _totalMemories;
    [ObservableProperty] private int _totalMilestones;
    [ObservableProperty] private int _totalPhotos;
    [ObservableProperty] private int _totalVideos;
    [ObservableProperty] private List<Memory> _recentMemories = new();
    [ObservableProperty] private List<MediaItem> _recentPhotos = new();
    [ObservableProperty] private List<YearMemoryCount> _memoriesByYear = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _greeting = string.Empty;

    public DashboardViewModel(IMemoryRepository memoryRepo, IMediaRepository mediaRepo)
    {
        _memoryRepo = memoryRepo;
        _mediaRepo = mediaRepo;
        UpdateGreeting();
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        Greeting = hour < 12 ? "Good Morning" : hour < 17 ? "Good Afternoon" : "Good Evening";
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            TotalMemories = await _memoryRepo.GetTotalCountAsync();
            TotalMilestones = await _memoryRepo.GetMilestoneCountAsync();
            TotalPhotos = await _mediaRepo.GetPhotoCountAsync();
            TotalVideos = await _mediaRepo.GetVideoCountAsync();
            RecentMemories = await _memoryRepo.GetRecentAsync(5);
            RecentPhotos = await _mediaRepo.GetRecentPhotosAsync(8);

            var all = await _memoryRepo.GetAllAsync();
            MemoriesByYear = all.GroupBy(m => m.MemoryDate.Year)
                                .Select(g => new YearMemoryCount { Year = g.Key, Count = g.Count() })
                                .OrderByDescending(y => y.Year)
                                .ToList();
        }
        finally { IsLoading = false; }
    }
}
