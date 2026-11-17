using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoryTimeline.Services;
using System.Windows;

namespace MemoryTimeline.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _currentView = "Dashboard";

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    public MainViewModel(
        INavigationService navigationService,
        IThemeService themeService,
        ISettingsService settingsService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        _settingsService = settingsService;

        _isDarkTheme = _settingsService.IsDarkTheme;
        _navigationService.NavigationChanged += (s, view) => CurrentView = view;
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        _navigationService.NavigateTo(viewName);
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _settingsService.IsDarkTheme = IsDarkTheme;
        _themeService.ApplyTheme(IsDarkTheme ? ThemeName.Dark : ThemeName.Light);
    }
}
