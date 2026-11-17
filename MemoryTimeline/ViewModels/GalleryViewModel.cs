using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;
using System.Collections.ObjectModel;

namespace MemoryTimeline.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    private readonly IMediaRepository _mediaRepo;
    private readonly IMemoryRepository _memoryRepo;

    [ObservableProperty] private ObservableCollection<MediaItem> _mediaItems = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private MediaItem? _selectedItem;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _selectedTypeFilter = "All";
    [ObservableProperty] private bool _favoritesOnly;
    [ObservableProperty] private string _selectedYear = "All";
    [ObservableProperty] private string _selectedLocation = string.Empty;
    [ObservableProperty] private List<string> _availableYears = new() { "All" };
    [ObservableProperty] private List<string> _typeFilters = new() { "All", "Photos", "Videos" };

    public GalleryViewModel(IMediaRepository mediaRepo, IMemoryRepository memoryRepo)
    {
        _mediaRepo = mediaRepo;
        _memoryRepo = memoryRepo;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            MediaType? typeFilter = SelectedTypeFilter switch
            {
                "Photos" => Models.MediaType.Photo,
                "Videos" => Models.MediaType.Video,
                _ => null
            };

            var items = await _mediaRepo.SearchAsync(SearchQuery, typeFilter, FavoritesOnly ? true : null);

            if (int.TryParse(SelectedYear, out var filterYear) && filterYear > 0)
                items = items.Where(i => i.Memory?.MemoryDate.Year == filterYear).ToList();

            if (!string.IsNullOrWhiteSpace(SelectedLocation))
                items = items.Where(i => i.Memory?.LocationName?.Contains(SelectedLocation, StringComparison.OrdinalIgnoreCase) == true).ToList();

            MediaItems = new ObservableCollection<MediaItem>(items);

            // Populate year filter
            var allMedia = await _mediaRepo.GetAllAsync();
            var distinctYears = allMedia
                .Where(m => m.Memory != null)
                .Select(m => m.Memory!.MemoryDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .Select(y => y.ToString())
                .ToList();

            var yearList = new List<string> { "All" };
            yearList.AddRange(distinctYears);
            AvailableYears = yearList;

            if (string.IsNullOrEmpty(SelectedYear))
                SelectedYear = "All";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(MediaItem item)
    {
        item.IsFavorite = !item.IsFavorite;
        await _mediaRepo.UpdateAsync(item);
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchQuery = string.Empty;
        SelectedTypeFilter = "All";
        FavoritesOnly = false;
        SelectedYear = "All";
        SelectedLocation = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync() => await LoadAsync();
}
