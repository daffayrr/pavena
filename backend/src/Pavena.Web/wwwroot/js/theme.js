/**
 * PAVENA THEME SYSTEM
 * Handles instant FOUC-prevention, localStorage persistence, and system preference tracking.
 */
(function () {
  const STORAGE_KEY = 'pavena_theme';

  function getSystemPreference() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function resolveEffectiveTheme(theme) {
    if (theme === 'dark' || theme === 'light') {
      return theme;
    }
    return getSystemPreference();
  }

  function applyTheme(theme) {
    const effective = resolveEffectiveTheme(theme);
    document.documentElement.setAttribute('data-theme', effective);
    document.documentElement.style.colorScheme = effective;
  }

  // Early init before body is rendered (prevent flash)
  try {
    const saved = localStorage.getItem(STORAGE_KEY) || 'system';
    applyTheme(saved);
  } catch (e) {
    console.warn('Pavena theme init fallback:', e);
  }

  // Expose JS interop for Blazor
  window.PavenaTheme = {
    getTheme: function () {
      return localStorage.getItem(STORAGE_KEY) || 'system';
    },
    setTheme: function (theme) {
      if (theme !== 'light' && theme !== 'dark' && theme !== 'system') {
        theme = 'system';
      }
      localStorage.setItem(STORAGE_KEY, theme);
      applyTheme(theme);
      return resolveEffectiveTheme(theme);
    },
    getEffectiveTheme: function () {
      const theme = localStorage.getItem(STORAGE_KEY) || 'system';
      return resolveEffectiveTheme(theme);
    },
    initSystemWatcher: function (dotNetHelper) {
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      mediaQuery.addEventListener('change', function (e) {
        const currentPreference = localStorage.getItem(STORAGE_KEY) || 'system';
        if (currentPreference === 'system') {
          applyTheme('system');
          if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnSystemThemeChanged', e.matches ? 'dark' : 'light');
          }
        }
      });
    }
  };
})();
