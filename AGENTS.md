# AmbulatoryCarePortal — Anchored Summary

## Session 1: Full Codebase Exploration & Baseline

### Architecture
- Clean Architecture: `Domain` → `Application` → `Infrastructure` → `Presentation` (MVC)
- Identity + EF Core + AutoMapper + FluentValidation
- Build: 0 errors, 115 warnings (style/analyzers)

### Auth System
- 3 roles: SuperAdmin, ClinicAdmin, ClinicViewer
- Policy-based: `[Authorize(Policy = "Permission.{resource}.{action}")]`
- ~40 policies in `PermissionPolicies.cs`, claims-based handler (`PermissionAuthorizationHandler`)
- `RolePermissionsSeeder` defines all role→permission mappings

### Controllers (16 total)
- SuperAdmin (1): `AccountController`
- ClinicAdmin (7): Dashboard, PolicyManagement, ClinicManagement, KPI, Compliance, Notifications, Reporting
- Shared (6): Document, Signature, AuditLog, Appointment, Alert, User
- ClinicViewer (1): Dashboard
- Plus `AccountController` (login/register)

### Translation
- Custom `TranslationService` (JSON files in `Resources/`, no `.resx`)
- Keys: `"Page.*"`, `"Alert.*"`, `"Button.*"`, `"Form.*"`, `"Table.*"`
- RTL via cookie `"lang" = "ar"`, `_RTLStyles.cshtml` partial

### UI Audit (UI_AUDIT_REPORT.md)
- 18 findings: 4 duplicate partials, 3 pagination implementations, 1 empty view, hardcoded URLs, inconsistent CSS (5+ conventions)
- Settings: SPA-like page, 6 tabs, duplicated validation logic
- Chart.js: only on ComplianceDashboard

### Middleware Pipeline (14 middlewares, ordered)
1. ExceptionHandler → 2. HSTS → 3. HTTPS → 4. StaticFiles → 5. DeveloperExceptionPage → 6. Identity/Init → 7. Culture → 8. Session → 9. Routing → 10. Authentication → 11. Authorization → 12. Antiforgery → 13. Endpoints → 14. ErrorPages
- DB init runs after endpoints in `RunAsync` via `InitializeDatabaseAsync`

### Key Files
- `Program.cs` — full pipeline + service registration
- `PermissionPolicies.cs` — all policy registrations
- `RolePermissionsSeeder.cs` — role→permission seed data
- `TranslationService.cs` — JSON-based i18n
- `UI_AUDIT_REPORT.md` — complete UI audit with findings
