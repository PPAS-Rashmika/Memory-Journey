using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using MemoryTimeline.Models;
using MemoryTimeline.Repositories;
using MemoryTimeline.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryTimeline.Views;

public partial class MediaPreviewWindow : Window
{
    private MediaItem _item;
    private readonly IMediaService? _mediaService;
    private readonly IMediaRepository? _mediaRepo;
    private readonly List<MediaItem>? _siblings;
    private int _currentIndex;
    private DispatcherTimer? _timer;

    public MediaPreviewWindow(MediaItem item, List<MediaItem>? siblings = null, int index = 0)
    {
        _item = item;
        _siblings = siblings;
        _currentIndex = index;

        var sp = ((App)App.Current).ServiceProvider;
        _mediaService = sp?.GetService<IMediaService>();
        _mediaRepo = sp?.GetService<IMediaRepository>();

        InitializeComponent();

        PrevBtn.Click += (s, e) => Navigate(-1);
        NextBtn.Click += (s, e) => Navigate(1);
        OpenVideoBtn.Click += (s, e) => OpenExternal();
        RelinkBtn.Click += OnRelinkClick;
        BtnFavorite.Click += OnFavoriteClick;

        BtnPlay.Click += (s, e) => VideoElement.Play();
        BtnPause.Click += (s, e) => VideoElement.Pause();
        BtnStop.Click += (s, e) => { VideoElement.Stop(); TimelineSlider.Value = 0; };

        VideoElement.MediaOpened += (s, e) =>
        {
            if (VideoElement.NaturalDuration.HasTimeSpan)
            {
                TimelineSlider.Maximum = VideoElement.NaturalDuration.TimeSpan.TotalSeconds;
            }
        };

        TimelineSlider.ValueChanged += (s, e) =>
        {
            if (TimelineSlider.IsMouseCaptureWithin)
            {
                VideoElement.Position = TimeSpan.FromSeconds(TimelineSlider.Value);
            }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (s, e) =>
        {
            if (VideoElement.NaturalDuration.HasTimeSpan && !TimelineSlider.IsMouseCaptureWithin)
            {
                TimelineSlider.Value = VideoElement.Position.TotalSeconds;
            }
        };
        _timer.Start();

        Closed += (s, e) =>
        {
            _timer.Stop();
            VideoElement.Stop();
            VideoElement.Close();
        };

        PrevBtn.Visibility = (_siblings?.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
        NextBtn.Visibility = (_siblings?.Count > 1) ? Visibility.Visible : Visibility.Collapsed;

        LoadItem(_item);
    }

    private void LoadItem(MediaItem item)
    {
        _item = item;
        FileNameLabel.Text = item.FileName;
        MemoryLabel.Text = item.Memory != null ? $"From: {item.Memory.Title}" : string.Empty;

        var path = _mediaService?.GetEffectivePath(item) ?? item.OriginalFilePath;
        bool isCloud = item.IsCloud ||
                       path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        StorageModeLabel.Text = isCloud
            ? "☁️ Cloud Storage"
            : (item.StorageMode == StorageMode.Copied ? "📁 App Storage" : "🔗 Original Location");

        BtnFavorite.Content = item.IsFavorite ? "❤️ Favorited" : "🤍 Favorite";
        OpenVideoBtn.Content = isCloud ? "☁️ Open in Browser" : "↗ External Player";

        // Check if local file is missing (only for non-cloud items)
        if (!isCloud && !File.Exists(path))
        {
            PreviewImage.Visibility = Visibility.Collapsed;
            VideoPlayerGrid.Visibility = Visibility.Collapsed;
            MissingFilePanel.Visibility = Visibility.Visible;
            MissingPathLabel.Text = $"Missing local file path:\n{path}";
            Title = $"⚠️ [Missing] {item.FileName}";
            return;
        }

        MissingFilePanel.Visibility = Visibility.Collapsed;

        if (item.MediaType == MediaType.Photo)
        {
            VideoPlayerGrid.Visibility = Visibility.Collapsed;
            VideoElement.Stop();
            PreviewImage.Visibility = Visibility.Visible;

            try
            {
                var resolvedPath = isCloud
                    ? (Converters.CloudUrlHelper.ResolveImageUrl(path) ?? path)
                    : path;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(resolvedPath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                if (!isCloud) bmp.Freeze();
                PreviewImage.Source = bmp;
            }
            catch
            {
                PreviewImage.Source = null;
            }

            Title = isCloud ? $"☁️ [Cloud Photo] {item.FileName}" : $"📷 {item.FileName}";
        }
        else
        {
            PreviewImage.Visibility = Visibility.Collapsed;
            VideoPlayerGrid.Visibility = Visibility.Visible;

            try
            {
                VideoElement.Source = new Uri(path);
                VideoElement.Play();
            }
            catch { }

            Title = isCloud ? $"☁️ [Cloud Video] {item.FileName}" : $"🎬 {item.FileName}";
        }
    }

    private async void OnRelinkClick(object sender, RoutedEventArgs e)
    {
        var isVideo = _item.MediaType == MediaType.Video;
        var dlg = new OpenFileDialog
        {
            Title = $"Locate '{_item.FileName}'",
            Filter = isVideo
                ? "Video Files|*.mp4;*.mov;*.avi;*.mkv;*.wmv;*.webm|All Files|*.*"
                : "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|All Files|*.*",
            FileName = _item.FileName
        };

        if (dlg.ShowDialog() == true)
        {
            var newPath = dlg.FileName;
            if (_mediaService != null)
            {
                await _mediaService.RelinkMediaAsync(_item, newPath);
            }
            else
            {
                _item.OriginalFilePath = newPath;
                _item.FileName = Path.GetFileName(newPath);
            }

            if (_mediaRepo != null)
            {
                await _mediaRepo.UpdateAsync(_item);
            }

            MessageBox.Show($"File successfully relinked to:\n{newPath}", "Relink Successful",
                MessageBoxButton.OK, MessageBoxImage.Information);

            LoadItem(_item);
        }
    }

    private async void OnFavoriteClick(object sender, RoutedEventArgs e)
    {
        _item.IsFavorite = !_item.IsFavorite;
        BtnFavorite.Content = _item.IsFavorite ? "❤️ Favorited" : "🤍 Favorite";

        if (_mediaRepo != null)
        {
            await _mediaRepo.UpdateAsync(_item);
        }
    }

    private void Navigate(int direction)
    {
        if (_siblings == null || _siblings.Count == 0) return;
        _currentIndex = (_currentIndex + direction + _siblings.Count) % _siblings.Count;
        LoadItem(_siblings[_currentIndex]);
    }

    private void OpenExternal()
    {
        var path = _mediaService?.GetEffectivePath(_item) ?? _item.OriginalFilePath;
        bool isCloud = _item.IsCloud ||
                       path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (isCloud)
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return;
        }

        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        else
        {
            MessageBox.Show("File not found. Please use the Relink option to locate it.",
                "File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
