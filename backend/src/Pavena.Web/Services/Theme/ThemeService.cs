using Microsoft.JSInterop;

namespace Pavena.Web.Services.Theme;

public class ThemeService : IThemeService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ThemeService>? _dotNetRef;
    private bool _initialized;

    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.System;
    public string EffectiveTheme { get; private set; } = "light";

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            var saved = await _jsRuntime.InvokeAsync<string?>("PavenaTheme.getTheme");
            CurrentTheme = saved?.ToLowerInvariant() switch
            {
                "light" => ThemeMode.Light,
                "dark" => ThemeMode.Dark,
                _ => ThemeMode.System
            };

            var effective = await _jsRuntime.InvokeAsync<string?>("PavenaTheme.getEffectiveTheme");
            EffectiveTheme = effective ?? "light";

            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("PavenaTheme.initSystemWatcher", _dotNetRef);
            _initialized = true;
            OnThemeChanged?.Invoke();
        }
        catch (JSException)
        {
            // Prerendering fallback
        }
        catch (InvalidOperationException)
        {
            // JS runtime not yet available
        }
    }

    public async Task SetThemeAsync(ThemeMode mode)
    {
        CurrentTheme = mode;
        var themeString = mode switch
        {
            ThemeMode.Light => "light",
            ThemeMode.Dark => "dark",
            _ => "system"
        };

        try
        {
            EffectiveTheme = await _jsRuntime.InvokeAsync<string>("PavenaTheme.setTheme", themeString);
        }
        catch
        {
            EffectiveTheme = mode == ThemeMode.Dark ? "dark" : "light";
        }

        OnThemeChanged?.Invoke();
    }

    [JSInvokable]
    public void OnSystemThemeChanged(string systemTheme)
    {
        if (CurrentTheme == ThemeMode.System)
        {
            EffectiveTheme = systemTheme;
            OnThemeChanged?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        await ValueTask.CompletedTask;
    }
}
