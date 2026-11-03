namespace Pavena.Web.Services.Theme;

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    string EffectiveTheme { get; }
    event Action? OnThemeChanged;
    Task InitializeAsync();
    Task SetThemeAsync(ThemeMode mode);
}
