# AmbulatoryCarePortal (CBAHI Portal) - Complete Architecture Analysis

> **Project:** AmbulatoryCarePortal  
> **Target Framework:** .NET 8.0  
> **Database:** SQL Server (via EF Core 8.0)  
> **Architecture:** Clean Architecture (4-layer)  
> **Last analyzed:** June 2026

---

## 1. PROJECT SUMMARY

### What is this system?

This is a **SaaS compliance management platform** for ambulatory care clinics in Saudi Arabia. It helps clinics achieve and maintain compliance with **CBAHI (Saudi Central Board for Accreditation of Healthcare Institutions)** standards.

### The problem it solves

Ambulatory care clinics (AMB, Dental) must comply with CBAHI accreditation standards across multiple domains:
- Policies & documentation
- Clinical KPIs
- Staff credentialing
- Infection control checklists
- Facility management documentation

Without this system, clinics manage compliance manually—scattered spreadsheets, paper documents, missed renewals, no centralized oversight, and no real-time compliance score.

### Main business domains

| Domain | Description |
|--------|-------------|
| **Clinic Management** | Multi-tenant clinic registration, department creation by clinic type |
| **Policy Management** | CBAHI-standard policies with versioning, approval workflow, evidence attachments |
| **KPI Tracking** | Define KPIs per department, enter monthly values, track against targets |
| **Checklist Management** | Daily/Weekly/Monthly checklists per department, execution rounds, approval |
| **HR Credentialing** | Staff records, professional document tracking, expiry monitoring, verification |
| **Document Templates** | Upload Word/PDF templates with `{{placeholder}}` substitution variables, assign to clinics |
| **Document Generation** | Fill templates with clinic data, generate DOCX/PDF with signatures and logos |
| **Compliance Scoring** | Weighted scoring engine: Policies (25%), KPIs (20%), Checklists (25%), HR (20%), Documents (10%) |
| **Reporting** | PDF/Excel/CSV report builder (Compliance, KPI, Audit, Checklist, HR) |
| **Notifications & Alerts** | Email + in-app notifications for expiries, compliance gaps, checklists due |
| **Audit Trail** | Full entity-level audit with old/new value snapshots |
| **System Settings** | Configurable weights, email SMTP, branding, localization |

### Target Users

| Role | Access Level |
|------|-------------|
| **SuperAdmin** | Full system access. Manage clinics, users, roles, system config, all clinics data |
| **ClinicAdmin** | Manage their assigned clinic: policies, KPIs, checklists, staff, documents, reports |
| **ClinicViewer** | Read-only access to their clinic's dashboard, compliance scores, reports |

### User Journey

```
Login → Dashboard (compliance score, gaps, calendar) 
  → Manage Policies (create/approve/upload evidence)
  → Enter KPI Data
  → Execute Checklists
  → Manage Staff & Documents
  → View Compliance Calendar
  → Generate Reports
SuperAdmin additionally: Create clinics, manage users, configure system, manage templates
```

---

## 2. ARCHITECTURE ANALYSIS

### Solution Structure

```
AmbulatoryCarePortal.sln
├── src/
│   ├── AmbulatoryCarePortal.Domain          (Class Library)
│   ├── AmbulatoryCarePortal.Application     (Class Library)
│   ├── AmbulatoryCarePortal.Infrastructure  (Class Library)
│   └── AmbulatoryCarePortal.Presentation    (ASP.NET Core MVC Web App)
└── tests/
    └── AmbulatoryCarePortal.Tests           (xUnit)
```

### Dependency Flow

```
Presentation → Application ← Domain
Presentation → Infrastructure → Application → Domain
                     Tests → Infrastructure, Application
```

### Layer Responsibilities

#### Domain Layer (no dependencies)
- **Entities:** 30 domain entities (all inherit `BaseEntity`)
- **Enums:** 17 enums
- **Pure domain logic** (POCOs with navigation properties)
- Dependency: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (for `IdentityUser` base)

#### Application Layer (depends on Domain)
- **Services:** 28 service implementations
- **Interfaces:** 24 interfaces + 2 repository interfaces
- **DTOs:** 17 DTO files across subfolders
- **Validators:** 11 FluentValidation validators
- **Background Jobs:** 8 Hangfire job classes
- **AutoMapper Profile:** Entity ↔ DTO mapping
- **Settings POCOs:** `DatabaseSettings`, `EmailSettings`, `FileUploadSettings`, `NotificationSettings`, `RedisSettings`, `SecuritySettings`
- **Common:** `PagedResult<T>`

#### Infrastructure Layer (depends on Application + Domain)
- **DbContext:** `AppDbContext` (IdentityDbContext<AppUser>)
- **Configurations:** 29 `IEntityTypeConfiguration<T>` files
- **Migrations:** 9 migration files
- **Repositories:** `GenericRepository<T>` + `UnitOfWork`
- **Services:** `CacheService`, `DataProtectionEncryptionService`
- **Seeds:** `RolePermissionsSeeder`, `DepartmentSeeder`, `DbInitializer`
- **Interceptors:** `AuditSaveChangesInterceptor`

#### Presentation Layer (depends on Application + Infrastructure)
- **Controllers:** 18 controllers (2 main, 12 ClinicAdmin area, 4 SuperAdmin area)
- **Middleware:** 6 middleware classes
- **Authorization:** `PermissionRequirement`, `PermissionAuthorizationHandler`, `PermissionPolicies`
- **ViewModels:** 7 view model files
- **Filters:** `ClinicAuthorizationFilter`
- **TagHelpers:** `ActiveRouteTagHelper`
- **HealthChecks:** `ApplicationHealthCheck`
- **Helpers:** `TranslationService`, `StatusBadgeHelper`, `ExpiryHelper`, `ClinicClaimsPrincipalFactory`

### Architecture Style: **Clean Architecture with MVC Presentation**

- **Domain-centric:** All business rules in Application layer services
- **Repository + Unit of Work pattern** for data access
- **AutoMapper** for object mapping
- **FluentValidation** for input validation
- **ASP.NET Core Identity** for authentication
- **Claims-based authorization** with custom permission system
- **Hangfire** for background job processing
- **Serilog** for structured logging
- **OpenTelemetry** for distributed tracing
- **Redis** for distributed caching (with memory fallback)

### Request Flow

```
HTTP Request
  → Rate Limiter Middleware
  → Exception Middleware
  → Request Logging Middleware
  → Audit Middleware (logs non-GET requests)
  → Authentication (ASP.NET Core Identity)
  → Authorization (Permission policies)
  → LogContextEnrichment Middleware
  → SecurityHeaders Middleware
  → ClinicAccess Middleware (enforces clinic isolation)
  → MVC Route → Controller Action
    → Service (Application Layer)
      → UnitOfWork.Repository<T> 
        → GenericRepository<T>
          → AppDbContext
            → SQL Server Database
    ← Response (View/Json)
  → Response
```

---

## 3. TECHNOLOGY STACK

| Component | Technology | Version |
|-----------|-----------|---------|
| **Framework** | .NET | 8.0 |
| **Language** | C# | 12 |
| **ORM** | Entity Framework Core | 8.0.0 |
| **Database** | SQL Server | (via SqlServer provider) |
| **Web Framework** | ASP.NET Core MVC | 8.0 |
| **Authentication** | ASP.NET Core Identity | 8.0.0 |
| **Validation** | FluentValidation | 11.7.1 |
| **Mapping** | AutoMapper | 15.1.1 |
| **Background Jobs** | Hangfire | 1.8.23 |
| **Logging** | Serilog | 4.3.1 (Presentation), 3.1.1 (Infrastructure) |
| **Tracing** | OpenTelemetry | 1.16.0 |
| **Caching** | StackExchange.Redis | 10.0.9 |
| **PDF/Excel** | QuestPDF (PDF), ClosedXML (Excel) | 2026.6.0, 0.105.0 |
| **Word Processing** | DocumentFormat.OpenXml | 3.1.1 |
| **Email** | MailKit | 4.17.0 |
| **UI** | Razor Pages + MVC Views | - |
| **Testing** | xUnit + Moq | 2.6.6 / 4.20.70 |
| **Encryption** | ASP.NET Core Data Protection | 8.0.0 |

---

## 4. DATABASE DEEP ANALYSIS

### Complete Entity-Relationship Model

```
Clinic (root aggregate)
├── AppUser (ASP.NET IdentityUser)
├── Department
│   ├── PolicyDocument
│   │   └── EvidenceAttachment
│   ├── KPI
│   │   └── KPIEntry
│   ├── ChecklistTemplate
│   │   ├── ChecklistItem
│   │   └── ChecklistRound
│   │       └── ChecklistAnswer
│   ├── HrStaff
│   │   └── HrDocument
│   └── ChecklistRound (also directly)
├── Form
│   └── FormVersion
├── Notification
├── AuditTrail
├── ClinicDocument
│   └── ClinicDocumentAttachment
├── ClinicTemplateAssignment
│   ├── ClinicTemplateValue
│   └── GeneratedDocument
├── ComplianceScoreSnapshot
└── ClinicSignature

DocumentTemplate (master data, cross-clinic)
├── DocumentTemplateVersion
├── TemplateVariable
├── ClinicTemplateAssignment
├── GeneratedDocument
└── TemplateSigner

SystemSetting (key-value, global configuration)
```

### Soft Delete
- **All 30 entities** implement `BaseEntity` with `IsDeleted` flag
- Global query filter `HasQueryFilter(x => !x.IsDeleted)` on every entity
- Soft delete via `GenericRepository.SoftDelete()` sets `IsDeleted = true`
- Migration files show database columns include `IsDeleted`, `CreatedAt`, `UpdatedAt`

### Audit Fields
Every entity has: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` (from `BaseEntity`)

### Key Relationships

**Clinic → Department**: One-to-Many (Clinic has Departments)  
**Clinic → AppUser**: One-to-Many (Clinic has Users)  
**Department → PolicyDocument**: One-to-Many  
**PolicyDocument → EvidenceAttachment**: One-to-Many  
**Department → KPI**: One-to-Many  
**KPI → KPIEntry**: One-to-Many  
**ChecklistTemplate → ChecklistItem**: One-to-Many  
**ChecklistTemplate → ChecklistRound**: One-to-Many  
**ChecklistRound → ChecklistAnswer**: One-to-Many  
**HrStaff → HrDocument**: One-to-Many  
**DocumentTemplate → TemplateVariable**: One-to-Many  
**DocumentTemplate → TemplateSigner**: One-to-Many  
**ClinicTemplateAssignment → ClinicTemplateValue**: One-to-Many  
**ClinicTemplateAssignment → GeneratedDocument**: One-to-Many  
**Clinic → ClinicSignature**: One-to-Many  

### Key Constraints & Indexes
- **Clinic:** Unique index on `Name` (filtered: `[IsDeleted] = 0`), Unique on `LicenseNumber`
- **Department:** Unique constraint on Code + ClinicId
- **AppUser:** IdentityUser index on Email
- **All FKs:** `DeleteBehavior.Restrict` (forced in `OnModelCreating`)

### Important DB Design Issue
The `OnModelCreating` in `AppDbContext` overrides ALL FK delete behaviors to `Restrict`, including those individually configured as `Cascade` in entity configurations. This means cascade deletes are effectively disabled across the entire system.

---

## 5. SAAS BUSINESS LOGIC

### Tenant Structure
- **Clinic is the tenant.** Each clinic is isolated by `ClinicId`.
- No formal multi-tenant DbContext filtering (no `TenantId` global query filter)
- Row-level access enforced via **application logic** and **middleware**

### SaaS Isolation
```
ClinicAccessMiddleware: Non-SuperAdmin users can only access routes 
containing their own ClinicId. Blocks 403 if mismatch.
```
- Every service method takes `clinicId` parameter
- User has `ClinicId` claim (via `ClinicClaimsPrincipalFactory`)
- SuperAdmin bypasses all clinic isolation checks

### Users & Roles
- Users created by SuperAdmin (no self-registration)
- `AppUser` extends `IdentityUser` with `ClinicId`, `FullNameEn/Ar`, `IsActive`
- 3 roles: `SuperAdmin`, `ClinicAdmin`, `ClinicViewer`

### Feature Access (by role)
See **Section 6 - Authorization** for complete permission map

### Subscription / Billing
- **NOT IMPLEMENTED.** No subscription model, no billing, no plans, no payment processing.
- The system appears to be deployed per-organization or used internally.
- Clinics are created by SuperAdmin manually.

### Limits
- No usage limits enforced in code
- No feature flags or plan-based access control
- No trial period logic

---

## 6. AUTHENTICATION & AUTHORIZATION

### Authentication Flow

```
Login (POST /Account/Login)
  → UserManager.FindByEmailAsync(email)
  → SignInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true)
    → Password validation (8+ chars, digit, upper, lower, non-alphanumeric)
    → Lockout after 5 failed attempts (15 min lockout)
    → Success: User.LastLoginAt = UTC now
    → Redirect to Home/Index (which redirects to area dashboard based on role)
```

**Additional auth flows:**
- ForgotPassword → Generate token → Email reset link via `IEmailService`
- ResetPassword → Validate token → Change password
- Email verification flow (GET/POST ConfirmEmail)
- No 2FA (SignIn.RequireConfirmedEmail = false)
- No registration (users created by SuperAdmin only)

### Identity Configuration
```csharp
Password: 8+ chars, digit, uppercase, lowercase, non-alphanumeric
Lockout: 5 attempts, 15 min lockout
Cookie: HttpOnly, SameSite=Strict, Secure=Always, 8hr sliding expiration
Session: 20 min timeout
```

### Authorization System - Permission Model

The system implements a **fine-grained claim-based permission system** on top of ASP.NET Core Identity roles:

1. **Permissions defined** as constants in `RolePermissionsSeeder.Permissions` (48 permissions)
2. **Roles mapped to permissions** in seed method: SuperAdmin (all 48), ClinicAdmin (34), ClinicViewer (11)
3. **Permissions stored as Claims** on `IdentityRole` with type `"Permission"` and value like `"policies.create"`
4. **On login**, `ClinicClaimsPrincipalFactory` loads role claims and adds them to the user's identity
5. **Authorization policies** created for each permission via `AddPermissionPolicies()`
6. **In controllers**, `[Authorize(Policy = "Permission.policies.create")]` checks claims
7. **Permission cache**: Claims cached in Redis for 30 minutes

### Complete Permission Map

| Permission | SuperAdmin | ClinicAdmin | ClinicViewer |
|-----------|:----------:|:-----------:|:------------:|
| **Clinic Management** | | | |
| `clinics.create` | ✓ | - | - |
| `clinics.read` | ✓ | ✓ | ✓ |
| `clinics.update` | ✓ | ✓ | - |
| `clinics.delete` | ✓ | - | - |
| `clinics.compliance.view` | ✓ | ✓ | ✓ |
| `clinics.export` | ✓ | ✓ | - |
| **Policies** | | | |
| `policies.create` | ✓ | ✓ | - |
| `policies.read` | ✓ | ✓ | ✓ |
| `policies.update` | ✓ | ✓ | - |
| `policies.delete` | ✓ | - | - |
| `policies.evidence.upload` | ✓ | ✓ | - |
| `policies.approve` | ✓ | ✓ | - |
| **KPIs** | | | |
| `kpis.create` | ✓ | ✓ | - |
| `kpis.read` | ✓ | ✓ | ✓ |
| `kpis.update` | ✓ | ✓ | - |
| `kpis.delete` | ✓ | - | - |
| `kpis.data.enter` | ✓ | ✓ | - |
| `kpis.export` | ✓ | ✓ | - |
| **Checklists** | | | |
| `checklists.create` | ✓ | ✓ | - |
| `checklists.read` | ✓ | ✓ | ✓ |
| `checklists.execute` | ✓ | ✓ | - |
| `checklists.approve` | ✓ | ✓ | - |
| `checklists.history.view` | ✓ | ✓ | ✓ |
| **HR** | | | |
| `staff.manage` | ✓ | ✓ | - |
| `staff.view` | ✓ | ✓ | ✓ |
| `documents.manage` | ✓ | ✓ | - |
| `documents.upload` | ✓ | ✓ | - |
| `documents.verify` | ✓ | ✓ | - |
| `documents.expiry.view` | ✓ | ✓ | - |
| **Audit** | | | |
| `audit.view` | ✓ | ✓ | ✓ |
| `audit.export` | ✓ | - | - |
| `notifications.manage` | ✓ | ✓ | - |
| `notifications.send` | ✓ | ✓ | - |
| **User Management** | | | |
| `users.manage` | ✓ | - | - |
| `users.create`, `.edit`, `.delete` | ✓ | - | - |
| `roles.manage` | ✓ | - | - |
| **Dashboard & Reports** | | | |
| `dashboard.view` | ✓ | ✓ | ✓ |
| `reports.generate` | ✓ | ✓ | - |
| `reports.export` | ✓ | ✓ | - |
| `analytics.view` | ✓ | ✓ | ✓ |
| **System Config** | | | |
| `system.configure` | ✓ | - | - |
| `system.settings.view` | ✓ | ✓ | - |
| `system.email.manage` | ✓ | - | - |
| `system.backup` | ✓ | - | - |
| **Signatures** | | | |
| `signatures.manage` | ✓ | ✓ | - |
| `signatures.view` | ✓ | ✓ | ✓ |

---

## 7. API / CONTROLLER MAP

### Main Controllers (no area)

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/` | AllowAnonymous | Redirect to dashboard or login |
| GET | `/Home/Error` | AllowAnonymous | Error page |
| GET/POST | `/Account/Login` | AllowAnonymous | Login |
| POST | `/Account/Logout` | Authenticated | Logout |
| GET/POST | `/Account/ForgotPassword` | AllowAnonymous | Password reset request |
| GET/POST | `/Account/ResetPassword` | AllowAnonymous | Password reset |
| GET/POST | `/Account/ConfirmEmail` | AllowAnonymous | Email verification |
| POST | `/Account/ResendConfirmationEmail` | Authenticated | Resend verification |
| GET | `/Account/Profile` | Authenticated | View profile |
| GET | `/Account/AccessDenied` | AllowAnonymous | Access denied page |

### Area: ClinicAdmin (all require `[Authorize]` + ClinicAccessMiddleware)

| Controller | Key Actions | Business Purpose |
|-----------|-------------|-----------------|
| **Dashboard** | Index, Policies, KPIs, Staff, ExpiringDocuments, ComplianceCalendar, ComplianceScore, ScoreHistory, UpdateComplianceScore | Main clinic dashboard with compliance overview, calendar, and score management |
| **Reporting** | Index, ComplianceReportBuilder, KPIReportBuilder, AuditReportBuilder, ChecklistReportBuilder, HRReportBuilder, EmailReport, Analytics | Generate and email reports in PDF/Excel |
| **PolicyManagement** | Index, Create, Edit, Details, UploadEvidence, Delete, Approve, Export, GetSummary | Full CRUD for CBAHI policy documents with approval workflow |
| **PolicyDocuments** | Index, Details | Unified view of policies + clinic documents |
| **Notifications** | Index, MarkRead, MarkAllRead | In-app notification management |
| **KPIManagement** | Index, ByDepartment, Create, EnterData, ViewAnalytics, Compare, Export, GetSummary | KPI definition, data entry, analytics, comparison |
| **HRManagement** | Index, Create, Edit, Details, UploadDocument, VerifyDocument, ExpiringDocuments, NonCompliantStaff, Export, GetSummary | Staff management, document upload/verify, expiry tracking |
| **Forms** | Index, Create, Versions, Delete, UploadVersion | Form management with version history |
| **DepartmentManagement** | Index, Create, Edit, Details, Delete | Department CRUD for clinic |
| **ClinicSignatures** | Index, SaveSignature, UploadSignature, DeleteSignature, Preview, DownloadWord, DownloadPdf | Manage clinic digital signatures |
| **ClinicDocuments** | Index, Details, Download, UpdateStatus | View clinic-assigned documents |
| **ChecklistManagement** | Index, Create, Execute, ViewHistory, Approve, GetAnalytics, Export | Checklist lifecycle: create → execute → approve → analytics |

### Area: SuperAdmin

| Controller | Key Actions | Business Purpose |
|-----------|-------------|-----------------|
| **Dashboard** | Index, Clinics, ClinicDetail, CreateClinic, Edit, Delete, SaveDocumentValues, SaveGlobalValues, PreviewDocument, DownloadDocumentWord, AuditLog | System dashboard with clinic CRUD, document assignment, global template values |
| **UserManagement** | Index, Create, Edit, Delete, ResetPassword, ActivityLog | User CRUD, role assignment, password reset |
| **Settings** | Index, UpdateMail, UpdateDocumentTemplate, UpdateBranding, UpdateNotifications, UpdateLocalization, UpdateGeneral, SendTestEmail | System settings by category with tabbed UI |
| **DocumentTemplates** | Index, GetStandards, Details, ExtractVariables, Create, Edit, Delete, AssignToAllClinics, UpdateVariable, DeleteVariable, RestoreVersion, Preview | Template management with variable extraction from DOCX, clinic assignment |

### Internal API Endpoints (JSON responses, typically for AJAX)
- `Dashboard/ScoreData` (GET) - Compliance score chart data
- `Dashboard/GetReportSummary` (GET) - Report summary JSON
- `PolicyManagement/GetSummary` (GET) - Policy statistics JSON
- `KPIManagement/GetSummary` (GET) - KPI statistics JSON
- `HRManagement/GetSummary` (GET) - HR statistics JSON
- `ChecklistManagement/GetAnalytics` (GET) - Checklist analytics JSON
- `DocumentTemplates/GetStandards` (GET) - Standards auto-complete

### Note: No REST API
All controllers return MVC Views or JSON for AJAX. There is no dedicated REST API for external consumption.

---

## 8. SERVICE LAYER ANALYSIS

### Core Services

| Service | Responsibility | Dependencies |
|---------|---------------|-------------|
| **ClinicService** | Clinic CRUD, department auto-creation by type, document template auto-assignment, compliance score calculation | UnitOfWork, Mapper, UserManager, IClinicTemplateAssignmentService |
| **PolicyDocumentService** | Policy CRUD with pagination, department filtering, missing/expired counts | UnitOfWork, Mapper |
| **KPIService** | KPI CRUD, KPI data entry (upsert), department filtering | UnitOfWork, Mapper |
| **ChecklistService** | Checklist template CRUD, execution rounds with answers | UnitOfWork, Mapper |
| **HrService** | Staff CRUD, document management, expiring document detection | UnitOfWork, Mapper |
| **FormService** | Form CRUD with version history | UnitOfWork, Mapper |

### Advanced Services

| Service | Responsibility | Key Methods |
|---------|---------------|-------------|
| **ComplianceScoreService** | Weighted compliance score calculation engine | `CalculateScoreAsync` (5 components), `GetDashboardAsync`, `GetScoreTrendAsync` |
| **DocumentGenerationService** | DOCX placeholder substitution, PDF generation, signature image injection | `GenerateDocxAsync`, `GeneratePdfAsync`, `PreviewDocxAsync`, `PreviewPdfAsync` |
| **DocumentTemplateService** | Template CRUD with OpenXML variable extraction | `ExtractVariablesAsync`, version management |
| **ClinicTemplateAssignmentService** | Assign templates to clinics, manage variable values | CRUD + global values |
| **ReportingService** | PDF/Excel report generation (5 report types) | `GenerateComplianceReportAsync`, `GenerateKPIReportAsync`, etc. |
| **AnalyticsService** | Dashboard metrics, compliance insights, trends | `GetDashboardMetricsAsync`, `GetComplianceInsightsAsync` |
| **AuditService** | Audit trail via Hangfire background job | `LogActionAsync`, `GetAuditTrailAsync` |
| **NotificationService** | In-app notification management | `SendNotificationAsync`, `MarkAsReadAsync` |
| **SettingsService** | System setting key-value management with caching | `GetValueAsync<T>`, `SetValueAsync`, `GetByCategoryAsync` |
| **ComplianceCalendarService** | Aggregated compliance calendar (policies, HR docs, KPIs, checklists, clinic docs) | `GetCalendarAsync`, `GetUpcomingItemsAsync` |
| **DataExportService** | Generic data export (Excel/PDF/CSV/JSON) | `ExportToExcelAsync<T>`, `ExportToPdfAsync<T>`, etc. |
| **BulkOperationsService** | Bulk operations (delete, approve, verify) | `BulkDeletePoliciesAsync`, `BulkApproveChecklistsAsync`, etc. |
| **BackgroundJobService** | Manual trigger for background jobs | `ScheduleDocumentExpiryCheckAsync`, etc. |

### Infrastructure Services

| Service | Responsibility |
|---------|---------------|
| **CacheService** | Redis-based generic cache with memory fallback |
| **DataProtectionEncryptionService** | ASP.NET Data Protection for encrypting system settings |

### Email Services
- `MailKitEmailSender` (referenced but file not found - likely missing/broken)
- `HangfireEmailService` - wraps MailKitEmailSender in Hangfire background jobs
- `EmailJob` - Hangfire job that calls `MailKitEmailSender`

### Background Jobs (Hangfire)

| Job | Schedule | Purpose |
|-----|----------|---------|
| `DocumentExpiryCheckJob` | Every 6 hours | Check expired HR documents, send email reminders |
| `ChecklistReminderJob` | Every 12 hours | Create reminders for overdue checklists |
| `ComplianceAlertJob` | Every 8 hours | Create alerts for clinics with >5 missing or >3 expired docs |
| `ComplianceScoreJob` | Every 6 hours | Recalculate compliance scores for all active clinics |
| `WeeklyDigestJob` | Mondays 8 AM | Weekly summary (not inspected in detail) |
| `AuditLogJob` | On-demand | Record audit trail entries via Hangfire |
| `EmailJob` | On-demand | Send individual/bulk emails |

### Known Issues & Duplicated Logic

1. **Duplicate compliance score calculation**: `ClinicService.CalculateComplianceScoreAsync()` is a simplified (policies-only) version. `ComplianceScoreService.CalculateScoreAsync()` is the full 5-component engine. Both update `Clinic.ComplianceScore`.

2. **MailKitEmailSender not found**: The `HangfireEmailService` references `MailKitEmailSender` but it doesn't exist anywhere in the project. Email sending will fail at runtime.

3. **Redundant IGenericRepository**: Two copies exist:
   - `Application/Interfaces/Repositories/IGenericRepository.cs` (canonical)
   - `Infrastructure/Repositories/IGenericRepository.cs` (backward compatibility stub)

4. **Redundant IUnitOfWork**: Two copies exist:
   - `Application/Interfaces/Repositories/IUnitOfWork.cs` (canonical)
   - `Infrastructure/UnitOfWork/IUnitOfWork.cs` (backward compatibility stub)

5. **Stringly-typed enums**: `AuditService.LogActionAsync` takes `string actionType` and parses it with `Enum.TryParse`. Should use strongly-typed enum.

6. **N+1 query risks**: Several service methods iterate over entities and call repositories in loops (e.g., `ComplianceScoreService`, `AuditService.GetAuditTrailAsync`)

7. **Unused/useless services**: `AdvancedNotificationService` has several no-op methods. `AnalyticsService.GetComplianceTrendsAsync` returns hardcoded data.

8. **Inconsistent validation**: Some controllers use FluentValidation, others have inline validation in POST methods.

9. **No request DTOs for many actions**: Some controllers directly bind ViewModels which are also used for display (mixed concerns).

10. **Path resolution duplication**: Both `TemplateVariableService` and `DocumentGenerationService` have identical `ResolvePath` logic.

---

## 9. FRONTEND / MVC ANALYSIS

### View Structure
- **Razor Views** with MVC pattern (no client-side SPA)
- **Areas**: `Areas/ClinicAdmin/Views/`, `Areas/SuperAdmin/Views/`
- **RTL Support**: Arabic translations via `ITranslationService`, `NameAr` fields throughout
- **UI Framework**: Bootstrap-based (implied from view models, icons, badges)

### UI Flow
```
Login → SuperAdmin Dashboard
  → Clinic list → Create/Edit Clinic
  → User Management → Create/Edit Users
  → Document Templates → Upload/Assign templates
  → System Settings → Email/Branding/Notifications
  → Audit Log

Login → ClinicAdmin Dashboard
  → Compliance Overview (score, calendar, gaps)
  → Policies → Create/Edit/Approve evidence
  → KPIs → Create/Enter data/Analytics
  → Checklists → Create/Execute/Approve
  → HR → Staff/Documents/Expiry tracking
  → Reports → Generate/Export
  → Notifications → Read/Mark read
```

### Key Frontend Patterns
- `StatusBadgeHelper` for colored status labels
- `ExpiryHelper` for expiry date formatting
- `ActiveRouteTagHelper` for active navigation
- `ITranslationService` (`T("key")`) for localization
- AJAX calls for dashboard data, search, export
- Form validation via FluentValidation + client-side

---

## 10. SECURITY AUDIT

### Authentication Security
- ✅ Password policy: 8+ chars, digit, upper, lower, non-alphanumeric
- ✅ Account lockout: 5 attempts, 15 min
- ✅ Cookie: HttpOnly, SameSite=Strict, Secure=Always
- ✅ Anti-forgery tokens on all POST forms
- ✅ Rate limiting: Login (5/min), API (100/min), Global (1000/min)
- ❌ Email confirmation NOT required (`RequireConfirmedEmail = false`)
- ❌ No 2FA
- ❌ Password reset sends email but email service may be broken (MailKitEmailSender missing)

### Authorization Security
- ✅ Fine-grained permission system with claims
- ✅ Clinic-level isolation via middleware
- ✅ SuperAdmin bypasses isolation (as intended)
- ✅ Hangfire dashboard restricted to SuperAdmin / system.configure
- ❌ `ClinicAccessMiddleware` path parsing is fragile (checks if any numeric segment matches user's clinic - could match IDs in URL not related to clinic)

### Data Security
- ✅ Sensitive settings encrypted with ASP.NET Data Protection
- ✅ Anti-forgery tokens
- ✅ Security headers middleware (X-Content-Type-Options, X-Frame-Options, CSP, etc.)
- ❌ No input sanitization on user-uploaded content

### API Exposure
- Several endpoints return JSON with potentially sensitive data
- No API key / JWT token for third-party access (not needed for this design)
- All endpoints behind authentication

---

## 11. PRODUCTION READINESS AUDIT

### ✅ Strong Areas
- **Structured logging**: Serilog with Console, File (rolling), MSSqlServer sinks
- **Distributed tracing**: OpenTelemetry (ASP.NET Core, HttpClient, EF Core)
- **Health checks**: `/health`, `/health/ready`, `/health/live` with DB check, Redis check
- **Exception handling**: Global exception middleware with proper status codes
- **CORS**: Restricted to localhost only
- **Rate limiting**: Login (5/min), API (100/min), Global (1000/min)
- **Retry policy**: SQL Server connection retry (3 attempts, 10s delay)
- **Redis caching**: Distributed cache with memory fallback
- **Background jobs**: Hangfire with SQL Server storage, 4 queues, retry policies
- **Environment-specific configs**: Development, Staging, Production with appropriate log levels
- **Request logging**: Method, path, status, duration, userId, clinicId

### ❌ Critical Issues
1. **MailKitEmailSender not found** - All email functionality broken
2. **AdminPassword not configured**: Startup throws if `AdminPassword` is missing - but its path is configurable (appsettings, env var)
3. **No database migration safety**: `MigrateAsync()` runs on every startup - dangerous for production
4. **Seed runs on every startup**: RolePermissionsSeeder, DepartmentSeeder run on every app start
5. **No distributed lock for background jobs**: Multiple instances would duplicate work
6. **Missing application lifecycle management**: No graceful shutdown handling

### ⚠️ Moderate Issues
7. **No API versioning**
8. **No Swagger/OpenAPI documentation**
9. **No integration tests** (only 4 unit tests for ClinicService)
10. **Minimal input validation in service layer** (most validation in controllers)
11. **No caching layer for report generation** (expensive queries run on every request)
12. **No pagination limits enforced on client side** (MaxPageSize=100 in GenericRepository is good but no server-side enforcement beyond that)

---

## 12. CODE QUALITY REVIEW

### Strengths
- Consistent project structure across all layers
- Clean separation of concerns (Domain → Application → Infrastructure → Presentation)
- Good use of modern C# features (records, pattern matching, nullable reference types)
- Comprehensive audit trail system (interceptor-based)
- Soft delete pattern applied consistently
- Bilingual support (Arabic/English) throughout

### Weaknesses
- **Over-abstraction**: Generic Repository + UnitOfWork adds complexity without clear benefit over direct DbContext use
- **Inconsistent pattern usage**: Some services use AutoMapper, others manually map
- **Stub files**: Redundant `IGenericRepository` and `IUnitOfWork` in Infrastructure project
- **Stub services**: `AdvancedNotificationService`, `BulkOperationsService` have incomplete implementations
- **Hardcoded values**: `AnalyticsService.GetComplianceTrendsAsync` returns hardcoded data
- **Exception handling in services**: Some services catch `Exception` broadly (e.g., `BulkOperationsService`)
- **Missing interface segregation**: `IUnitOfWork` combines repository access and save - violates Interface Segregation Principle
- **Limited test coverage**: Only 4 tests for ClinicService, zero tests for UI, infrastructure, or integration

---

## 13. RISKS & ISSUES

### High Priority
1. **Email system broken**: `MailKitEmailSender` class not found in project
2. **Startup migration in production**: `MigrateAsync()` on every startup can cause downtime, doesn't support rollback
3. **No database transaction consistency**: Multiple `SaveChangesAsync()` calls without explicit transactions
4. **No API security for internal endpoints**: JSON endpoints are accessible to any authenticated user within their role

### Medium Priority
5. **Duplicate compliance calculation logic**: Two implementations may diverge
6. **Loop-based N+1 queries**: Common pattern of FindAsync + loop = N additional queries
7. **Missing `MailKitEmailSender` dependency**: Not registered in DI
8. **No background job deduplication**: Hangfire jobs may overlap if previous run exceeds schedule interval

### Low Priority
9. **Hardcoded email templates**: HTML embedded in C# code
10. **No file upload validation**: Only extension check, no content-type verification
11. **Session state in cookie**: No distributed session store for scaled-out deployment

---

## 14. IMPROVEMENT RECOMMENDATIONS

### Critical Fixes
1. **Create `MailKitEmailSender.cs`** implementing `IEmailService` with proper SMTP configuration
2. **Move startup migration** to a manual process or use EF Core bundles for production
3. **Add distributed lock** (e.g., Redis-based) for Hangfire jobs to support multi-instance

### Architecture Improvements
4. **Consolidate compliance scoring** into single service, remove duplicate from ClinicService
5. **Remove redundant stub files** (Infrastructure IGenericRepository, IUnitOfWork)
6. **Replace GenericRepository + UnitOfWork** with direct DbContext injection in services (simpler, more testable)
7. **Add proper API layer** if third-party integration is needed

### Testing
8. **Add integration tests** with TestContainers (SQL Server)
9. **Add service-level tests** for all 28 services
10. **Add UI tests** with Playwright for critical user journeys

### Performance
11. **Add pagination parameters** to all list endpoints
12. **Cache compliance scores** with TTL instead of recalculating every 6 hours for all clinics
13. **Add projection queries** (Select only needed columns) instead of loading full entities

### Code Quality
14. **Remove unused/stub services** or implement fully
15. **Add centralized validation** with FluentValidation for all DTOs
16. **Replace magic strings** with strongly-typed enums throughout
17. **Add `CancellationToken`** support to all async methods

### Production Readiness
18. **Add health check for Hangfire** (job queue depth, failed jobs)
19. **Add structured error codes** to API responses
20. **Add Swagger/Scalar** for API documentation
21. **Add application metrics** (request rate, error rate, job duration)

---

## 15. COMPLETE FILE INDEX

### Domain Layer (30 entities + 17 enums)
```
src/AmbulatoryCarePortal.Domain/
├── Entities/           (30 files)
│   ├── BaseEntity.cs
│   ├── AppUser.cs
│   ├── Clinic.cs, Department.cs
│   ├── PolicyDocument.cs, EvidenceAttachment.cs
│   ├── KPI.cs, KPIEntry.cs
│   ├── ChecklistTemplate.cs, ChecklistItem.cs, ChecklistRound.cs, ChecklistAnswer.cs
│   ├── Form.cs, FormVersion.cs
│   ├── HrStaff.cs, HrDocument.cs
│   ├── Notification.cs, AuditTrail.cs
│   ├── DocumentTemplate.cs, DocumentTemplateVersion.cs, TemplateVariable.cs
│   ├── ClinicDocument.cs, ClinicDocumentAttachment.cs
│   ├── ClinicTemplateAssignment.cs, ClinicTemplateValue.cs, GeneratedDocument.cs
│   ├── SystemSetting.cs, ComplianceScoreSnapshot.cs
│   ├── ClinicSignature.cs, TemplateSigner.cs
├── Enums/              (17 files)
│   ├── AuditActionType.cs, ChecklistAnswer.cs, ChecklistSchedule.cs
│   ├── ClinicDocumentStatus.cs, ClinicType.cs, ComplianceItemSeverity.cs
│   ├── ComplianceItemType.cs, DepartmentCodeEnum.cs, DocumentStatus.cs
│   ├── HrDocumentType.cs, KPIFrequency.cs, NotificationType.cs
│   ├── SettingCategory.cs, SettingValueType.cs, SignatureType.cs
│   ├── StaffType.cs, UserRole.cs
```

### Application Layer (28 services + 24 interfaces + 17 DTOs + 8 jobs)
```
src/AmbulatoryCarePortal.Application/
├── BackgroundJobs/     (8 files)
│   ├── AuditLogJob.cs, ChecklistReminderJob.cs, ComplianceAlertJob.cs
│   ├── ComplianceScoreJob.cs, DocumentExpiryCheckJob.cs, EmailJob.cs
│   ├── HangfireConfiguration.cs, WeeklyDigestJob.cs
├── Common/             (1 file) PagedResult.cs
├── DependencyInjection/(1 file) ApplicationServiceExtensions.cs
├── DTOs/               (17 files)
│   ├── Analytics/ (AnalyticsDtos.cs, ComplianceScoreDto.cs)
│   ├── AuditTrail/ (AuditTrailDtos.cs)
│   ├── Checklist/ (ChecklistDtos.cs)
│   ├── Clinic/ (ClinicDtos.cs)
│   ├── Department/ (DepartmentDtos.cs)
│   ├── Document/ (DocumentDtos.cs, TemplateVariableDtos.cs, SignatureDtos.cs)
│   ├── Form/ (FormDtos.cs)
│   ├── HR/ (HrDtos.cs)
│   ├── KPI/ (KpiDtos.cs)
│   ├── Notification/ (NotificationDtos.cs)
│   ├── PolicyDocument/ (PolicyDocumentDtos.cs)
│   ├── ComplianceCalendarItemDto.cs, SystemSettingDto.cs, MailSettingsDto.cs
├── Interfaces/         (26 files incl. repositories)
│   ├── Repositories/ (IGenericRepository.cs, IUnitOfWork.cs)
│   ├── I*Service.cs (24 service interfaces)
├── Mappings/           (1 file) MappingProfile.cs
├── Services/           (28 files) All service implementations
├── Settings/           (6 files) DatabaseSettings, EmailSettings, FileUploadSettings, NotificationSettings, RedisSettings, SecuritySettings
├── Validators/         (11 files) FluentValidation validators
```

### Infrastructure Layer (29 configurations + 9 migrations + 2 repositories + 3 seeds)
```
src/AmbulatoryCarePortal.Infrastructure/
├── Data/
│   ├── Configurations/ (29 files) IEntityTypeConfiguration for all entities
│   ├── Migrations/     (2 migration sets, 9 files total)
│   ├── Seed/           (3 files) DbInitializer, DepartmentSeeder, RolePermissionsSeeder
│   ├── AppDbContext.cs
│   ├── AppDbContextDesignTimeFactory.cs
│   └── AuditSaveChangesInterceptor.cs
├── DependencyInjection/(1 file) InfrastructureServiceExtensions.cs
├── Repositories/       (2 files) GenericRepository.cs, IGenericRepository.cs (stub)
├── Services/           (2 files) CacheService.cs, DataProtectionEncryptionService.cs
├── UnitOfWork/         (2 files) UnitOfWork.cs, IUnitOfWork.cs (stub)
```

### Presentation Layer (18 controllers + 6 middleware + 6 DI extensions)
```
src/AmbulatoryCarePortal.Presentation/
├── Areas/
│   ├── ClinicAdmin/Controllers/ (12 controllers)
│   └── SuperAdmin/Controllers/  (4 controllers)
├── Authorization/     (3 files) PermissionRequirement, PermissionPolicies, PermissionAuthorizationHandler
├── Controllers/       (2 files) HomeController, AccountController
├── DependencyInjection/(6 files) PresentationServiceExtensions, IdentityServiceExtensions, CachingServiceExtensions, HangfireServiceExtensions, ObservabilityServiceExtensions
├── Extensions/        (1 file)
├── Filters/           (1 file) ClinicAuthorizationFilter
├── HealthChecks/      (1 file) ApplicationHealthCheck
├── Helpers/           (5 files) TranslationService, StatusBadgeHelper, ExpiryHelper, ClinicClaimsPrincipalFactory
├── Middleware/        (6 files) ExceptionMiddleware, RequestLoggingMiddleware, AuditMiddleware, LogContextEnrichmentMiddleware, SecurityHeadersMiddleware, ClinicAccessMiddleware
├── TagHelpers/        (1 file) ActiveRouteTagHelper
├── ViewModels/        (7 files) AccountViewModels, ComplianceDashboardViewModels, ComplianceScoreViewModels, LoginViewModel, RoleManagementViewModels, SettingsViewModels, SuperAdminDashboardViewModels
├── Program.cs
└── appsettings*.json (5 files)
```

### Tests (4 unit tests)
```
tests/AmbulatoryCarePortal.Tests/
├── ClinicServiceTests.cs (4 test methods)
└── AmbulatoryCarePortal.Tests.csproj
```
