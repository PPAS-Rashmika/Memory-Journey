using CommunityToolkit.Mvvm.ComponentModel;
using MemoryTimeline.Services;

namespace MemoryTimeline.ViewModels;

public partial class NavigationViewModelBase : ObservableObject
{
    protected readonly INavigationService NavigationService;

    public NavigationViewModelBase(INavigationService navigationService)
    {
        NavigationService = navigationService;
    }
}
