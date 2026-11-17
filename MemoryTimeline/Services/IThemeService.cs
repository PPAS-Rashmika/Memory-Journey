namespace MemoryTimeline.Services;

public enum ThemeName
{
    Dark,
    Light
}

public interface IThemeService
{
    void ApplyTheme(ThemeName theme);
    ThemeName CurrentTheme { get; }
    event EventHandler<ThemeName>? ThemeChanged;
}
