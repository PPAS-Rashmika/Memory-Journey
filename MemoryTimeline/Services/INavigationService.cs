namespace MemoryTimeline.Services;

public interface INavigationService
{
    void NavigateTo(string viewName);
    string CurrentView { get; }
    event EventHandler<string>? NavigationChanged;
}
