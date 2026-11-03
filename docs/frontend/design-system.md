# Pavena Design System Specification
**Baseline Version:** 1.0 (Phase 3 / Milestone H)  
**Style Archetype:** Academic Professional  
**Brand Tagline:** *Connecting Minds, Advancing Research with Integrity.*

---

## 1. Brand Personality & Visual Direction

Pavena delivers an environment tailored for international scholars, researchers, and scientific teams.
- **Tone:** Professional, Trustworthy, Modern, Minimalist, International.
- **Philosophy:** Content-first, calm canvas, restrained animation, high legibility for scientific formulas and academic writing.
- **Layout Density:** **Balanced** — comfortable whitespace without sparse emptiness or excessive data clutter.

---

## 2. Color Palette & Token Architecture

All colors are exposed through CSS custom properties in `tokens.css`. Direct hardcoded hex colors are disallowed in component markup.

### 2.1 Light Mode Baseline

| Token Name | Hex Code | Visual Role | WCAG Contrast Ratio |
| :--- | :--- | :--- | :--- |
| `--pv-color-primary` | `#17365D` | Deep Academic Navy (Main actions, brand) | **11.2:1** on surface (AAA) |
| `--pv-color-primary-hover`| `#102642` | Darker Navy Hover | **13.5:1** on surface (AAA) |
| `--pv-color-primary-subtle`| `#EBF1F8` | Light Blue Tint (Badges, active nav) | Complementary tint |
| `--pv-color-secondary` | `#187C78` | Refined Scholar Teal (Accents, highlights) | **4.7:1** on surface (AA) |
| `--pv-color-secondary-subtle`| `#E6F4F3`| Light Teal Tint | Complementary tint |
| `--pv-color-bg` | `#F7F9FC` | Off-white Cool Canvas | Background base |
| `--pv-color-surface` | `#FFFFFF` | Pure White (Cards, dialogs, dropdowns) | Container surface |
| `--pv-color-surface-hover` | `#F1F4F9` | Subtle Gray Surface Hover | Interaction feedback |
| `--pv-color-text` | `#202C3A` | Dark Slate (High contrast readable text) | **12.3:1** on background (AAA) |
| `--pv-color-text-muted` | `#697789` | Muted Slate (Secondary labels, metadata) | **4.6:1** on surface (AA) |
| `--pv-color-border` | `#E1E7EF` | Crisp Subtle Boundary Line | Structural division |

### 2.2 Dark Mode Baseline

| Token Name | Hex Code | Visual Role | WCAG Contrast Ratio |
| :--- | :--- | :--- | :--- |
| `--pv-color-primary` | `#8DB7EA` | Soft Academic Blue | **6.5:1** on surface (AA) |
| `--pv-color-primary-hover`| `#A5C8F2` | Lighter Blue Hover | **8.1:1** on surface (AAA) |
| `--pv-color-primary-subtle`| `#192D44` | Midnight Blue Tint | Complementary tint |
| `--pv-color-secondary` | `#55BDB2` | Sage Teal Accent | **6.9:1** on surface (AA) |
| `--pv-color-secondary-subtle`| `#173836`| Dark Pine Teal Tint | Complementary tint |
| `--pv-color-bg` | `#101923` | Deep Midnight Slate Canvas | Low eye-strain base |
| `--pv-color-surface` | `#1B2937` | Elevated Slate (Cards, dialogs) | Elevated surface |
| `--pv-color-surface-hover` | `#223447` | Elevated Surface Hover | Interaction feedback |
| `--pv-color-text` | `#E8EEF5` | Crisp White-Blue Text | **14.1:1** on background (AAA) |
| `--pv-color-text-muted` | `#A5B2C1` | Cool Gray Secondary Text | **6.1:1** on surface (AA) |
| `--pv-color-border` | `#334353` | Structural Dark Border | Boundary line |

---

## 3. Theme System & Flash Prevention (FOIT)

1. **Theme Modes:**
   - `Light`: Enforces light mode tokens.
   - `Dark`: Enforces dark mode tokens.
   - `System`: Dynamically respects OS `prefers-color-scheme`.
2. **Persistence:**
   - Stored in `localStorage` under key `pavena_theme`.
3. **Zero Flash of Incorrect Theme:**
   - Handled via `wwwroot/js/theme.js` inserted directly in the `<head>` of `App.razor`. It executes synchronously before HTML body rendering and attaches `data-theme` to `document.documentElement`.
4. **Live System Listening:**
   - A `matchMedia('(prefers-color-scheme: dark)')` listener dispatches updates to the active session when the operating system switches themes while in `System` mode.

---

## 4. Layout Density & Breakpoints

- **Density:** Balanced layout with `1rem` base spacing, `1.25rem` card body padding, and `64px` topbar height.
- **Maximum Width:** `--pv-content-max-width: 1240px` centered with auto margins.
- **Breakpoints:**
  - `Desktop`: $\ge 992\text{px}$ — Sticky sidebar with full navigation.
  - `Tablet & Mobile`: $< 992\text{px}$ — Off-canvas slide-out drawer with dimmed backdrop and mobile hamburger menu.
