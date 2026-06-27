# Phase 2B — Layout Stabilization & Regression Verification

## Files Changed

| File | Change |
|---|---|
| `wwwroot/js/site.js` | Added `aria-hidden` sync for desktop sidebar collapse state (line 76) |
| `Views/Shared/_Layout.cshtml` | Removed redundant `role="contentinfo"` from `<footer>` (line 350) |
| `Views/Shared/_Breadcrumb.cshtml` | Removed redundant `@inject ITranslationService Localizer` (already in `_ViewImports`) |
| `wwwroot/css/app.css` | Added breadcrumb overflow/ellipsis CSS (section 9); fixed section numbering (sections 10→35 shifted to 11→35) |

## Bugs Fixed

### 1. Desktop Sidebar `aria-hidden` Not Synced (Bug)
- **Root cause**: `toggleSidebar()` in `site.js` only set `aria-hidden` on the sidebar in mobile mode (line 67). When sidebar was collapsed on desktop via `sidebar-collapsed` class, it was visually hidden (`translateX(-100%)`) but still present in the accessibility tree — screen reader users could tab into invisible navigation links.
- **Fix**: Added `sidebar.setAttribute('aria-hidden', ...)` in the desktop collapse branch (line 76), matching the mobile behavior. Now the sidebar is correctly hidden from screen readers when collapsed on desktop.

### 2. Footer Redundant `role="contentinfo"` (Cleanup)
- **Root cause**: `<footer>` elements have an implicit ARIA role of `contentinfo`. The explicit `role="contentinfo"` attribute was redundant.
- **Fix**: Removed `role="contentinfo"` from `<footer class="app-footer">`.

### 3. Breadcrumb Redundant Dependency Injection (Cleanup)
- **Root cause**: `_Breadcrumb.cshtml` had `@inject ITranslationService Localizer` which is already provided globally by `_ViewImports.cshtml`.
- **Fix**: Removed the redundant inject directive.

### 4. Breadcrumb Overflow / Long Titles (Prevention)
- **Root cause**: Long breadcrumb labels could overflow their container without ellipsis.
- **Fix**: Added CSS for `.breadcrumb-item` with `max-width: 280px`, `overflow: hidden`, `text-overflow: ellipsis`, and `white-space: nowrap`. Breadcrumb container has `flex-wrap: wrap` for mobile.

## Responsive Fixes

- None required beyond existing CSS. The responsive breakpoints (1199.98px, 991.98px, 767.98px, 575.98px) were verified to handle sidebar collapse, header positioning, footer margins, and content padding correctly in both LTR and RTL.

## RTL Verification

All RTL-specific overrides verified:
- Sidebar position: `inset-inline-start: 0` → `right: 0` ✓
- Header position: `inset-inline-start: var(--sidebar-w); inset-inline-end: 0` → `right: 260px; left: 0` ✓
- Content margin: `margin-inline-start: var(--sidebar-w)` → `margin-right: 260px` ✓
- Footer margin: `margin-inline-start: var(--sidebar-w)` → `margin-right: 260px` ✓
- Collapse animation: `translateX(100%)` in RTL ✓
- Mobile sidebar: slides from right side in RTL ✓
- Breadcrumb: `text-overflow: ellipsis` works in RTL ✓

## Accessibility Improvements

| Attribute | Component | Status |
|---|---|---|
| `aria-label` | Sidebar `<aside>`, hamburger, notifications, user menu, brand link, developer link, breadcrumb `<nav>` | ✓ All present |
| `aria-expanded` | Hamburger button, collapse toggles | ✓ Synced by JS + Bootstrap |
| `aria-controls` | Hamburger → sidebar, collapse toggles | ✓ |
| `aria-current="page"` | Active nav links (from `ActiveRouteTagHelper`) | ✓ Exact page match only |
| `aria-hidden="true"` | All `<i>` icons, sidebar overlay, sidebar when collapsed, user/nav icons | ✓ Synced on desktop collapse now |
| `aria-haspopup="true"` | Notification + user dropdown triggers | ✓ |
| `aria-pressed` | Language toggle buttons | ✓ |
| `keyboard navigation` | Sidebar toggle (Escape), dropdowns (Bootstrap default) | ✓ |
| `focus management` | Hamburger → first nav link on mobile open, hamburger focus on Escape | ✓ |

## Remaining Issues

**Pre-existing — not addressed** (no risk, cosmetic, or out of scope):
1. User dropdown "Settings" and "Profile" both link to `Account/Profile` — the settings URL was never updated from the original template. Requires controller knowledge to fix (out of scope for layout stabilization).
2. `initLanguageToggle()` uses `location.reload()` which causes a full page reload on language switch. This is the existing behavior and ensures server-side localization is consistent.
3. No full focus trap implementation in mobile sidebar (Tab key not trapped). Current navigation works for basic use cases.

## Files Not Modified

No controllers, services, models, DTOs, repositories, middleware, routing, localization, authentication, authorization, or any business logic files were touched.

## Build Result

- **Errors**: 0
- **Warnings**: 119 (all pre-existing CS860x nullable warnings from controllers — unchanged)
