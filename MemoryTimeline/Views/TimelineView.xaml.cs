using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MemoryTimeline.Models;
using MemoryTimeline.Services;
using MemoryTimeline.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryTimeline.Views;

public partial class TimelineView : UserControl
{
    private readonly TimelineViewModel _vm;

    public TimelineView(TimelineViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();

        AddBtn.Click += (s, e) => OpenAddMemory(null);
        SearchBox.KeyDown += async (s, e) =>
        {
            if (e.Key == Key.Enter) await _vm.SearchCommand.ExecuteAsync(null);
        };

        Loaded += async (s, e) => await _vm.LoadAsync();
        _vm.AddMemoryRequested += (s, e) => OpenAddMemory(null);
        _vm.MemoryEditRequested += (s, memory) => OpenAddMemory(memory);
    }

    private void OpenAddMemory(Memory? memory)
    {
        var sp = ((App)App.Current).ServiceProvider;
        if (sp == null) return;
        var vm = sp.GetRequiredService<AddEditMemoryViewModel>();
        if (memory != null) vm.LoadForEdit(memory);
        var win = new AddEditMemoryWindow(vm) { Owner = App.Current.MainWindow };
        if (win.ShowDialog() == true)
            _ = _vm.LoadAsync();
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        // Don't open detail if an inner interactive element was clicked
        if (e.OriginalSource is Button ||
            (e.OriginalSource is FrameworkElement fe &&
             (fe.TemplatedParent is Button || fe.DataContext is MediaItem || fe.Name == "MoreBadge")))
            return;

        var memory = ResolveMemory((sender as FrameworkElement)?.DataContext);
        if (memory != null)
        {
            var detailWin = new MemoryDetailWindow(memory) { Owner = App.Current.MainWindow };
            detailWin.ShowDialog();
            if (detailWin.MemoryModified)
            {
                _ = _vm.LoadAsync();
            }
        }
    }

    private void OnTimelineMediaThumbClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement el && el.DataContext is MediaItem item)
        {
            var ti = FindParentDataContext<TimelineItemViewModel>(el);
            var siblings = ti?.Memory?.MediaItems?.ToList()
                        ?? item.Memory?.MediaItems?.ToList()
                        ?? new List<MediaItem> { item };

            var index = siblings.IndexOf(item);
            var previewWin = new MediaPreviewWindow(item, siblings, index >= 0 ? index : 0)
            {
                Owner = App.Current.MainWindow
            };
            previewWin.ShowDialog();
            _ = _vm.LoadAsync();
        }
    }

    private void OnMoreMediaBadgeClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement el)
        {
            var ti = ResolveTimelineItem(el.DataContext) ?? FindParentDataContext<TimelineItemViewModel>(el);
            var allMedia = ti?.Memory?.MediaItems?.ToList();
            if (allMedia != null && allMedia.Count > 0)
            {
                var previewWin = new MediaPreviewWindow(allMedia[0], allMedia, 0)
                {
                    Owner = App.Current.MainWindow
                };
                previewWin.ShowDialog();
                _ = _vm.LoadAsync();
            }
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var memory = ResolveMemory((sender as FrameworkElement)?.DataContext);
        if (memory != null)
            OpenAddMemory(memory);
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var memory = ResolveMemory((sender as FrameworkElement)?.DataContext);
        if (memory != null)
        {
            var result = MessageBox.Show(
                $"Delete \"{memory.Title}\"? This cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
                await _vm.DeleteMemoryCommand.ExecuteAsync(memory);
        }
    }

    private void OnDriveLinkMouseUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var memory = ResolveMemory((sender as FrameworkElement)?.DataContext);
        if (memory != null)
        {
            OpenDriveUrl(memory.GoogleDriveUrl);
        }
    }

    private void OnOpenDriveButtonClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var memory = ResolveMemory((sender as FrameworkElement)?.DataContext);
        if (memory != null)
        {
            OpenDriveUrl(memory.GoogleDriveUrl);
        }
    }

    private static void OpenDriveUrl(string? url)
    {
        var sp = ((App)App.Current).ServiceProvider;
        var svc = sp?.GetRequiredService<IGoogleDriveService>();
        if (svc?.IsValidDriveUrl(url) == true)
        {
            svc.OpenInBrowser(url!);
        }
        else if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            MessageBox.Show("Invalid or missing Cloud / Google Drive URL.", "Cannot Open",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static Memory? ResolveMemory(object? context)
    {
        if (context is Memory m) return m;
        if (context is TimelineItemViewModel ti) return ti.Memory;
        return null;
    }

    private static TimelineItemViewModel? ResolveTimelineItem(object? context)
    {
        if (context is TimelineItemViewModel ti) return ti;
        return null;
    }

    private static T? FindParentDataContext<T>(DependencyObject? child) where T : class
    {
        while (child != null)
        {
            if (child is FrameworkElement fe && fe.DataContext is T match)
                return match;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
