using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;
using MemoryTimeline.Services;
using MemoryTimeline.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryTimeline.Views;

public partial class MemoryDetailWindow : Window
{
    private Memory _memory;
    private readonly IMemoryRepository? _memoryRepo;
    private readonly IMediaRepository? _mediaRepo;
    private readonly IMediaService? _mediaService;
    private readonly IGoogleDriveService? _driveService;

    public bool MemoryDeleted { get; private set; }
    public bool MemoryModified { get; private set; }

    public MemoryDetailWindow(Memory memory)
    {
        _memory = memory;

        var sp = ((App)App.Current).ServiceProvider;
        _memoryRepo = sp?.GetService<IMemoryRepository>();
        _mediaRepo = sp?.GetService<IMediaRepository>();
        _mediaService = sp?.GetService<IMediaService>();
        _driveService = sp?.GetService<IGoogleDriveService>();

        InitializeComponent();

        BtnClose.Click += (s, e) => Close();
        BtnEdit.Click += OnEditClick;
        BtnDelete.Click += OnDeleteClick;
        BtnOpenDrive.Click += OnOpenDriveClick;
        BtnViewOnMap.Click += OnViewOnMapClick;

        LoadMemoryDetails();
    }

    private void LoadMemoryDetails()
    {
        Title = $"Memory: {_memory.Title}";
        TxtTitle.Text = _memory.Title;
        TxtDate.Text = _memory.MemoryDate.ToString("dddd, MMMM d, yyyy");

        // Milestone & Favorite Badges
        TxtMilestoneBadge.Visibility = _memory.IsMilestone ? Visibility.Visible : Visibility.Collapsed;
        TxtFavoriteBadge.Visibility = _memory.IsFavorite ? Visibility.Visible : Visibility.Collapsed;

        // Location
        if (!string.IsNullOrWhiteSpace(_memory.LocationName))
        {
            LocationPanel.Visibility = Visibility.Visible;
            TxtLocation.Text = _memory.LocationName;
        }
        else
        {
            LocationPanel.Visibility = Visibility.Collapsed;
        }

        // Map Button
        BtnViewOnMap.Visibility = (_memory.Latitude.HasValue && _memory.Longitude.HasValue)
            ? Visibility.Visible : Visibility.Collapsed;

        // Description
        TxtDescription.Text = string.IsNullOrWhiteSpace(_memory.Description)
            ? "No description provided."
            : _memory.Description;

        // Tags
        TagsWrapPanel.Children.Clear();
        var tags = _memory.Tags?.Where(t => t.Tag != null).Select(t => t.Tag!).ToList() ?? new List<Tag>();
        if (tags.Count > 0)
        {
            TagsSection.Visibility = Visibility.Visible;
            foreach (var tag in tags)
            {
                var pill = new Border
                {
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 0, 8, 6),
                    CornerRadius = new CornerRadius(12),
                    Background = (Brush)FindResource("AccentBrush"),
                    Opacity = 0.85
                };
                pill.Child = new TextBlock
                {
                    Text = "# " + tag.Name,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold
                };
                TagsWrapPanel.Children.Add(pill);
            }
        }
        else
        {
            TagsSection.Visibility = Visibility.Collapsed;
        }

        // Google Drive Section
        if (!string.IsNullOrWhiteSpace(_memory.GoogleDriveUrl))
        {
            DriveAttachedPanel.Visibility = Visibility.Visible;
            TxtNoDrive.Visibility = Visibility.Collapsed;
            TxtDriveUrl.Text = _memory.GoogleDriveUrl;
        }
        else
        {
            DriveAttachedPanel.Visibility = Visibility.Collapsed;
            TxtNoDrive.Visibility = Visibility.Visible;
        }

        // Media Section
        MediaWrapPanel.Children.Clear();
        var mediaItems = _memory.MediaItems?.ToList() ?? new List<MediaItem>();
        TxtMediaHeader.Text = $"Media Attachments ({mediaItems.Count})";

        if (mediaItems.Count == 0)
        {
            TxtNoMedia.Visibility = Visibility.Visible;
        }
        else
        {
            TxtNoMedia.Visibility = Visibility.Collapsed;
            for (int i = 0; i < mediaItems.Count; i++)
            {
                var item = mediaItems[i];
                var index = i;

                var card = new Border
                {
                    Width = 110,
                    Height = 110,
                    Margin = new Thickness(0, 0, 10, 10),
                    CornerRadius = new CornerRadius(8),
                    Background = (Brush)FindResource("SurfaceBrush"),
                    BorderBrush = (Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var grid = new Grid();

                var isCloud = item.IsCloud ||
                              (!string.IsNullOrEmpty(item.OriginalFilePath) &&
                               (item.OriginalFilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                item.OriginalFilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));

                var imgPath = !string.IsNullOrEmpty(item.ThumbnailPath)
                    ? item.ThumbnailPath
                    : (_mediaService?.GetEffectivePath(item));

                var canLoadImg = item.MediaType == MediaType.Photo &&
                                 (isCloud || (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath)));

                if (canLoadImg)
                {
                    var img = new Image
                    {
                        Stretch = Stretch.UniformToFill,
                        Source = _mediaService?.LoadImage(imgPath)
                    };
                    grid.Children.Add(img);
                }
                else
                {
                    var icon = new TextBlock
                    {
                        Text = item.MediaType == MediaType.Video ? "🎬" : "🖼️",
                        FontSize = 36,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    grid.Children.Add(icon);
                }

                if (item.MediaType == MediaType.Video)
                {
                    var playBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
                        CornerRadius = new CornerRadius(16),
                        Width = 32,
                        Height = 32,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    playBorder.Child = new TextBlock
                    {
                        Text = "▶",
                        Foreground = Brushes.White,
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(2, 0, 0, 0)
                    };
                    grid.Children.Add(playBorder);
                }

                // Storage mode badge (top-right)
                var modeBadge = new Border
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Background = new SolidColorBrush(Color.FromArgb(200, 26, 26, 46)),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(4),
                    CornerRadius = new CornerRadius(4)
                };
                modeBadge.Child = new TextBlock
                {
                    Text = isCloud ? "☁️ Cloud" : (item.StorageMode == StorageMode.Copied ? "📁 App" : "🔗 Link"),
                    Foreground = Brushes.White,
                    FontSize = 9,
                    FontWeight = FontWeights.SemiBold
                };
                grid.Children.Add(modeBadge);

                // Overlay label
                var badge = new Border
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Padding = new Thickness(4, 2, 4, 2),
                    CornerRadius = new CornerRadius(0, 0, 8, 8)
                };
                badge.Child = new TextBlock
                {
                    Text = item.FileName,
                    Foreground = Brushes.White,
                    FontSize = 10,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                grid.Children.Add(badge);

                card.Child = grid;
                card.MouseLeftButtonUp += (s, e) =>
                {
                    var previewWin = new MediaPreviewWindow(item, mediaItems, index)
                    {
                        Owner = this
                    };
                    previewWin.ShowDialog();
                };

                MediaWrapPanel.Children.Add(card);
            }
        }
    }

    private void OnOpenDriveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_memory.GoogleDriveUrl)) return;

        if (_driveService != null && _driveService.IsValidDriveUrl(_memory.GoogleDriveUrl))
        {
            _driveService.OpenInBrowser(_memory.GoogleDriveUrl);
        }
        else
        {
            MessageBox.Show(
                "The link does not appear to be a valid Google Drive URL (drive.google.com or docs.google.com).",
                "Invalid Link", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnViewOnMapClick(object sender, RoutedEventArgs e)
    {
        if (_memory.Latitude.HasValue && _memory.Longitude.HasValue)
        {
            var mapWin = new LocationPickerWindow(_memory.LocationName, _memory.Latitude, _memory.Longitude)
            {
                Owner = this
            };
            mapWin.ShowDialog();
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        var sp = ((App)App.Current).ServiceProvider;
        if (sp == null) return;
        var vm = sp.GetRequiredService<AddEditMemoryViewModel>();
        vm.LoadForEdit(_memory);

        var win = new AddEditMemoryWindow(vm) { Owner = this };
        if (win.ShowDialog() == true)
        {
            MemoryModified = true;
            if (_memoryRepo != null)
            {
                var refreshed = await _memoryRepo.GetByIdAsync(_memory.Id);
                if (refreshed != null)
                {
                    _memory = refreshed;
                    LoadMemoryDetails();
                }
            }
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"Are you sure you want to permanently delete \"{_memory.Title}\"?\nThis will also remove attached application-managed media.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            if (_mediaRepo != null && _mediaService != null)
            {
                var mediaList = await _mediaRepo.GetByMemoryIdAsync(_memory.Id);
                foreach (var m in mediaList)
                {
                    await _mediaService.DeleteMediaFileAsync(m);
                }
            }

            if (_memoryRepo != null)
            {
                await _memoryRepo.DeleteAsync(_memory.Id);
            }

            MemoryDeleted = true;
            MemoryModified = true;
            Close();
        }
    }
}
