# Phase 3A — Settings Page Full UI Redesign

## Overview
Complete redesign of the SuperAdmin Settings page into a premium healthcare SaaS admin interface. All existing functionality preserved — CSS uses app.css design tokens, views enhanced for modern visual hierarchy, mobile responsiveness, RTL support, and accessibility.

## Files Changed (8 files + 1 CSS section)

### 1. `Index.cshtml` — Main layout
- **Enhanced hero header**: Added decorative icon in frosted-glass circle (52×52px, `rgba(255,255,255,0.15)` background)
- **Mobile pills navigation**: New `.settings-pills` horizontal scrollable pill tabs visible on mobile (<992px) — replaces the old dropdown-only pattern
- **Desktop sidebar**: Unchanged vertical nav with list-group, sticky positioning
- **Dual nav sync**: Both sidebar and pills share `data-target` — clicking either switches sections
- **Per-form submit handler**: Added `submit` event listener on each form to disable the card-footer save button (prevents double-submit, shows loading spinner)
- **data-active attribute**: Sections use `data-active="true"`/`"false"` for the save-all JS selector
- All ARIA maintained and extended (`aria-selected`, `aria-controls`, `aria-expanded`, `aria-hidden`)

### 2. `_MailSettingsPartial.cshtml` — SMTP section
- Added `.settings-field-group` dividers: separates credentials → sender → test recipient into 3 visual groups
- Added `.settings-card-footer` with per-section Save button (`type="submit"`)
- Icon upgraded to `.header-icon-lg` (48px rounded square with shadow)

### 3. `_DocumentTemplateSettingsPartial.cshtml` — Document templates
- Added `.settings-field-group` dividers: separates numeric fields → extensions
- Added `.settings-card-footer` with Save button
- Icon upgraded to `.header-icon-lg`

### 4. `_BrandingSettingsPartial.cshtml` — Branding
- Added `.settings-field-group` dividers: separates site names → colors/support
- Added `.settings-card-footer` with Save button
- Icon upgraded to `.header-icon-lg`

### 5. `_NotificationsSettingsPartial.cshtml` — Notifications
- Added `.settings-field-group` dividers: separates expiry days → notification settings
- Added `.settings-card-footer` with Save button
- Icon upgraded to `.header-icon-lg`

### 6. `_LocalizationSettingsPartial.cshtml` — Localization
- Added `.settings-card-footer` with Save button
- Icon upgraded to `.header-icon-lg`

### 7. `_GeneralSettingsPartial.cshtml` — General
- Added `.settings-field-group` dividers: separates timeout → maintenance message
- Added `.settings-card-footer` with Save button
- Icon upgraded to `.header-icon-lg`

### 8. `_SettingsToggle.cshtml` — Toggle switch partial
- Unchanged from Phase 3A (already had `id`, `aria-label`, `aria-hidden`)

### 9. `site.css` — CSS additions (lines ~4657–4770)
- **`.header-icon-lg`**: 48px icon with `var(--radius-lg)` rounding + `var(--shadow-xs)` — bigger, more premium
- **`.settings-card-footer`**: Gray-50 background, top border, flex-end aligned, contains `.btn-primary` Save with disabled/loading state
- **`.settings-pills`**: Horizontal flex with `overflow-x: auto`, hidden scrollbar, `.pill` buttons with border/pill shape, active state mirrors sidebar active
- **`.settings-field-group`**: Top-border divider between related field groups within a card body
- **Responsive**: `.settings-pills` shown as `display:flex` at `max-width: 991.98px`

## Features Preserved
- All 6 form `asp-action` URLs and anti-forgery tokens
- All input `name` attributes (model binding intact)
- `GetSetting()`/`SettingValue()`/`SettingBool()` helper functions
- Save All button (submits active form via fetch with `data-active` selector)
- Send Test Email button
- Reset to Default button
- Hash-based navigation (`#mail`, `#branding`, etc.)
- Mobile sidebar toggle (retained for backward compatibility)
- Color picker live preview in Branding
- Password visibility toggle
- Toast notification system with `prefers-reduced-motion`

## New UX Improvements
- **Per-section Save buttons**: Each settings card now has its own Save button in the footer — users can save individual sections without using the global Save All
- **Form field grouping**: Related fields are visually grouped with subtle dividers (`.settings-field-group`)
- **Mobile pill navigation**: Horizontal scrollable pill tabs at the top on mobile for fast section switching
- **Loading state on per-form save**: Card footer button shows spinner and disables during form submission
- **Dual nav sync**: Pills and sidebar nav stay in sync when switching sections

## Responsive
- 1920–1024px: Sidebar nav (sticky) + content area
- 768px: Pills nav visible, sidebar collapses to toggle, cards go 1-column
- 375px: Pills scroll horizontally, action bar stacks vertically, card padding reduced

## RTL
- All new CSS uses logical properties (`margin-inline-start`, `inset-inline-end`, `padding-inline-start`, `border-start-end-radius`)
- RTL fallbacks exist for pills scroll and card header icon spacing
- Pills flex order naturally reverses in RTL

## Accessibility
- `aria-selected` on both sidebar and pill nav items
- `aria-controls` pointing to each section ID
- `aria-hidden="true"` on all decorative icons
- `aria-label` on password toggle buttons (English for assistive tech)
- Per-form submit disable prevents double-submit

## Build
- **0 errors**, **~113 warnings** (all pre-existing, none introduced)
