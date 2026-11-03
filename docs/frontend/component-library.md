# Pavena Reusable Component Catalog

All components reside in `Pavena.Web.Components.UI` and `Pavena.Web.Components.Common`.

---

## 1. Components Overview

| Component | Selector | Key Parameters | Purpose |
| :--- | :--- | :--- | :--- |
| **Button** | `<PvButton />` | `Variant`, `Size`, `IsLoading`, `Disabled`, `IconOnly`, `OnClick` | Primary, secondary, outline, ghost, danger actions with loading spinners. |
| **Input** | `<PvInput />` | `Label`, `Type`, `Value`, `Placeholder`, `HelperText`, `ErrorMessage`, `IsMultiline` | Accessible form textboxes and textareas with inline error states. |
| **Search** | `<PvSearch />` | `Placeholder`, `Value`, `OnSearch`, `DebounceMs`, `ShowShortcut` | Debounced search bar with clear button and `Ctrl+K` shortcut badge. |
| **Card** | `<PvCard />` | `Header`, `ChildContent`, `Footer`, `IsHoverable` | Elevated content container with subtle borders and balanced padding. |
| **Avatar** | `<PvAvatar />` | `ImageUrl`, `Name`, `Size`, `Status` | Scholar portrait with automatic fallback initials and online status indicator. |
| **Badge** | `<PvBadge />` | `Variant`, `ShowDot`, `ChildContent` | Status labels (`primary`, `secondary`, `success`, `warning`, `danger`, `neutral`). |
| **Dropdown** | `<PvDropdown />` | `Trigger`, `ChildContent`, `Align` | Popover menu with click-outside detection and keyboard navigation. |
| **Modal** | `<PvModal />` | `IsOpen`, `Title`, `ChildContent`, `Footer`, `MaxWidth`, `OnClose` | Dialog with focus trap, dimmed backdrop, and ESC key dismissal. |
| **Tabs** | `<PvTabs />` | `Tabs`, `ActiveTab`, `ActiveTabChanged` | Accessible navigation tabs adhering to WAI-ARIA `role="tablist"`. |
| **Toast** | `<PvToastContainer />` | Handled via `IToastService` | Global notification stack (`Success`, `Info`, `Warning`, `Danger`) with auto-dismiss. |
| **Loading** | `<PvLoading />` | `Type`, `Size`, `Circle`, `Style` | Shimmer skeletons (cards, text lines) and spinner indicators. |
| **Empty State** | `<PvEmptyState />` | `Title`, `Description`, `CustomIcon`, `ActionContent` | Academic empty state display with call-to-action button. |
| **Error State** | `<PvErrorState />` | `Title`, `Message`, `RetryLabel`, `OnRetry` | Error boundary banner with retry action callback. |
| **Theme Toggle**| `<PvThemeToggle />` | None | Dropdown selector for Light, Dark, or System mode with checkmark state. |
| **Brand Logo** | `<PvBrandLogo />` | `ShowTagline`, `Size` | Pavena monogram brand mark and official typography. |

---

## 2. Interactive Verification Hub

A comprehensive interactive showcase is hosted on the route `/design-system`, allowing real-time testing of:
- All button variants, sizes, and states.
- Input validation states (valid, error, multiline).
- Real-time search debouncing.
- Dynamic modal launch and backdrop dismissal.
- Live Light/Dark/System theme switching with zero page reload.
