using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MemoryTimeline.Models;
using MemoryTimeline.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryTimeline.Views;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _vm;

    public DashboardView(DashboardViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (s, e) => await _vm.LoadAsync();
        NewMemoryBtn.Click += (s, e) => OpenAddMemory();
    }

    private void OpenAddMemory()
    {
        var sp = ((App)App.Current).ServiceProvider;
        if (sp == null) return;
        var vm = sp.GetRequiredService<AddEditMemoryViewModel>();
        var win = new AddEditMemoryWindow(vm) { Owner = App.Current.MainWindow };
        if (win.ShowDialog() == true)
            _ = _vm.LoadAsync();
    }

    private void OnRecentMemoryClick(object sender, MouseButtonEventArgs e)
    {
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

    private void OnRecentPhotoClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is MediaItem photo)
        {
            var allPhotos = _vm.RecentPhotos.ToList();
            var index = allPhotos.IndexOf(photo);
            var previewWin = new MediaPreviewWindow(photo, allPhotos, index >= 0 ? index : 0)
            {
                Owner = App.Current.MainWindow
            };
            previewWin.ShowDialog();
        }
    }
}
