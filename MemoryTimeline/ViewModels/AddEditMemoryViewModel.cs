using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;
using MemoryTimeline.Services;
using System.Collections.ObjectModel;

namespace MemoryTimeline.ViewModels;

public partial class AddEditMemoryViewModel : ObservableObject
{
    private readonly IMemoryRepository _memoryRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IMediaRepository _mediaRepo;
    private readonly IMediaService _mediaService;

    [ObservableProperty] private Guid _memoryId = Guid.NewGuid();
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private DateTime _memoryDate = DateTime.Today;
    [ObservableProperty] private string _locationName = string.Empty;
    [ObservableProperty] private double? _latitude;
    [ObservableProperty] private double? _longitude;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _googleDriveUrl = string.Empty;
    [ObservableProperty] private bool _isMilestone;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private string _tagsInput = string.Empty;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _titleText = "Add Memory";
    [ObservableProperty] private string _validationMessage = string.Empty;
    [ObservableProperty] private bool _hasValidationError;

    // Cloud media inputs
    [ObservableProperty] private string _cloudUrlInput = string.Empty;
    [ObservableProperty] private string _cloudNameInput = string.Empty;
    [ObservableProperty] private string _cloudTypeInput = "Photo";
    [ObservableProperty] private bool _isCloudInputOpen;

    // Preview Photo properties
    [ObservableProperty] private string? _previewPhotoPath;
    [ObservableProperty] private string? _previewPhotoFileName;
    [ObservableProperty] private StorageMode _previewPhotoStorageMode = StorageMode.Copied;
    [ObservableProperty] private bool _hasPreviewPhoto;
    [ObservableProperty] private bool _isPreviewCloudOpen;
    [ObservableProperty] private string _previewCloudUrl = string.Empty;

    public string PreviewPhotoStorageLabel => PreviewPhotoStorageMode switch
    {
        StorageMode.Cloud => "☁️ Cloud",
        StorageMode.Copied => "📁 Local Copy",
        _ => "🔗 Local Link"
    };

    partial void OnPreviewPhotoPathChanged(string? value)
    {
        HasPreviewPhoto = !string.IsNullOrWhiteSpace(value);
        OnPropertyChanged(nameof(PreviewPhotoStorageLabel));
    }

    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    public string CoordinateDisplay => HasCoordinates 
        ? $"📍 GPS: {Latitude:F4}, {Longitude:F4}" 
        : string.Empty;

    partial void OnLatitudeChanged(double? value)
    {
        OnPropertyChanged(nameof(HasCoordinates));
        OnPropertyChanged(nameof(CoordinateDisplay));
    }

    partial void OnLongitudeChanged(double? value)
    {
        OnPropertyChanged(nameof(HasCoordinates));
        OnPropertyChanged(nameof(CoordinateDisplay));
    }

    [ObservableProperty]
    private ObservableCollection<PendingMediaItem> _pendingMedia = new();

    private readonly List<PendingMediaItem> _removedExistingMedia = new();

    public event EventHandler<bool>? Completed;

    public AddEditMemoryViewModel(
        IMemoryRepository memoryRepo,
        ITagRepository tagRepo,
        IMediaRepository mediaRepo,
        IMediaService mediaService)
    {
        _memoryRepo = memoryRepo;
        _tagRepo = tagRepo;
        _mediaRepo = mediaRepo;
        _mediaService = mediaService;
    }

    public void LoadForEdit(Memory memory)
    {
        IsEditMode = true;
        TitleText = "Edit Memory";
        MemoryId = memory.Id;
        Title = memory.Title;
        MemoryDate = memory.MemoryDate;
        LocationName = memory.LocationName ?? string.Empty;
        Latitude = memory.Latitude;
        Longitude = memory.Longitude;
        Description = memory.Description ?? string.Empty;
        GoogleDriveUrl = memory.GoogleDriveUrl ?? string.Empty;
        IsMilestone = memory.IsMilestone;
        IsFavorite = memory.IsFavorite;

        var tagNames = memory.Tags
            .Where(mt => mt.Tag != null)
            .Select(mt => mt.Tag!.Name);
        TagsInput = string.Join(", ", tagNames);

        // Reset removed items
        _removedExistingMedia.Clear();

        // Load existing media as pending items
        PendingMedia.Clear();
        foreach (var m in memory.MediaItems.OrderBy(x => x.DateAdded))
        {
            PendingMedia.Add(new PendingMediaItem
            {
                ExistingId = m.Id,
                FilePath = _mediaService.GetEffectivePath(m),
                FileName = m.FileName,
                MediaType = m.MediaType,
                StorageMode = m.StorageMode,
                IsExisting = true,
                ThumbnailPath = m.ThumbnailPath
            });
        }

        var firstPhoto = PendingMedia.FirstOrDefault(p => p.MediaType == Models.MediaType.Photo);
        if (firstPhoto != null)
        {
            firstPhoto.IsPreview = true;
            PreviewPhotoPath = firstPhoto.FilePath;
            PreviewPhotoFileName = firstPhoto.FileName;
            PreviewPhotoStorageMode = firstPhoto.StorageMode;
            HasPreviewPhoto = true;
        }
    }

    public void SetPreviewPhoto(string path, StorageMode mode)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        PreviewPhotoPath = path;
        PreviewPhotoFileName = System.IO.Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(PreviewPhotoFileName))
            PreviewPhotoFileName = path.Contains("drive.google.com") ? "Google Drive Photo" : "Cloud Photo";
        PreviewPhotoStorageMode = mode;
        HasPreviewPhoto = true;

        // Ensure in PendingMedia at index 0 and marked IsPreview
        var existing = PendingMedia.FirstOrDefault(p => p.FilePath == path);
        if (existing != null)
        {
            PendingMedia.Remove(existing);
            existing.IsPreview = true;
            PendingMedia.Insert(0, existing);
        }
        else
        {
            PendingMedia.Insert(0, new PendingMediaItem
            {
                FilePath = path,
                FileName = PreviewPhotoFileName,
                MediaType = Models.MediaType.Photo,
                StorageMode = mode,
                IsExisting = false,
                IsPreview = true,
                ThumbnailPath = mode == StorageMode.Cloud ? Converters.CloudUrlHelper.ResolveImageUrl(path) : null
            });
        }

        foreach (var p in PendingMedia.Skip(1))
            p.IsPreview = false;
    }

    [RelayCommand]
    private void RemovePreviewPhoto()
    {
        if (!string.IsNullOrEmpty(PreviewPhotoPath))
        {
            var match = PendingMedia.FirstOrDefault(p => p.FilePath == PreviewPhotoPath);
            if (match != null) match.IsPreview = false;
        }
        PreviewPhotoPath = null;
        PreviewPhotoFileName = null;
        HasPreviewPhoto = false;

        var nextPhoto = PendingMedia.FirstOrDefault(p => p.MediaType == Models.MediaType.Photo);
        if (nextPhoto != null)
        {
            SetPreviewPhoto(nextPhoto.FilePath, nextPhoto.StorageMode);
        }
    }

    [RelayCommand]
    private void TogglePreviewCloud() => IsPreviewCloudOpen = !IsPreviewCloudOpen;

    [RelayCommand]
    private void SetCloudPreviewPhoto()
    {
        if (string.IsNullOrWhiteSpace(PreviewCloudUrl)) return;
        var url = PreviewCloudUrl.Trim();
        SetPreviewPhoto(url, StorageMode.Cloud);
        PreviewCloudUrl = string.Empty;
        IsPreviewCloudOpen = false;
    }

    [RelayCommand]
    private void MakePreviewItem(PendingMediaItem item)
    {
        if (item != null && item.MediaType == Models.MediaType.Photo)
        {
            SetPreviewPhoto(item.FilePath, item.StorageMode);
        }
    }

    [RelayCommand]
    private void AddMediaFile((string path, StorageMode mode) args)
    {
        var mediaType = GetMediaType(args.path);
        bool shouldBePreview = !HasPreviewPhoto && mediaType == Models.MediaType.Photo;

        var item = new PendingMediaItem
        {
            FilePath = args.path,
            FileName = System.IO.Path.GetFileName(args.path),
            MediaType = mediaType,
            StorageMode = args.mode,
            IsExisting = false,
            IsPreview = shouldBePreview,
            ThumbnailPath = null
        };

        if (shouldBePreview)
        {
            PendingMedia.Insert(0, item);
            PreviewPhotoPath = args.path;
            PreviewPhotoFileName = item.FileName;
            PreviewPhotoStorageMode = args.mode;
            HasPreviewPhoto = true;
        }
        else
        {
            PendingMedia.Add(item);
        }
    }

    [RelayCommand]
    private void ToggleCloudInput() => IsCloudInputOpen = !IsCloudInputOpen;

    [RelayCommand]
    private void AddCloudMediaItem()
    {
        if (string.IsNullOrWhiteSpace(CloudUrlInput)) return;

        var url = CloudUrlInput.Trim();
        var mediaType = CloudTypeInput == "Video" ? Models.MediaType.Video : Models.MediaType.Photo;
        var name = string.IsNullOrWhiteSpace(CloudNameInput)
            ? (url.Contains("drive.google.com") ? "Google Drive Cloud Media" : "Cloud Media")
            : CloudNameInput.Trim();

        bool shouldBePreview = !HasPreviewPhoto && mediaType == Models.MediaType.Photo;

        var item = new PendingMediaItem
        {
            FilePath = url,
            FileName = name,
            MediaType = mediaType,
            StorageMode = StorageMode.Cloud,
            IsExisting = false,
            IsPreview = shouldBePreview,
            ThumbnailPath = Converters.CloudUrlHelper.ResolveImageUrl(url)
        };

        if (shouldBePreview)
        {
            PendingMedia.Insert(0, item);
            PreviewPhotoPath = url;
            PreviewPhotoFileName = name;
            PreviewPhotoStorageMode = StorageMode.Cloud;
            HasPreviewPhoto = true;
        }
        else
        {
            PendingMedia.Add(item);
        }

        CloudUrlInput = string.Empty;
        CloudNameInput = string.Empty;
        IsCloudInputOpen = false;
    }

    [RelayCommand]
    private void RemovePendingMedia(PendingMediaItem item)
    {
        PendingMedia.Remove(item);
        if (item.IsExisting && item.ExistingId.HasValue)
        {
            _removedExistingMedia.Add(item);
        }
        if (item.FilePath == PreviewPhotoPath)
        {
            RemovePreviewPhoto();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        var memory = IsEditMode
            ? await _memoryRepo.GetByIdAsync(MemoryId) ?? new Memory { Id = MemoryId }
            : new Memory { Id = MemoryId };

        memory.Title = Title.Trim();
        memory.MemoryDate = MemoryDate;
        memory.LocationName = LocationName.Trim();
        memory.Latitude = Latitude;
        memory.Longitude = Longitude;
        memory.Description = Description.Trim();
        memory.GoogleDriveUrl = GoogleDriveUrl.Trim();
        memory.IsMilestone = IsMilestone;
        memory.IsFavorite = IsFavorite;
        memory.UpdatedAt = DateTime.UtcNow;

        if (!IsEditMode)
        {
            memory.CreatedAt = DateTime.UtcNow;
            await _memoryRepo.AddAsync(memory);
        }
        else
        {
            await _memoryRepo.UpdateAsync(memory);
        }

        // Handle tags
        await _tagRepo.RemoveAllTagsFromMemoryAsync(memory.Id);
        if (!string.IsNullOrWhiteSpace(TagsInput))
        {
            var tagNames = TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(t => t.Trim())
                                    .Where(t => !string.IsNullOrEmpty(t))
                                    .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var name in tagNames)
            {
                var tag = await _tagRepo.GetByNameAsync(name)
                          ?? new Tag { Id = Guid.NewGuid(), Name = name, Color = "#E94560" };
                if (tag.Id == Guid.Empty || await _tagRepo.GetByNameAsync(name) == null)
                    await _tagRepo.AddAsync(tag);
                await _tagRepo.AddTagToMemoryAsync(memory.Id, tag.Id);
            }
        }

        // Delete existing media that were removed during edit
        foreach (var removed in _removedExistingMedia)
        {
            if (removed.ExistingId.HasValue)
            {
                var existingItem = await _mediaRepo.GetByIdAsync(removed.ExistingId.Value);
                if (existingItem != null)
                {
                    try { await _mediaService.DeleteMediaFileAsync(existingItem); } catch { }
                    await _mediaRepo.DeleteAsync(existingItem.Id);
                }
            }
        }
        _removedExistingMedia.Clear();

        // If preview photo is an existing media item, ensure it has earliest DateAdded to remain first
        if (!string.IsNullOrEmpty(PreviewPhotoPath))
        {
            var previewPending = PendingMedia.FirstOrDefault(p => p.IsPreview);
            if (previewPending != null && previewPending.IsExisting && previewPending.ExistingId.HasValue)
            {
                var existingItem = await _mediaRepo.GetByIdAsync(previewPending.ExistingId.Value);
                if (existingItem != null)
                {
                    existingItem.DateAdded = DateTime.UtcNow.AddYears(-10);
                    await _mediaRepo.UpdateAsync(existingItem);
                }
            }
        }

        // Import new media (both local files and cloud URLs), saving preview photo first
        var orderedPending = PendingMedia.OrderByDescending(p => p.IsPreview).ToList();
        int offset = 0;
        foreach (var pending in orderedPending.Where(p => !p.IsExisting))
        {
            try
            {
                MediaItem item;
                if (pending.StorageMode == StorageMode.Cloud ||
                    pending.FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    pending.FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    item = await _mediaService.ImportCloudMediaAsync(pending.FilePath, pending.FileName, memory.Id, pending.MediaType);
                }
                else
                {
                    item = await _mediaService.ImportMediaAsync(pending.FilePath, memory.Id, pending.StorageMode);
                }

                if (pending.IsPreview)
                {
                    item.DateAdded = DateTime.UtcNow.AddYears(-10);
                }
                else
                {
                    item.DateAdded = DateTime.UtcNow.AddSeconds(offset++);
                }

                await _mediaRepo.AddAsync(item);
            }
            catch { /* log and continue */ }
        }

        Completed?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => Completed?.Invoke(this, false);

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsEditMode) return;
        var media = await _mediaRepo.GetByMemoryIdAsync(MemoryId);
        foreach (var m in media)
            await _mediaService.DeleteMediaFileAsync(m);
        await _memoryRepo.DeleteAsync(MemoryId);
        Completed?.Invoke(this, true);
    }

    private bool Validate()
    {
        HasValidationError = false;
        ValidationMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Title))
        {
            ValidationMessage = "Memory title is required.";
            HasValidationError = true;
            return false;
        }
        if (MemoryDate == default)
        {
            ValidationMessage = "Please select a valid date.";
            HasValidationError = true;
            return false;
        }
        return true;
    }

    private static Models.MediaType GetMediaType(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (ext.Contains('?')) ext = ext.Split('?')[0];

        return ext switch
        {
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".wmv" or ".m4v" or ".webm" => Models.MediaType.Video,
            _ => Models.MediaType.Photo
        };
    }
}

public partial class PendingMediaItem : ObservableObject
{
    public Guid? ExistingId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Models.MediaType MediaType { get; set; }
    public StorageMode StorageMode { get; set; }
    public bool IsExisting { get; set; }
    public string? ThumbnailPath { get; set; }
    [ObservableProperty] private bool _isPreview;

    public string TypeLabel => MediaType == Models.MediaType.Video ? "🎬" : "🖼️";
    public string ModeLabel => StorageMode switch
    {
        StorageMode.Cloud => "☁️ Cloud",
        StorageMode.Copied => "📁 Copy",
        _ => "🔗 Link"
    };
    public bool IsCloud => StorageMode == StorageMode.Cloud;
    public bool IsPhoto => MediaType == Models.MediaType.Photo;

    public string EffectivePreviewPath => !string.IsNullOrEmpty(ThumbnailPath) ? ThumbnailPath : FilePath;
}
