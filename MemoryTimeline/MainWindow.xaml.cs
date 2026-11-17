using System.Windows;
using MemoryTimeline.Services;
using MemoryTimeline.ViewModels;
using MemoryTimeline.Views;

namespace MemoryTimeline;

public partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly MainViewModel _viewModel;

    // Map view names → UserControl factories
    private readonly Dictionary<string, Func<System.Windows.Controls.UserControl>> _viewFactory;

    public MainWindow(MainViewModel viewModel, INavigationService navigationService,
                      DashboardView dashboardView, TimelineView timelineView,
                      CalendarView calendarView, GalleryView galleryView,
                      MilestonesView milestonesView, FavoritesView favoritesView,
                      SettingsView settingsView)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _navigationService = navigationService;
        DataContext = _viewModel;

        _viewFactory = new Dictionary<string, Func<System.Windows.Controls.UserControl>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"]  = () => dashboardView,
            ["Timeline"]   = () => timelineView,
            ["Calendar"]   = () => calendarView,
            ["Gallery"]    = () => galleryView,
            ["Milestones"] = () => milestonesView,
            ["Favorites"]  = () => favoritesView,
            ["Settings"]   = () => settingsView,
        };

        _navigationService.NavigationChanged += OnNavigationChanged;

        // Show initial view
        ShowView("Dashboard");
    }

    private void OnNavigationChanged(object? sender, string viewName) => ShowView(viewName);

    private void ShowView(string viewName)
    {
        if (_viewFactory.TryGetValue(viewName, out var factory))
        {
            var ctrl = factory();
            MainContent.Content = ctrl;

            if (ctrl is DashboardView dv && dv.DataContext is DashboardViewModel dvm) _ = dvm.LoadAsync();
            else if (ctrl is TimelineView tv && tv.DataContext is TimelineViewModel tvm) _ = tvm.LoadAsync();
            else if (ctrl is CalendarView cv && cv.DataContext is CalendarViewModel cvm) _ = cvm.LoadAsync();
            else if (ctrl is GalleryView gv && gv.DataContext is GalleryViewModel gvm) _ = gvm.LoadAsync();
            else if (ctrl is MilestonesView mv && mv.DataContext is MilestonesViewModel mvm) _ = mvm.LoadAsync();
            else if (ctrl is FavoritesView fv && fv.DataContext is FavoritesViewModel fvm) _ = fvm.LoadAsync();
        }
    }
}
