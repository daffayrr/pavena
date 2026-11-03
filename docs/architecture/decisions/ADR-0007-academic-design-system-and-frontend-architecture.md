# ADR-0007: Academic Design System & Blazor Frontend Architecture

## Status
Accepted

## Date
2026-11-03

## Context
Pavena is an academic and professional social collaboration platform. The visual and user interaction experience must project credibility, academic integrity, focus, and modern usability rather than imitating commercial entertainment social media.

Furthermore, academic scholars interact with long-form scientific documents, mathematical notation, and research metadata, requiring:
1. Dual-theme support (Light Mode and Dark Mode) with system preference integration and zero flash of incorrect theme (FOIT).
2. Balanced layout density (comfortable whitespace, readable typography, moderate card padding, responsive from mobile to wide monitors).
3. Centralized CSS design tokens avoiding hardcoded color leaks across components.
4. Reusable, accessible UI components (Buttons, Inputs, Search with shortcuts, Cards, Avatars, Badges, Dropdowns, Modals, Tabs, Toasts, Skeletons, Empty States, and Error States).
5. Seamless alignment with the existing .NET 8 LTS modular monolith backend.

## Decision
1. **Frontend Framework:** Adopt **Blazor Web App (.NET 8 LTS)** residing in `backend/src/Pavena.Web`, directly integrated into `backend/Pavena.sln`.
2. **Visual Direction:** "Academic Professional" characterized by deep navy (`#17365D`) and soft blue (`#8DB7EA`) primary colors, refined scholar teal (`#187C78` / `#55BDB2`) secondary accents, clean surfaces, and high WCAG 2.1 AA/AAA contrast ratios.
3. **Design Tokens:** Centralized in `wwwroot/css/tokens.css` exposing standard CSS custom properties (`--pv-color-*`, `--pv-space-*`, `--pv-radius-*`, `--pv-shadow-*`).
4. **Theme Preference & FOIT Prevention:**
   - Supported modes: `Light`, `Dark`, `System`.
   - Persisted securely in `localStorage` under key `pavena_theme`.
   - Instant `<head>` inline execution (`theme.js`) applying `data-theme` attribute to the root document element before DOM content renders, ensuring zero theme flash.
   - Live media query watcher (`prefers-color-scheme: dark`) synchronizing system adjustments dynamically.
5. **Application Shell:**
   - `Topbar`: Global branding, debounced search (`PvSearch`), notification badge, theme switcher (`PvThemeToggle`), and user account dropdown.
   - `Sidebar`: Desktop sticky navigation with responsive off-canvas mobile drawer and backdrop dismissal.
   - `Main Content Area`: Centered, balanced container adhering to `max-width: 1240px`.

## Consequences
- **Positive:**
  - Complete type safety and shared DTOs with `Pavena.Application`.
  - Accessible, WCAG AA/AAA compliant color contrast across both themes.
  - Zero scattered hardcoded color declarations; any future theme tuning occurs solely in `tokens.css`.
  - High performance, responsive layout supporting desktop, tablet, and mobile.
- **Trade-off:**
  - Interactive server components require active WebSocket connection; client-side WASM or hybrid can be layered for offline document editing in future milestones.
