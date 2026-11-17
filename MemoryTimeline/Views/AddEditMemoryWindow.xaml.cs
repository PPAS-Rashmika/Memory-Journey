using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MemoryTimeline.Models;
using MemoryTimeline.ViewModels;

namespace MemoryTimeline.Views;

public partial class AddEditMemoryWindow : Window
{
    private readonly AddEditMemoryViewModel _vm;

    public AddEditMemoryWindow(AddEditMemoryViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();

        _vm.Completed += (s, success) =>
        {
            DialogResult = success;
            Close();
        };

        PickOnMapBtn.Click += PickOnMapBtn_Click;
        AddCopyBtn.Click += AddCopyBtn_Click;
        AddLinkBtn.Click += AddLinkBtn_Click;
    }

    private void PickOnMapBtn_Click(object sender, RoutedEventArgs e)
    {
        var mapPicker = new LocationPickerWindow(_vm.LocationName, _vm.Latitude, _vm.Longitude)
        {
            Owner = this
        };

        if (mapPicker.ShowDialog() == true)
        {
            if (!string.IsNullOrWhiteSpace(mapPicker.SelectedLocationName))
            {
                _vm.LocationName = mapPicker.SelectedLocationName;
            }
            _vm.Latitude = mapPicker.SelectedLatitude;
            _vm.Longitude = mapPicker.SelectedLongitude;
        }
    }

    private void AddCopyBtn_Click(object sender, RoutedEventArgs e) => AddMedia(StorageMode.Copied);
    private void AddLinkBtn_Click(object sender, RoutedEventArgs e) => AddMedia(StorageMode.Original);

    private void ChoosePreviewPhoto_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Preview Photo",
            Multiselect = false,
            Filter = "Images|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|All Files|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            _vm.SetPreviewPhoto(dlg.FileName, StorageMode.Copied);
        }
    }

    private void MakePreviewBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is PendingMediaItem item)
        {
            _vm.MakePreviewItemCommand.Execute(item);
        }
    }

    private void AddMedia(StorageMode mode)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Media Files",
            Multiselect = true,
            Filter = "Media Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp;*.mp4;*.mov;*.avi;*.mkv;*.wmv|All Files|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        foreach (var file in dlg.FileNames)
            _vm.AddMediaFileCommand.Execute((file, mode));
    }

    private void RemoveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is PendingMediaItem item)
            _vm.RemovePendingMediaCommand.Execute(item);
    }
}
