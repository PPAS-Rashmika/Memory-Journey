using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MemoryTimeline.Models;
using MemoryTimeline.ViewModels;

namespace MemoryTimeline.Views;

public partial class FavoritesView : UserControl
{
    private readonly FavoritesViewModel _vm;

    public FavoritesView(FavoritesViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (s, e) => await _vm.LoadAsync();
    }

    private void OnFavoriteMemoryClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button || (e.OriginalSource is FrameworkElement fe && fe.TemplatedParent is Button))
            return;

        if (sender is FrameworkElement el && el.DataContext is Memory memory)
        {
            var detailWin = new MemoryDetailWindow(memory) { Owner = App.Current.MainWindow };
            detailWin.ShowDialog();
            if (detailWin.MemoryModified)
            {
                _ = _vm.LoadAsync();
            }
        }
    }

    private void OnFavoriteMediaClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is MediaItem item)
        {
            var allItems = _vm.FavoriteMedia;
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
