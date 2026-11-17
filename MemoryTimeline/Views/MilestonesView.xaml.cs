using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MemoryTimeline.Models;
using MemoryTimeline.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryTimeline.Views;

public partial class MilestonesView : UserControl
{
    private readonly MilestonesViewModel _vm;

    public MilestonesView(MilestonesViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (s, e) => await _vm.LoadAsync();
        _vm.MilestoneEditRequested += (s, memory) => OpenEditMemory(memory);
    }

    private void OpenEditMemory(Memory memory)
    {
        var sp = ((App)App.Current).ServiceProvider;
        if (sp == null) return;
        var vm = sp.GetRequiredService<AddEditMemoryViewModel>();
        vm.LoadForEdit(memory);
        var win = new AddEditMemoryWindow(vm) { Owner = App.Current.MainWindow };
        if (win.ShowDialog() == true)
            _ = _vm.LoadAsync();
    }

    private void OnMilestoneCardClick(object sender, MouseButtonEventArgs e)
    {
        // Don't trigger if clicked on an action button
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
}
