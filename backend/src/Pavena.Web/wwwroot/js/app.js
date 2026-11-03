/**
 * PAVENA UI UTILITIES
 * Outside click detection, keyboard focus, and modal esc dismissal.
 */
window.PavenaUI = {
  activeDropdowns: new Set(),

  registerDropdown: function (id, dotNetHelper) {
    function handler(e) {
      const el = document.getElementById(id);
      if (el && !el.contains(e.target)) {
        dotNetHelper.invokeMethodAsync('CloseDropdown');
      }
    }
    document.addEventListener('click', handler);
  },

  trapFocus: function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const focusable = el.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
    if (focusable.length > 0) {
      focusable[0].focus();
    }
  }
};
