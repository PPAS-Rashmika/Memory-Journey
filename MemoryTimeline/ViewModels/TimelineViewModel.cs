using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;
using MemoryTimeline.Services;
using System.Collections.ObjectModel;

namespace MemoryTimeline.ViewModels;

public partial class TimelineViewModel : ObservableObject
{
    private readonly IMemoryRepository _memoryRepo;
    private readonly IMediaRepository _mediaRepo;
    private readonly IMediaService _mediaService;
    private readonly INavigationService _navService;

    [ObservableProperty] private ObservableCollection<Memory> _memories = new();
    [ObservableProperty] private ObservableCollection<TimelineItemViewModel> _timelineItems = new();
    [ObservableProperty] private bool _isWindingView = true;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Memory? _selectedMemory;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _showMilestonesOnly;

    public event EventHandler<Memory>? MemoryEditRequested;
    public event EventHandler? AddMemoryRequested;

    public TimelineViewModel(
        IMemoryRepository memoryRepo,
        IMediaRepository mediaRepo,
        IMediaService mediaService,
        INavigationService navService)
    {
        _memoryRepo = memoryRepo;
        _mediaRepo = mediaRepo;
        _mediaService = mediaService;
        _navService = navService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var all = ShowMilestonesOnly
                ? await _memoryRepo.GetMilestonesAsync()
                : string.IsNullOrWhiteSpace(SearchQuery)
                    ? await _memoryRepo.GetAllAsync()
                    : await _memoryRepo.SearchAsync(SearchQuery);

            Memories = new ObservableCollection<Memory>(all);

            var items = new List<TimelineItemViewModel>();
            for (int i = 0; i < all.Count; i++)
            {
                var m = all[i];
                var allMedia = m.MediaItems?.OrderBy(x => x.DateAdded).ToList() ?? new List<MediaItem>();
                var preview = allMedia.Take(3).ToList();
                var remaining = Math.Max(0, allMedia.Count - 3);

                items.Add(new TimelineItemViewModel
                {
                    Memory = m,
                    Index = i,
                    IsFirst = (i == 0),
                    IsLast = (i == all.Count - 1),
                    PreviewMedia = preview,
                    RemainingMediaCount = remaining
                });
            }
            TimelineItems = new ObservableCollection<TimelineItemViewModel>(items);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private void ToggleViewMode() => IsWindingView = !IsWindingView;

    [RelayCommand]
    private void AddMemory() => AddMemoryRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void EditMemory(Memory memory) => MemoryEditRequested?.Invoke(this, memory);

    [RelayCommand]
    public async Task DeleteMemoryAsync(Memory memory)
    {
        try
        {
            var mediaList = await _mediaRepo.GetByMemoryIdAsync(memory.Id);
            foreach (var m in mediaList)
            {
                await _mediaService.DeleteMediaFileAsync(m);
            }
        }
        catch { /* best effort cleanup */ }

        await _memoryRepo.DeleteAsync(memory.Id);
        await LoadAsync();
    }

    partial void OnShowMilestonesOnlyChanged(bool value) => _ = LoadAsync();
}
