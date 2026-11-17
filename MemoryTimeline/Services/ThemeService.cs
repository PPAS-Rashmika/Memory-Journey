using System.Windows;

namespace MemoryTimeline.Services;

public class ThemeService : IThemeService
{
    private ThemeName _currentTheme = ThemeName.Dark;

    public ThemeName CurrentTheme => _currentTheme;
    public event EventHandler<ThemeName>? ThemeChanged;

    public void ApplyTheme(ThemeName theme)
    {
        _currentTheme = theme;

        var app = Application.Current;
        if (app == null) return;

        // Remove existing theme dictionaries
        var toRemove = app.Resources.MergedDictionaries
            .Where(d => d.Source != null &&
                   (d.Source.OriginalString.Contains("Dark.xaml") ||
                    d.Source.OriginalString.Contains("Light.xaml")))
            .ToList();

        foreach (var dict in toRemove)
            app.Resources.MergedDictionaries.Remove(dict);

        // Add the selected theme
        var themePath = theme == ThemeName.Dark
            ? "Themes/Dark.xaml"
            : "Themes/Light.xaml";

        var newDict = new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        };
        app.Resources.MergedDictionaries.Add(newDict);

        ThemeChanged?.Invoke(this, theme);
    }
}
