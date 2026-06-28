# AmbulatoryCarePortal — Anchored Summary

## Session 1: Full Codebase Exploration & Baseline

### Architecture
- Clean Architecture: `Domain` → `Application` → `Infrastructure` → `Presentation` (MVC)
- Identity + EF Core + AutoMapper + FluentValidation
- Build: 0 errors, ~115 warnings (style/analyzers)

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

### Phases Completed
- Phase 0: Baseline codebase exploration
- Phase 1: UI Foundation — `app.css`, partials, design tokens
- Phase 2A: Layout Infrastructure — Sidebar, Topbar, Breadcrumb, ActiveRouteTagHelper, JS
- Phase 2B: Layout Stabilization — A11Y sync, footer, breadcrumb overflow, section numbering
- Phase 3: Authentication UI Modernization — see `PHASE_3_AUTH_REPORT.md`
- Phase A.1: Assignment sync bug fix — `ClinicController.cs:317` changed to use `!Contains(DepartmentCategory)` instead of `Contains(StandardCode)`
- Phase 4A: Analysis — ClinicDocument/PolicyDocument removal feasibility (`PHASE_4A_POLICY_REMOVAL_ANALYSIS.md`)
- Phase 4B.1: Migrate 3 services from `Repository<ClinicDocument>` to `Repository<ClinicTemplateAssignment>` (ClinicService, ComplianceScoreService, ComplianceCalendarService)
- Phase 4C: Disconnect PolicyDocumentsController from IClinicDocumentService → uses IClinicTemplateAssignmentService
- Phase 4B.2: Delete dead ClinicDocument code — removed IClinicDocumentService, ClinicDocumentService, 3 DTOs, 3 mappings, 1 DI registration (~300 lines)
- Phase 4D: Rename ClinicDocumentsController → ClinicTemplatesController, rename view folder, update all nav/redirect/translation references
- Phase 5: Backend PolicyDocument migration — migrated 3 files (ClinicService, ComplianceAlertJob, WeeklyDigestJob) from PolicyDocument to ClinicTemplateAssignment. 12 remaining consumers blocked/deferred (see `PHASE_5_BACKEND_POLICY_MIGRATION.md`)
- Phase 5.1: Deep backend audit — zero-trust re-evaluation of all 12 blocked consumers. Corrected 2 false BLOCKEDs: migrated ComplianceScoreService.GetDashboardAsync (missing-attachment count) and DataExportService.ExportPoliciesAsync (policy export via DocumentTemplate nav). Confirmed 5 true blocks, 5 UI controllers out of scope. Build: 0 errors. (see `PHASE_5_1_DEEP_BACKEND_AUDIT.md`)

### Phase 3 — Auth UI Modernization
- Extracted 4x duplicated brand panels into `_AuthBrandPanel.cshtml` partial (tackles UI audit finding #1)
- Glass-morphism form cards with `backdrop-filter: blur()`
- Unified status page system (`.auth-status`) with icon pop animations
- Password strength indicator on ResetPassword with real-time scoring
- Keyboard-accessible skip link in login layout
- `prefers-reduced-motion` support throughout
- Fixed hardcoded URLs in AccessDenied and Profile (asp tag helpers)
- Profile page redesigned with modern card layout (`.profile-card`)

### Key Architecture Milestones
- **ClinicDocument** — fully disconnected from runtime (0 runtime consumers), entity still exists as schema-only (deferred to later DB cleanup)
- **PolicyDocument** — 15 total consumers: 3 migrated, 7 blocked (no direct ClinicTemplateAssignment equivalent), 5 UI controllers out of scope
- UI naming standardized to "Clinic Templates" to match underlying ClinicTemplateAssignment entity
- Build baseline: 0 errors, ~106-122 pre-existing nullability warnings

### Key Files
- `Program.cs` — full pipeline + service registration
- `PermissionPolicies.cs` — all policy registrations
- `RolePermissionsSeeder.cs` — role→permission seed data
- `TranslationService.cs` — JSON-based i18n
- `UI_AUDIT_REPORT.md` — complete UI audit with findings
- `PHASE_3_AUTH_REPORT.md` — Phase 3 auth modernization report
- `PHASE_4A_POLICY_REMOVAL_ANALYSIS.md` — ClinicDocument/PolicyDocument removal feasibility
- `PHASE_5_BACKEND_POLICY_MIGRATION.md` — Phase 5 backend migration report
- `PHASE_7_POLICY_MODULE_REMOVAL.md` — Phase 7 complete Policy module removal report
- `PHASE_8_FINAL_CLEANUP.md` — Phase 8 final dead translation key cleanup

### Phase 7 — Complete Policy Module Removal
- Deleted PolicyManagementController, PolicyDocumentsController, all Policy views, PolicyDocumentService, IPolicyDocumentService, PolicyDocumentDtos, policy ViewModels/Validators
- Deleted PolicyDocument entity, EvidenceAttachment entity, DocumentStatus enum
- Deleted PolicyDocumentConfiguration, EvidenceAttachmentConfiguration
- Removed DbSet and query filters from AppDbContext
- Removed nav properties from AppUser, Clinic, Department
- Removed EF configurations from Clinic/Department/AppUser configs
- Removed AutoMapper mappings and DI registration
- Removed PolicyDocument usage from: ClinicAdmin DashboardController, SuperAdmin DashboardController, DepartmentManagementController, AnalyticsService, ReportingService, AdvancedSearchService, BulkOperationsService, ComplianceCalendarService
- Migrated ComplianceScoreService.CalculatePolicyScoreAsync to ClinicTemplateAssignment
- Updated StatusBadgeHelper, _ViewImports, Department views, ClinicTemplates view
- Phase 8: Remaining dead translation keys and fallback strings removed from TranslationService.cs and JSON files
- Remaining: historical EF migrations only (untouched)
- Build: 0 errors, 0 warnings
