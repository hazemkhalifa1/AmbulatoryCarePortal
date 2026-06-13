# CBAHI Ambulatory Care Compliance Portal — Architecture Document

---

## 1. Executive Summary

**CBAHI Ambulatory Care Portal** is a multi-tenant compliance management platform for healthcare facilities (clinics) in Saudi Arabia. It enables clinics to manage, track, and demonstrate compliance with CBAHI (Saudi Central Board for Accreditation of Healthcare Institutions) accreditation standards.

The system manages the full lifecycle of: policy documents, KPI tracking, checklist inspections, HR/staff credentialing, forms/versioning, clinic document templates, notifications, and compliance reporting. It serves three user roles: SuperAdmin (platform-wide control), ClinicAdmin (per-clinic operations), and ClinicViewer (read-only compliance monitoring).

**Target Users:**
- **SuperAdmin**: System administrators managing clinics, users, document templates, and global settings
- **ClinicAdmin**: Clinic compliance officers managing policies, KPIs, checklists, HR, forms, and generating reports
- **ClinicViewer**: Read-only access for auditing and compliance oversight

---

## 2. Solution Architecture

### Architecture Style
**Clean Architecture** (4-layer) with **Area-based MVC** for presentation segregation. Follows **Domain-Driven Design** principles with entities, value objects, and domain enums in the core layer.

### Layers and Responsibilities

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Domain** | `AmbulatoryCarePortal.Domain` | Entities, enums, base classes (no dependencies) |
| **Application** | `AmbulatoryCarePortal.Application` | Business logic, DTOs, interfaces, validators, mapping profiles, services |
| **Infrastructure** | `AmbulatoryCarePortal.Infrastructure` | EF Core DbContext, SQL Server, migrations, repositories, UnitOfWork, encryption, email |
| **Presentation** | `AmbulatoryCarePortal.Presentation` | ASP.NET Core MVC views, controllers, areas (SuperAdmin/ClinicAdmin), middleware, filters, tag helpers |

### Design Patterns Used

| Pattern | Where |
|---------|-------|
| **Repository** | `GenericRepository<T>` wrapping EF Core DbSet |
| **Unit of Work** | `UnitOfWork` wrapping `AppDbContext` with shared transaction scope |
| **Dependency Injection** | Built-in ASP.NET Core DI, all services registered via extension methods |
| **Strategy** | Various service interfaces with single implementations |
| **Facade** | BackgroundJobService wrapping multiple services into scheduled jobs |

### Dependency Injection Setup
Four static extension classes register services:
- `InfrastructureServiceExtensions` — DbContext, UnitOfWork, GenericRepository, EncryptionService
- `ApplicationServiceExtensions` — All 20 application services + 1 hosted service
- `PresentationServiceExtensions` — HttpContextAccessor, ClinicAuthorizationFilter, TranslationService
- Identity, MVC, Session, AutoMapper, FluentValidation, Serilog in Program.cs

### Architectural Strengths
- **True Clean Architecture**: Domain has zero external dependencies; Application depends only on Domain
- **Area-based routing** cleanly separates SuperAdmin from ClinicAdmin concerns
- **Claims-based permissions** with 48 granular permissions rather than rigid role checks
- **Bilingual first-class support** with translation service in every view
- **Soft delete** globally enforced via EF Core query filters
- **Audit trail middleware** captures all non-GET requests automatically
- **Clinic-scoped data isolation** via middleware and claims

### Architectural Weaknesses
- **Domain anemic**: Entities are property bags with no domain logic or behavior
- **No event-driven architecture**: No domain events, no integration events, no pub/sub
- **No CQRS**: Read and write use same models and same DbContext
- **No caching abstraction**: In-memory ConcurrentDictionary is used directly; no IDistributedCache
- **Service layer is procedural**: Services are large classes with no separation of queries and commands
- **No background job framework**: Hangfire would be superior to the custom polling BackgroundService
- **No specification pattern**: LINQ queries scattered across services with duplication

---

## 3. Project Structure

### `AmbulatoryCarePortal.Domain`
- **Purpose**: Core domain model with zero external dependencies
- **Main responsibilities**: Entity definitions, enum definitions, base entity with audit fields
- **Dependencies**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (for IdentityUser base class)

### `AmbulatoryCarePortal.Application`
- **Purpose**: Business logic orchestration, DTOs, service interfaces, validation
- **Main responsibilities**: 20+ application services, 11 FluentValidation validators, AutoMapper profile, DTOs for all modules, service interfaces
- **Dependencies**: Domain only + AutoMapper, FluentValidation, OpenXml, Hosting/Logging abstractions

### `AmbulatoryCarePortal.Infrastructure`
- **Purpose**: Data access, external service implementations, persistence
- **Main responsibilities**: EF Core DbContext with 21 entity configurations, 4 migrations, UnitOfWork, GenericRepository, Data Protection encryption, SMTP email client, Serilog logging
- **Dependencies**: Domain, Application + EF Core/SqlServer, Identity, Serilog sinks, MailKit, DataProtection

### `AmbulatoryCarePortal.Presentation`
- **Purpose**: User interface, MVC controllers, middleware, views
- **Main responsibilities**: 15 controllers across 2 areas + 2 root controllers, 3 middleware classes, 50+ views, session management, client assets (Bootstrap 5/AdminLTE/jQuery)
- **Dependencies**: Application, Infrastructure + AutoMapper, Identity UI, Serilog.AspNetCore

---

## 4. Core Modules

### Clinic Management (SuperAdmin)
- **Purpose**: CRUD for clinics with license tracking, logo upload, activation/deactivation
- **Entities**: Clinic, Department
- **Key workflows**: Create clinic (with departments), edit clinic details, track license expiry, view clinic compliance score

### User & Role Management (SuperAdmin)
- **Purpose**: Create users, assign roles, reset passwords, view activity logs
- **Entities**: AppUser, IdentityRole
- **Key workflows**: Create user with role + password, edit user details, reset password, view login activity

### Document Templates (SuperAdmin)
- **Purpose**: Define standardized document templates per clinic type and CBAHI standard
- **Entities**: DocumentTemplate, ClinicDocument (template assignments)
- **Key workflows**: Create template, upload template file, assign to all clinics

### System Settings (SuperAdmin)
- **Purpose**: Global configuration across 6 categories (General, Mail, Branding, DocumentTemplate, Notifications, Localization)
- **Entities**: SystemSetting
- **Key workflows**: Update mail server settings (encrypted), branding, notification thresholds, localization defaults

### Policy Document Management (ClinicAdmin)
- **Purpose**: Full lifecycle of CBAHI policy documents from draft to approval
- **Entities**: PolicyDocument, EvidenceAttachment
- **Key workflows**: Create policy, upload PDF, submit for review, approve/reject, attach evidence, track expiry, export

### KPI Management (ClinicAdmin)
- **Purpose**: Track key performance indicators with periodic data entry
- **Entities**: KPI, KPIEntry
- **Key workflows**: Create KPI target, schedule frequency, enter actual values monthly, view trend analytics, compare across departments

### Checklist Inspections (ClinicAdmin)
- **Purpose**: Execute compliance checklists with pass/fail scoring
- **Entities**: ChecklistTemplate, ChecklistItem, ChecklistRound, ChecklistAnswer
- **Key workflows**: Create checklist template, execute round, upload evidence, approve round

### HR/Staff Credentialing (ClinicAdmin)
- **Purpose**: Manage healthcare staff credentials and document expiry
- **Entities**: HrStaff, HrDocument (types: ID, CV, License, Training, etc.)
- **Key workflows**: Add staff, upload credential documents, verify documents, track expiring docs, flag non-compliant staff

### Form Management (ClinicAdmin)
- **Purpose**: Version-controlled forms (e.g., consent forms, patient questionnaires)
- **Entities**: Form, FormVersion
- **Key workflows**: Create form, upload version, track version history, publish/activate

### Clinic Documents (ClinicAdmin)
- **Purpose**: Track clinic-specific documents sourced from SuperAdmin templates
- **Entities**: ClinicDocument, ClinicDocumentAttachment
- **Key workflows**: Upload required document against template, track status, attach evidence

### Notifications (ClinicAdmin)
- **Purpose**: In-app notifications for document expiry, compliance alerts, system updates
- **Entities**: Notification
- **Types**: DocumentExpiry, DocumentMissing, OpenGap, SystemUpdate, ComplianceAlert, PolicyUpdate

### Reporting & Analytics (ClinicAdmin)
- **Purpose**: Generate compliance reports (PDF/Excel/CSV/JSON) and view trends
- **Note**: PDF and Excel generation are string-based stubs — no real document generation

### Audit Trail (All)
- **Purpose**: Complete audit log of all non-GET operations
- **Entities**: AuditTrail
- **Workflows**: Automatic capture via middleware

### Compliance Calendar (ClinicAdmin)
- **Purpose**: Unified calendar view of all upcoming compliance deadlines
- **Data sources**: PolicyDocument expiry, HR document expiry, KPI due dates, Checklist due dates, ClinicDocument expiry

---

## 5. Database Analysis

### Key Relationships

| Parent | Child | Type |
|--------|-------|------|
| Clinic | Department, PolicyDocument, KPI, ChecklistTemplate, HrStaff, Form, Notification, AuditTrail, ClinicDocument, AppUser | 1:N |
| Department | PolicyDocument, KPI, HrStaff, ChecklistTemplate | 1:N |
| PolicyDocument | EvidenceAttachment | 1:N |
| KPI | KPIEntry | 1:N |
| ChecklistTemplate | ChecklistItem, ChecklistRound | 1:N |
| ChecklistRound | ChecklistAnswer | 1:N |
| Form | FormVersion | 1:N |
| HrStaff | HrDocument | 1:N |
| DocumentTemplate | ClinicDocument | 1:N |
| ClinicDocument | ClinicDocumentAttachment | 1:N |

### DbContext
- **Class**: `AppDbContext` inherits `IdentityDbContext<AppUser>`
- **DbSets**: 20 total (19 business entities + SystemSettings)
- **OnModelCreating**:
  - Applies all `IEntityTypeConfiguration` from assembly (21 config files)
  - Forces `DeleteBehavior.Restrict` on all foreign keys globally
  - Applies `HasQueryFilter(x => !x.IsDeleted)` on all entities (soft delete)
  - All enums stored as strings via `HasConversion<string>()`

### Migration Strategy
- **Tool**: `dotnet ef` with design-time factory
- **Approach**: Code-first with auto-migration on startup
- **Current**: 4 migrations (InitialCreate → DepartmentCodeString → NationalIdMaxLength → AddSystemSettingsTable)

### Potential Database Issues
1. **Soft delete + unique indexes**: Conflicts when re-creating soft-deleted records
2. **No explicit cascade deletes**: Most relationships use Restrict, risking orphan records
3. **No audit trigger for soft delete**: `IsDeleted` changes without audit trail
4. **CreatedBy/UpdatedBy as strings**: No referential integrity
5. **No database-level row-level security**: Multi-tenancy at application layer only

---

## 6. Authentication & Authorization

### Authentication
- **Mechanism**: ASP.NET Core Identity (cookie-based, not JWT)
- **User store**: `AppUser` extending `IdentityUser` with custom properties
- **Sign-in**: `PasswordSignInAsync` with lockout (5 attempts, 15 min lockout)
- **Session**: 20-minute sliding expiration, HttpOnly cookie
- **Password policy**: Minimum 8 chars, requires digit+upper+lower+non-alphanumeric
- **Missing**: Email confirmation not required, phone confirmation not required, 2FA not configured, no ReCaptcha

### Roles (3)
| Role | Access |
|------|--------|
| **SuperAdmin** | Full platform access |
| **ClinicAdmin** | All ClinicAdmin area operations |
| **ClinicViewer** | Read-only access |

### Permissions (~48 claims)
Claims-based authorization via `User.HasClaim("Permission", "...")`.

### Security Observations
1. **No authorization middleware for permission claims**: Permission claims are seeded but controllers use `[Authorize(Roles = "...")]` exclusively
2. **Connection string in plaintext**: appsettings.json contains SQL Server credentials
3. **No CORS policy configured**
4. **No rate limiting**: Login endpoint vulnerable to brute force
5. **Admin seed password hardcoded** in `DbInitializer.cs`
6. **`ClinicAccessMiddleware` parses URL routes** — fragile and bypassable
7. **`AllowedHosts: "*"`** — accepts all host headers

---

## 7. API Analysis

The application has **no dedicated REST API**. All functionality is server-rendered MVC views. Some internal JSON endpoints exist for AJAX-driven UI features.

**External integrations**: None. No external APIs are called by the system.

---

## 8. Background Processing

### Current Implementation
- **Single hosted service**: `NotificationBackgroundService` extends `BackgroundService`
- **Polling interval**: 60 minutes
- **Jobs**: Document expiry check, Compliance alerts, Checklist reminders, Weekly digest, Report generation

### Critical Gaps
- **No Hangfire/Quartz**: No job persistence, retry logic, cron scheduling, or dashboard
- **No job queue**: Jobs run sequentially in a single loop
- **No failure handling**: If one job fails, remaining jobs are skipped
- **Email sending is synchronous**: Blocking the background loop
- **Weekly digest and report generation are stubs**: Not actually implemented

---

## 9. External Integrations

### Email (SMTP)
- **Library**: `System.Net.Mail.SmtpClient` (obsolete; MailKit referenced but not used)
- **Configuration**: SMTP credentials stored in `SystemSettings` table (encrypted)
- **Gap**: Password reset email is never actually sent

### File Storage
- **Type**: Local file system under `wwwroot/uploads/`
- **Max file size**: 20 MB
- **Allowed extensions**: .pdf, .doc, .docx, .jpg, .jpeg, .png, .xlsx, .xls
- **Gap**: No blob storage, no CDN, no backup strategy

### Not Integrated
- No SMS provider, no payment gateway, no SSO, no document scanning/OCR, no cloud storage

---

## 10. Logging & Monitoring

### Logging Implementation
- **Framework**: Serilog 3.1.1
- **Sinks**: Console + Rolling File (`logs/app-.log`, daily)
- **MSSqlServer sink**: Referenced but not configured
- **Usage**: All services and controllers use `ILogger<T>`

### Error Handling
- **ExceptionMiddleware**: Catches unhandled exceptions, logs, returns JSON
- **Custom error page**: `Views/Home/Error.cshtml`

### Audit Trails
- **AuditMiddleware**: Captures all non-GET requests
- **AuditService**: Creates AuditTrail records

### Monitoring Gaps
- No APM, no health checks, no metrics, no centralized log platform, no alerting

---

## 11. Security Review

### Critical Vulnerabilities

| Issue | Severity |
|-------|----------|
| Database credentials in plaintext | **Critical** |
| Permission claims never validated on controllers | **High** |
| Admin seed password hardcoded | **High** |
| Password reset token not actually sent | **High** |
| Welcome email sends temp password in plaintext | **High** |

### Medium Issues
- No CORS policy, no rate limiting on login, no MFA, no email confirmation, `AllowedHosts: "*"`, ClinicAccessMiddleware URL parsing fragile, soft-delete + unique index conflict

### Low Issues
- CreatedBy/UpdatedBy not foreign-keyed, no audit for soft-delete, missing security headers

### OWASP Top 10 Coverage

| Category | Status |
|----------|--------|
| A01: Broken Access Control | At risk — permission claims not enforced |
| A02: Cryptographic Failures | At risk — DB creds in plaintext |
| A03: Injection | Mitigated — EF Core parameterized queries |
| A04: Insecure Design | Present — no rate limiting, no 2FA |
| A05: Security Misconfiguration | Present — `AllowedHosts: "*"` |
| A06: Vulnerable Components | Low — MailKit moderate vuln, AutoMapper high vuln |
| A07: Identification/Auth Failures | Present — no MFA, no email confirmation |
| A09: Logging/Monitoring Failures | Present — no alerting, no centralized logging |

---

## 12. Scalability Review

### Bottlenecks
1. Single SQL Server instance
2. No caching layer (only ConcurrentDictionary)
3. Synchronous email sending blocks threads
4. Polling-based background jobs scan entire tables
5. Local file system storage
6. No async bulk operations
7. Audit middleware creates DB records synchronously

### Caching Opportunities
- PolicyDocument templates (read-heavy, write-rare)
- CBAHI standards/ClinicType mappings (static)
- User permissions (per session)
- Dashboard summary counts (5-min TTL)
- System settings (already partially cached)
- Compliance calendar (15-min TTL per clinic)
- KPI lookup data (1-hour TTL)

---

## 13. Enterprise Readiness Assessment

| Category | Score | Explanation |
|----------|-------|-------------|
| **Architecture** | **6/10** | Clean Architecture structure is good, but Domain is anemic, no CQRS/Events, procedural services |
| **Security** | **4/10** | Critical credentials exposure, no MFA, no permission enforcement on controllers, no rate limiting |
| **Scalability** | **3/10** | Single DB bottleneck, no cache, sync I/O blocking, local file storage, in-process session |
| **Maintainability** | **7/10** | Clean layering, consistent patterns, but 20 services in one project is large |
| **Observability** | **3/10** | File-only logging, no APM, no health checks, no metrics, no alerting |
| **DevOps Readiness** | **4/10** | No CI/CD config, no Dockerfile, DB creds in source control |
| **Overall** | **4.5/10** | Functionally complete but significant enterprise gaps |

---

## 14. Missing Enterprise Features

### Essential
- Docker/Kubernetes support, CI/CD pipeline, REST API, Swagger/OpenAPI, health checks, centralized logging, APM, secrets management, multi-tenancy hardening, audit for soft deletes

### Security
- MFA, SSO/SAML/OIDC, rate limiting, CAPTCHA, security headers, session revocation, API key management, data-at-rest encryption, GDPR/NCA compliance

### Scalability & Performance
- Redis/Distributed Cache, read replicas, blob storage, CDN, Hangfire/Quartz, message queue, full-text search

### Operations
- Feature flags, real reporting engine, scheduled report delivery, BI integration, webhook system, tenant onboarding workflow, billing/subscription, SLA monitoring

---

## 15. Future Roadmap

### Quick Wins (0–2 months)
- P0: Move connection string to User Secrets/Environment Variables (1 day)
- P0: Add `[Authorize(Policy = "...")]` using permission claims (1 week)
- P1: Add CORS policy, CSP headers, HSTS max-age (2 days)
- P1: Remove hardcoded admin password (1 day)
- P2: Replace `SmtpClient` with `MailKit` (2 days)
- P2: Add `/health` endpoint (1 day)
- P2: Add pagination defaults and unbounded result set protection (2 days)

### Medium-Term (2–6 months)
- Redis distributed cache (2 weeks)
- Hangfire integration (3 weeks)
- REST API with Swagger (4 weeks)
- Multi-tenancy hardening (3 weeks)
- Blob storage migration (2 weeks)
- Real PDF/Excel reporting (3 weeks)
- 2FA and ReCaptcha (3 weeks)

### Enterprise-Level (6–12 months)
- CQRS + Event Sourcing (2–3 months)
- Event-Driven Architecture with message bus (2 months)
- SSO/OIDC Federation (3 weeks)
- Full OpenTelemetry + APM (1 month)
- Docker/K8s with CI/CD (1 month)
- Document Intelligence (AI-powered OCR/classification) (2 months)
- Mobile App (3 months)
- Penetration testing (2 weeks)
