using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MemoryTimeline.Models;
using MemoryTimeline.ViewModels;

namespace MemoryTimeline.Views;

public partial class GalleryView : UserControl
{
    private readonly GalleryViewModel _vm;

    public GalleryView(GalleryViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (s, e) => await _vm.LoadAsync();
        SearchBox.KeyDown += async (s, e) =>
        {
            if (e.Key == Key.Enter) await _vm.LoadAsync();
        };
    }

    private void OnMediaCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement border && border.DataContext is MediaItem item)
        {
            var allItems = _vm.MediaItems.ToList();
            var index = allItems.IndexOf(item);
            var previewWin = new MediaPreviewWindow(item, allItems, index >= 0 ? index : 0)
            {
                Owner = App.Current.MainWindow
            };
            previewWin.ShowDialog();
            _ = _vm.LoadAsync();
        }
    }
}
