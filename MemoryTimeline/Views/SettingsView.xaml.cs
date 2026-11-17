using System.Windows.Controls;
using MemoryTimeline.ViewModels;

namespace MemoryTimeline.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsView(SettingsViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
    }
}
