# UI Audit Report

## Overview
This report documents the complete Presentation layer of **AmbulatoryCarePortal** — all layouts, views, partials, CSS, and JS. It catalogs every page, identifies duplicate/redundant components, documents inconsistent patterns, and ranks redesign priorities.

---

## 1. Layout System

| Layout | File | Purpose |
|--------|------|---------|
| `_Layout.cshtml` (main) | `Views/Shared/_Layout.cshtml` | Logged-in users — sidebar nav + topbar + footer |
| `_LoginLayout.cshtml` | `Views/Shared/_LoginLayout.cshtml` | Auth pages — split-panel (brand left, form right) |

### 1.1 Main `_Layout.cshtml` architecture
- **Sidebar**: collapsible, nav items iterated from ViewBag/ViewData, active-state logic inline
- **Topbar**: user avatar dropdown, logout, notification bell
- **Footer**: copyright, version
- **Script sections**: `@RenderSection("Scripts", required: false)`
- **Style sections**: `@RenderSection("Styles", required: false)` — only used by ComplianceDashboard

### 1.2 `_LoginLayout.cshtml` architecture
- Split-panel: brand panel (icon, app name, tagline, footer) + form panel
- Used by: Login, ForgotPassword, ResetPassword, ConfirmEmail, ResetPasswordConfirmation

---

## 2. Complete Page Inventory

### 2.1 Auth Pages (Views/Account)

| View | Layout | Notes |
|------|--------|-------|
| `Login.cshtml` | `_LoginLayout` | Full split-panel, loading spinner on submit, rate-limited |
| `ForgotPassword.cshtml` | `_LoginLayout` | Same layout, email input + submit |
| `ResetPassword.cshtml` | `_LoginLayout` | Password/confirm with toggle-eye, loading spinner |
| `ResetPasswordConfirmation.cshtml` | `_LoginLayout` | Success state, link to login |
| `ConfirmEmail.cshtml` | `_LoginLayout` | Single confirm button, loading spinner |
| `AccessDenied.cshtml` | `_Layout` (main) | 403 error page, uses `.error-page` classes |
| `Profile.cshtml` | `_Layout` (main) | User info card, resend confirmation, logout |

### 2.2 SuperAdmin Area (Areas/SuperAdmin)

| Controller | Views | Purpose |
|------------|-------|---------|
| **Dashboard** | `Index.cshtml`, `Clinics.cshtml`, `ClinicDetail.cshtml`, `Edit.cshtml`, `CreateClinic.cshtml`, `AuditLog.cshtml` | Executive dashboard, clinic CRUD, audit log |
| **UserManagement** | `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `ActivityLog.cshtml` | User CRUD, role/clinic assignment |
| **DocumentTemplates** | `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `Preview.cshtml` | Document template management |
| **Settings** | `Index.cshtml` + 6 partials | Tabbed SPA settings (mail, document template, branding, notifications, localization, general) |

### 2.3 ClinicAdmin Area (Areas/ClinicAdmin)

| Controller | Views | Purpose |
|------------|-------|---------|
| **Dashboard** | `Index.cshtml`, `Staff.cshtml`, `Policies.cshtml`, `KPIs.cshtml`, `ExpiringDocuments.cshtml`, `ComplianceScore.cshtml`, `ComplianceDashboard.cshtml`, `ComplianceCalendar.cshtml` | Main clinic dashboard, specialized dashboards |
| **HRManagement** | `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `ExpiringDocuments.cshtml`, `NonCompliantStaff.cshtml` | Staff CRUD, expiry/compliance filtering |
| **PolicyManagement** | `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml` | Policy document CRUD with filtering |
| **PolicyDocuments** | `Index.cshtml` | Alternate document listing (standards-tab filter) |
| **DepartmentManagement** | `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml` | Department CRUD |
| **KPIManagement** | `Index.cshtml`, `Create.cshtml`, `EnterData.cshtml`, `ViewAnalytics.cshtml`, `Compare.cshtml`, `ByDepartment.cshtml` | KPI monitoring, data entry, analytics |
| **ChecklistManagement** | `Index.cshtml` **(EMPTY)**, `Create.cshtml`, `Execute.cshtml`, `ViewHistory.cshtml` | Checklist execution & history |
| **Forms** | `Index.cshtml`, `Create.cshtml`, `Versions.cshtml` | Form library (card grid layout) |
| **Notifications** | `Index.cshtml` | Notification list with mark-read |
| **Reporting** | `Index.cshtml`, `Analytics.cshtml`, `ReportBuilder.cshtml` | Report type grid, builder, analytics |
| **ClinicDocuments** | `Index.cshtml`, `Details.cshtml` | Document assignments with filter |
| **ClinicSignatures** | `Index.cshtml` | Digital signatures |
| **Shared** | `_PageHeader.cshtml`, `_HealthMetricCard.cshtml`, `_EmptyState.cshtml`, `_ComplianceScoreWidget.cshtml` | Area-specific shared partials |

---

## 3. Shared Partials Inventory

### 3.1 Root `Views/Shared`

| Partial | Used By | Notes |
|---------|---------|-------|
| `_StatusBadge.cshtml` | (appears in root only) | Simple status dot + label |
| `_Pagination.cshtml` | (appears in root only) | Pagination with page numbers |
| `_HealthMetricCard.cshtml` | Dashboard views, Info panels | Metric card with icon/value/label/badge/link |
| `_EmptyState.cshtml` | Table empty states | Centered icon + title + optional description |
| `_DeleteConfirmation.cshtml` | PolicyManagement Index, etc. | Bootstrap modal for delete confirm |
| `_ValidationScriptsPartial.cshtml` | All Create/Edit forms | jQuery validation scripts |
| `_DeveloperContactCard.cshtml` | Error page | Developer contact info on error |
| `_Layout.cshtml` | All logged-in pages | Main application layout |
| `_LoginLayout.cshtml` | Auth pages | Split-panel auth layout |

### 3.2 SuperAdmin `Areas/SuperAdmin/Views/Shared`

| Partial | Duplicate of Root? | Notes |
|---------|--------------------|-------|
| `_StatusBadge.cshtml` | **YES** — different impl | Uses `um-*` prefix classes instead of Bootstrap |
| `_Pagination.cshtml` | **YES** — different impl | Uses `um-pag-*` classes vs Bootstrap pagination |
| `_RoleBadge.cshtml` | Unique | Role-specific icon + label (SuperAdmin/ClinicAdmin/ClinicViewer) |
| `_UserAvatar.cshtml` | Unique | Avatar initial + name + email + active indicator |

### 3.3 ClinicAdmin `Areas/ClinicAdmin/Views/Shared`

| Partial | Duplicate of Root? | Notes |
|---------|--------------------|-------|
| `_PageHeader.cshtml` | **UNIQUE** | Dynamic header with title/subtitle/actions (dynamic object model) |
| `_HealthMetricCard.cshtml` | **YES** — same as root | Identical functionality, different file |
| `_EmptyState.cshtml` | **YES** — same as root | Identical functionality, different file |
| `_ComplianceScoreWidget.cshtml` | Unique | SVG gauge + progress bars + component scores |

---

## 4. CSS Architecture

### 4.1 Files

| File | Lines | Purpose |
|------|-------|---------|
| `app.css` | ~1970 | **THE** stylesheet — custom design system, all UI components |
| `site.css` | ~296 | Minor overrides + utility classes |
| `bootstrap.min.css` | vendor | Bootstrap 5 (CDN) |

### 4.2 CSS Custom Properties (Design Tokens)
`app.css` defines comprehensive CSS custom properties:

```css
:root {
  --primary-color: #1a73e8;
  --primary-light: #e8f0fe;
  --primary-dark: #1557b0;
  --success-color: #1e8e3e;
  --warning-color: #f9ab00;
  --danger-color: #d93025;
  --info-color: #185abc;
  --bg-primary: #f0f2f5;
  --bg-secondary: #ffffff;
  --text-primary: #202124;
  --text-secondary: #5f6368;
  --border-color: #dadce0;
  --sidebar-width: 260px;
  --sidebar-collapsed: 60px;
  --header-height: 60px;
  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 16px;
}
```

### 4.3 Key CSS Component Classes

| Category | Classes |
|----------|---------|
| **Sidebar** | `.sidebar`, `.sidebar-nav`, `.nav-item`, `.nav-link`, `.nav-icon`, `.nav-label`, `.sidebar-collapsed` |
| **Topbar** | `.topbar`, `.topbar-left`, `.topbar-right`, `.user-dropdown` |
| **Cards** | `.card`, `.card-primary`, `.card-header`, `.card-body`, `.card-footer`, `.card-tools` |
| **Tables** | `.table-container`, `.table-toolbar`, `.table-footer`, `.toolbar-left`, `.toolbar-right` |
| **Search** | `.search-box`, `.search-icon` |
| **Filters** | `.filter-bar`, `.filter-group` |
| **Status Pills** | `.status-pill`, `.status-pill.active`, `.status-pill.inactive`, `.status-pill.compliant`, `.status-pill.needs-review`, `.status-pill.non-compliant` |
| **Badges** | `.badge`, `.badge-*` (Bootstrap), `.um-badge-*` (custom) |
| **Metric Cards** | `.metric-card`, `.metric-top`, `.metric-icon`, `.metric-value`, `.metric-label`, `.metric-footer`, `.metric-badge` |
| **Progress** | `.progress`, `.progress-bar`, `.progress-xs`, `.progress-sm`, `.progress-md`, `.progress-group` |
| **Score Ring** | `.readiness-score`, `.score-ring`, `.score-value`, `.score-label` |
| **Pagination** | `.pagination`, `.page-item`, `.page-link` (Bootstrap) + `.um-pagination`, `.um-pag-list`, `.um-pag-item`, `.um-pag-link` (custom) |
| **Page Header** | `.page-header`, `.page-header-title`, `.page-header-subtitle`, `.page-header-actions`, `.page-icon` |
| **Action Grid** | `.action-grid`, `.action-card-btn`, `.action-title`, `.action-desc` |
| **Info Rows** | `.health-info-row`, `.hi-label`, `.hi-value` |
| **Settings** | `.settings-layout`, `.settings-sidebar`, `.settings-content`, `.settings-section`, `.settings-action-bar`, `.settings-toast` |
| **Error Page** | `.error-page`, `.headline`, `.error-content` |
| **Fade In** | `.fade-in` (animation) |
| **Dashboard Hero** | `.dashboard-hero`, `.hero-content`, `.hero-title`, `.hero-subtitle` |
| **Forms** | `.form-group`, `.form-card`, `.form-card-header`, `.form-card-body`, `.form-label` |
| **Avatar** | `.um-avatar`, `.um-user-cell`, `.um-user-info`, `.um-user-name`, `.um-user-email` |
| **Role** | `.um-role-badge`, `.um-role-superadmin`, `.um-role-admin`, `.um-role-viewer`, `.um-role-none` |
| **Login** | `.login-container`, `.login-brand-panel`, `.login-form-panel`, `.login-card`, `.login-btn`, `.login-error`, `.login-success`, `.login-form-footer`, `.login-brand-footer` |
| **Compliance** | `.compliance-badge`, `.badge-dot` |

---

## 5. JavaScript Architecture

### 5.1 Files

| File | Purpose |
|------|---------|
| `site.js` | jQuery doc-ready wrapper, confirm dialogs, table search filtering, toggle accessibility |
| `jquery.min.js` | jQuery 3.x |
| `bootstrap.min.js` | Bootstrap 5 JS bundle |
| `jquery.validate.min.js` | jQuery Validate |
| `jquery.validate.unobtrusive.min.js` | ASP.NET Unobtrusive Validation |
| Chart.js (CDN) | Loaded on ComplianceDashboard only (`cdn.jsdelivr.net/npm/chart.js@4.4.0`) |

### 5.2 Inline JavaScript Patterns

| Pattern | Views Using It | Notes |
|---------|---------------|-------|
| Delete modal handler | PolicyManagement Index | Retrieves data-policy-id/title on modal show |
| Compliance refresh | Dashboard Index, ComplianceDashboard | `$.post` to update score, then reload |
| Chart.js rendering | ComplianceDashboard | Trend line + department bar charts |
| Settings tab switching | Settings Index | Pure JS tab switch, hash-based deep link, toast system |
| Settings save-all | Settings Index | Serializes active form, fetch POST, toast feedback |
| Test email button | Settings Index | Fetch + toast |
| Password toggle | Settings Index, ResetPassword | Toggles input type password/text |
| Notification mark-read | Notifications Index | Fetch POST with anti-forgery token |
| Calendar load | ComplianceDashboard | jQuery `.load()` to fetch calendar partial |
| Recipient validation | Reporting Index | Inline form validation for email field |

---

## 6. Patterns & Inconsistencies

### 6.1 Duplicate Partials (Highest Priority)

| Root Shared | Area Duplicate | Diff |
|-------------|---------------|------|
| `_StatusBadge.cshtml` | SuperAdmin `_StatusBadge.cshtml` | Root uses simple Bootstrap; SuperAdmin uses `um-*` classes |
| `_Pagination.cshtml` | SuperAdmin `_Pagination.cshtml` | Root uses Bootstrap pagination; SuperAdmin uses `um-pag-*` classes |
| `_HealthMetricCard.cshtml` | ClinicAdmin `_HealthMetricCard.cshtml` | **Identical code** — unnecessary duplicate |
| `_EmptyState.cshtml` | ClinicAdmin `_EmptyState.cshtml` | **Near-identical code** — unnecessary duplicate |

### 6.2 Three Different Pagination Implementations

1. **Root `_Pagination.cshtml`** — uses Bootstrap `.pagination`, `.page-item`, `.page-link`
2. **SuperAdmin `_Pagination.cshtml`** — uses custom `um-pag-list`, `um-pag-item`, `um-pag-link`
3. **Inline in SuperAdmin UserManagement `Index.cshtml`** — hand-rolled `<ul class="pagination">` with `page-link` anchors, hardcoded `?page=` query strings, **does NOT use the shared partial at all**

Additionally, several ClinicAdmin views have **inline pagination** in `table-footer` / `page-links` divs with `chevron-left`/`chevron-right` icons — yet another pattern.

### 6.3 Two Page Header Patterns

| Pattern | Used Where | Implementation |
|---------|-----------|---------------|
| **Via `_PageHeader` partial** | All ClinicAdmin views (consistent) | Dynamic object model, icon + title + subtitle + actions |
| **Inline markup** | All SuperAdmin views | Repeated `.page-header` structure in each view (inconsistent details) |

### 6.4 Hardcoded URLs vs Tag Helpers

| Approach | Used Where | Risk |
|----------|-----------|------|
| `asp-action`/`asp-controller` tag helpers | Most ClinicAdmin views | Safe — resolves at runtime |
| Hardcoded `/SuperAdmin/Dashboard/Clinics` | SuperAdmin views | **Brittle** — breaks if routing changes |
| Hardcoded `/SuperAdmin/UserManagement/Edit/@user.UserId` | SuperAdmin UserManagement Index | Same risk |

### 6.5 Card Header Patterns

| Area | Card Class | Header Pattern |
|------|-----------|---------------|
| SuperAdmin | `div.card` (no modifier) | `<div class="card-header"><h3>...</h3><div class="card-tools">...</div></div>` |
| ClinicAdmin | `div.card.card-primary` | `<div class="card-header"><h3>...</h3></div>` or `<div class="card-header"><h3>...</h3><span/div class="d-flex">...</div></div>` |
| SuperAdmin Dashboard | `div.card.card-primary` | Mixed — some use `card-primary`, some don't |

### 6.6 Status Badge / Pill Classes

| Class | Used Where | Meaning |
|-------|-----------|---------|
| `badge badge-success/danger/info/warning` | SuperAdmin views | Bootstrap badges |
| `status-pill active/inactive/compliant/needs-review/non-compliant` | ClinicAdmin views | Custom pill component |
| `um-status-dot.active/inactive` + `um-status-label` | SuperAdmin `_StatusBadge` partial | Custom status indicator |

### 6.7 Button Variant Inconsistencies

| Variant | Views Using It |
|---------|---------------|
| `btn btn-sm btn-primary` | Most consistent |
| `btn btn-sm btn-warning` (edit) | SuperAdmin UserManagement |
| `btn btn-sm btn-outline-warning` (edit) | ClinicAdmin PolicyManagement |
| `btn btn-sm btn-outline-info` (view) | ClinicAdmin views |
| `btn btn-sm btn-info` (activity log) | SuperAdmin UserManagement |
| `btn btn-sm btn-secondary btn-icon` | SuperAdmin Dashboard (with tooltip) |
| `btn btn-sm btn-outline-secondary` | ClinicAdmin filter clears |

### 6.8 Empty State Handling

| Pattern | Views |
|---------|-------|
| Via `_EmptyState` partial (dynamic) | Most ClinicAdmin list views |
| Inline empty-state markup | SuperAdmin Dashboard, Clinics, UserManagement |

### 6.9 Form Layout Patterns

| Pattern | Views |
|---------|-------|
| `div.row.justify-content-center > div.col-lg-8 > div.card.card-primary` | ClinicAdmin Create/Edit forms (consistent) |
| `div.row.fade-in > div.col-md-8.offset-md-2 > div.card.card-primary` | SuperAdmin UserManagement Create |
| `div.row.justify-content-center > div.col-md-8 > div.card.card-primary` | ClinicAdmin Forms Create |

### 6.10 Modal Delete Confirmation

| Pattern | Views |
|---------|-------|
| Via `_DeleteConfirmation` shared partial | PolicyManagement Index |
| Inline `confirm()` in form onsubmit | SuperAdmin UserManagement, SuperAdmin Clinics, ClinicAdmin Forms |
| Inline JavaScript modal handler | PolicyManagement Index (fetch + modal) |

---

## 7. CDN Dependencies

| Library | Version | Loaded From | Where Used | Fallback? |
|---------|---------|-------------|-----------|-----------|
| Chart.js | 4.4.0 | `cdn.jsdelivr.net` | ComplianceDashboard only | **No** |

---

## 8. Accessibility Observations

- **Good**: Many views have `aria-label`, `role`, `aria-required`, `aria-describedby` attributes
- **Inconsistent**: SuperAdmin Dashboard action buttons lack aria labels; ClinicAdmin views are generally better
- **Search forms**: Some have `role="search"` and `aria-label`, some don't
- **Tables**: Some `<table>` elements have `role="table"` + `aria-label`; some have only `id` attributes
- **Modal**: Delete confirmation modal uses `aria-labelledby` properly
- **Loading states**: Login/Confirm/Reset buttons have `data-loading` attributes with spinner + text swap

---

## 9. Empty / Dead Views

| View | Issue |
|------|-------|
| `ChecklistManagement\Index.cshtml` | **Completely empty file** (0 lines) — no content whatsoever |
| `ChecklistManagement\Create.cshtml` | Exists but may be placeholder |

---

## 10. RTL / Bilingual Support

- All views use `Localizer.T("key")` for all user-facing strings
- Arabic names shown with `dir="rtl"` attribute where `NameAr` fields exist
- Culture detection in some views (`CultureInfo.CurrentCulture.Name.StartsWith("ar")`)
- Text direction handled via CSS custom properties (likely in `app.css`)

---

## 11. Redesign Priorities

### P0 — Blocking (fix now)

| # | Issue | Location | Effort |
|---|-------|----------|--------|
| 1 | **Empty ChecklistManagement Index** | `Areas/ClinicAdmin/Views/ChecklistManagement/Index.cshtml` | Trivial |
| 2 | **3 different pagination patterns** | `_Pagination.cshtml` (root), `_Pagination.cshtml` (SuperAdmin), inline in UserManagement Index | Medium |

### P1 — Quality (fix soon)

| # | Issue | Location | Effort |
|---|-------|----------|--------|
| 3 | **Duplicate partials** — merge root ↔ area versions | `_HealthMetricCard`, `_EmptyState`, `_StatusBadge`, `_Pagination` | Small |
| 4 | **SuperAdmin hardcoded URLs** — replace with tag helpers | `Dashboard/*`, `UserManagement/*` | Small |
| 5 | **SuperAdmin inline page headers** — use shared partial or unify markup | All SuperAdmin views | Medium |
| 6 | **SuperAdmin UserManagement inline pagination** — convert to shared partial | `Areas/SuperAdmin/Views/UserManagement/Index.cshtml` | Small |

### P2 — Consistency (next sprint)

| # | Issue | Location | Effort |
|---|-------|----------|--------|
| 7 | **Inconsistent button variants** — standardize edit/view/delete buttons | All views | Medium |
| 8 | **Card class inconsistency** — unify `.card` vs `.card.card-primary` | All views | Small |
| 9 | **Inline empty states** — convert all to `_EmptyState` partial | SuperAdmin Dashboard, Clinics, UserManagement | Small |
| 10 | **Bootstrap badge vs status-pill** — pick one and migrate | All views | Medium |
| 11 | **Chart.js CDN → local fallback** | ComplianceDashboard | Small |
| 12 | **Consistent aria-label coverage** — audit all action buttons | All views | Medium |
| 13 | **Delete confirmation pattern** — unify modal vs `confirm()` | All views | Medium |
| 14 | **Search/filter form patterns** — standardize markup and aria | All list views | Medium |

### P3 — Nice to have

| # | Issue | Location | Effort |
|---|-------|----------|--------|
| 15 | **Consistent form layout** — use same column/offset pattern everywhere | All Create/Edit forms | Medium |
| 16 | **Centralize inline JS** — move recurring patterns to site.js | Settings Index, PolicyManagement Index | Medium |
| 17 | **Sidebar active state** — verify dynamic active-class logic in `_Layout` | `_Layout.cshtml` | Small |
| 18 | **CSS dead code detection** — audit unused classes in `app.css` | `app.css` | Large |

---

## 12. Summary Statistics

| Metric | Count |
|--------|-------|
| Total `.cshtml` files | 85 |
| Shared partials (root) | 8 |
| Shared partials (SuperAdmin) | 4 |
| Shared partials (ClinicAdmin) | 4 |
| CSS custom properties | ~20 |
| CSS files | 3 (app.css, site.css, bootstrap.min.css) |
| JS files | 6 (site.js, jquery, bootstrap, jquery.validate, jquery.validate.unobtrusive) |
| CDN dependencies | 1 (Chart.js 4.4.0) |
| Duplicate partials | 4 pairs |
| Pagination implementations | 3 distinct patterns |
| Empty view files | 1 (ChecklistManagement Index) |
| SuperAdmin views using hardcoded URLs | ~8 views |
