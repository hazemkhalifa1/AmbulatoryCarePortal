# CBAHI Ambulatory Care Portal — Implementation Plan

---

## Phase 0: Foundation & Security Hardening (Weeks 1–2)

**Goal**: Eliminate critical security vulnerabilities and stabilize the startup sequence.

### 0.1 Secrets Remediation
| Task | Effort | Details |
|------|--------|---------|
| Move connection string to User Secrets / Environment Variables | 4h | Remove from `appsettings.json`; use `dotnet user-secrets` for dev, env vars for production |
| Remove hardcoded admin password | 2h | Generate random password on first deploy; store in Key Vault / env var |
| Remove plaintext SMTP credentials from appsettings | 1h | SystemSettings table already handles SMTP at runtime; remove fallback defaults |
| Add `.gitignore` for `appsettings.*.json` secrets patterns | 1h | Ensure development/user secrets never committed |

### 0.2 Startup Fix
| Task | Effort | Details |
|------|--------|---------|
| Fix `DbInitializer.InitializeAsync` crash | 8h | Move `MigrateAsync()` before any service resolution that queries unfinalized tables. Or split startup into: (1) migrate, (2) seed, (3) bootstrap app |
| Make `SettingsService` resilient to missing table | 4h | Return defaults when `SystemSettings` table doesn't exist yet (startup sequence) |
| Add startup health check after migration | 2h | Verify all expected tables exist before app signals ready |

### 0.3 Permission Enforcement
| Task | Effort | Details |
|------|--------|---------|
| Create authorization policies matching seeded permissions | 1d | Map each `Permission` constant to a policy name + requirement |
| Apply `[Authorize(Policy = "...")]` to all controller actions | 2d | Currently only `[Authorize(Roles = "...")]` is used; permissions are view-checked only |
| Add authorization failure audit logging | 4h | Log permission-denied attempts with user, IP, target |

### 0.4 Anti-CSRF & Security Headers
| Task | Effort | Details |
|------|--------|---------|
| Add CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy via middleware | 4h | Security headers middleware before static files |
| Configure HSTS with `max-age=31536000` and `includeSubDomains` | 1h | Currently HSTS is only enabled with defaults |
| Add `SameSite=Strict` and `Secure` to session cookie | 1h | Session cookie currently only has `HttpOnly=true` |
| Set `AllowedHosts` to specific domain(s) | 1h | Currently `"*"` |

---

## Phase 1: Observability (Weeks 3–4)

**Goal**: Enable monitoring, health checks, and centralized logging.

### 1.1 Health Checks
| Task | Effort | Details |
|------|--------|---------|
| Add `Microsoft.AspNetCore.Diagnostics.HealthChecks` | 4h | `/health`, `/health/ready`, `/health/live` endpoints |
| Add database connectivity health check | 2h | Ping SQL Server via EF Core |
| Add storage health check | 2h | Verify `wwwroot/uploads/` is writable |
| Add SMTP health check | 2h | Verify SMTP connection (without sending) |

### 1.2 Logging Improvements
| Task | Effort | Details |
|------|--------|---------|
| Configure Serilog MSSqlServer sink | 4h | Log to `Logs` table for centralized querying |
| Add structured logging middleware | 4h | Log request method, path, status code, duration, user, clinic ID |
| Add error event IDs for alerting | 2h | Categorize errors by subsystem for easier filtering |
| Remove ILogger from middleware (use diagnostic source) | 2h | ExceptionMiddleware already logs; AuditMiddleware duplicates |

### 1.3 Audit Trail Enhancements
| Task | Effort | Details |
|------|--------|---------|
| Move audit logging to fire-and-forget (Hangfire or Channel) | 1d | Currently synchronous per-request blocking |
| Capture soft-delete events | 4h | `IsDeleted=true` changes currently bypass audit |
| Add entity change tracking for OldValues/NewValues | 2d | Currently OldValues/NewValues are empty |

---

## Phase 2: Background Jobs & Async Processing (Weeks 4–6)

**Goal**: Eliminate poll-based blocking background service with proper job framework.

### 2.1 Hangfire Integration
| Task | Effort | Details |
|------|--------|---------|
| Add Hangfire packages (core, SqlServer, dashboard) | 2h | Replace custom `BackgroundService` |
| Create Hangfire `Startup` configuration with SQL Server storage | 4h | Recurring jobs + fire-and-forget queues |
| Configure Hangfire dashboard (restrict to SuperAdmin) | 2h | `/hangfire` route with authorization |
| Migrate `BackgroundJobService` methods to Hangfire jobs | 2d | Each `Schedule*` method becomes a recurring job with cron expression |

### 2.2 Async Email Queue
| Task | Effort | Details |
|------|--------|---------|
| Create Hangfire fire-and-forget email job | 4h | `EmailService` methods become queued jobs |
| Replace `SmtpClient` with `MailKit` (already in packages) | 1d | `SmtpClient` is obsolete; MailKit is already referenced |
| Add email retry policy (3 attempts, exponential backoff) | 4h | Hangfire automatic retries |

### 2.3 Scheduled Report Generation
| Task | Effort | Details |
|------|--------|---------|
| Implement actual PDF generation (QuestPDF) | 3d | Replace string-based stub with real PDF |
| Implement actual Excel generation (ClosedXML) | 2d | Replace string-based stub with real Excel |
| Wire weekly digest with real data aggregator | 1d | Currently logs only |

---

## Phase 3: Caching & Performance (Weeks 6–7)

**Goal**: Reduce database load and improve response times.

### 3.1 Distributed Cache
| Task | Effort | Details |
|------|--------|---------|
| Add `IDistributedCache` with Redis provider | 4h | NuGet: `Microsoft.Extensions.Caching.StackExchangeRedis` |
| Create `CacheService` abstraction with TTL groups | 1d | Static data (standards, departments), reference data, user data |
| Add cache-aside pattern to all Application services | 3d | Cache-first with DB fallback |
| Replace `SettingsService.ConcurrentDictionary` with `IDistributedCache` | 4h | Current per-instance cache doesn't scale |

### 3.2 Query Optimization
| Task | Effort | Details |
|------|--------|---------|
| Audit N+1 query patterns across all services | 2d | Use AutoMapper `.ProjectTo()` instead of `.ToList()` + loop mapping |
| Add pagination defaults and max page size enforcement | 1d | Prevent unbounded result sets |
| Add composite indexes based on query patterns | 2d | Review EF Core generated queries for missing indexes |
| Convert background job table scans to date-range queries | 1d | Currently scans entire tables |

### 3.3 Session & State Management
| Task | Effort | Details |
|------|--------|---------|
| Replace in-memory session with Redis-backed session | 4h | `AddStackExchangeRedisCache` + `AddSession` configured for Redis |
| Cache user permissions in distributed cache | 1d | Avoid permission re-query on every request |

---

## Phase 4: Multi-Tenancy Hardening (Weeks 7–8)

**Goal**: Ensure tenant isolation is bulletproof, not route-parsing-fragile.

### 4.1 Tenant Isolation
| Task | Effort | Details |
|------|--------|---------|
| Replace `ClinicAccessMiddleware` URL parsing with tenant context | 2d | Use `IClinicContext` injected into services, not URL regex |
| Add `ClinicId` to every entity query automatically | 3d | Global query filter per entity where applicable |
| Add tenant ID to log context (Serilog enricher) | 4h | Every log line includes `ClinicId` context |
| Remove `ClinicAuthorizationFilter` (unused/partial) or complete it | 1d | Currently registered but not globally applied |

### 4.2 Soft-Delete Fix
| Task | Effort | Details |
|------|--------|---------|
| Add `IsDeleted` to unique indexes globally | 2d | Include `WHERE IsDeleted = 0` filter in unique indexes to prevent conflict |
| Add audit trigger for soft-delete | 4h | Currently `IsDeleted=true` has no audit trail |
| Add soft-delete restore endpoint/service | 1d | Currently no way to undo a deletion |

---

## Phase 5: Real Reporting & Export (Weeks 8–10)

**Goal**: Functioning PDF/Excel/CSV generation, not stubs.

### 5.1 Reporting Engine
| Task | Effort | Details |
|------|--------|---------|
| Integrate QuestPDF for compliance reports | 3d | Real PDF with tables, charts, branding |
| Integrate ClosedXML for Excel reports | 2d | Real .xlsx with formatting, multiple sheets |
| Integrate CsvHelper for CSV exports | 1d | Proper CSV with encoding, escaping |
| Add report template system | 3d | Configurable report sections per clinic type |

### 5.2 Scheduled Report Delivery
| Task | Effort | Details |
|------|--------|---------|
| Implement weekly/daily report generation via Hangfire | 2d | Auto-generate + email as attachment |
| Add report archive (blob storage) | 2d | Store generated reports for later retrieval |

---

## Phase 6: API Layer & External Access (Weeks 10–12)

**Goal**: Enable mobile apps and third-party integrations.

### 6.1 REST API
| Task | Effort | Details |
|------|--------|---------|
| Create `AmbulatoryCarePortal.Api` project (or area) | 4h | Separate Web API project referencing Application + Infrastructure |
| Add JWT Bearer authentication | 2d | JWT alongside existing cookie auth for API consumers |
| Add Swagger/OpenAPI with `Swashbuckle` | 1d | `/swagger` with OAuth2/JWT configuration |
| Implement API versioning | 1d | URL or header-based versioning |
| Add API rate limiting (`UseRateLimiter`) | 1d | Per-client-ID throttling |
| Create API endpoints for: ClinicAdmin read operations, KPI data entry, checklist execution, document upload | 1 week | Prioritize mobile-relevant operations |

### 6.2 Webhook System
| Task | Effort | Details |
|------|--------|---------|
| Create webhook registration/store | 2d | Per-clinic webhook URLs + event type subscriptions |
| Implement webhook delivery via Hangfire | 2d | Fire-and-forget delivery with retry |
| Add security headers (HMAC signature) | 1d | Outgoing webhook verification |

---

## Phase 7: Enterprise Authentication (Weeks 12–14)

**Goal**: Production-grade authentication.

### 7.1 MFA & Account Security
| Task | Effort | Details |
|------|--------|---------|
| Enable 2FA with authenticator app | 2d | QR code-based TOTP (built into ASP.NET Identity) |
| Add ReCaptcha v3 to login and forgot-password | 1d | Invisible captcha scoring |
| Require email confirmation for new users | 1d | Wire existing Identity email confirmation flow |
| Add session revocation on password change | 1d | Regenerate security stamp on password change |
| Add concurrent session limit | 2d | Track active sessions; limit per user |

### 7.2 SSO/SAML
| Task | Effort | Details |
|------|--------|---------|
| Add Azure AD / OpenID Connect integration | 1 week | `Microsoft.AspNetCore.Authentication.OpenIdConnect` |
| Add SAML2 integration for legacy IdPs (Sustainsys.Saml2) | 1 week | Alternative for non-Azure customers |
| Add identity provider switching per clinic | 2d | Per-tenant IdP configuration |

---

## Phase 8: Infrastructure & DevOps (Weeks 14–16)

**Goal**: Production deployment readiness.

### 8.1 Containerization
| Task | Effort | Details |
|------|--------|---------|
| Create Dockerfile for presentation layer | 1d | Multi-stage build with ASP.NET 8 runtime |
| Create docker-compose with SQL Server + Redis | 1d | Local dev environment parity |
| Add `.dockerignore` | 1h | Exclude obj/bin/node_modules |

### 8.2 CI/CD
| Task | Effort | Details |
|------|--------|---------|
| Create GitHub Actions / Azure Pipeline | 2d | Build → Test → Deploy stages |
| Add database migration step to pipeline | 1d | `dotnet ef database update` in deployment |
| Add container registry push step | 1d | Docker Hub / ACR / ECR |
| Add Slack/Teams deployment notifications | 2h | Webhook integration |

### 8.3 Infrastructure-as-Code
| Task | Effort | Details |
|------|--------|---------|
| Create ARM / Bicep / Terraform templates | 1 week | Azure App Service, SQL Database, Redis Cache, Storage Account, Key Vault |
| Add environment-specific variable management | 2d | Per-environment appsettings with Key Vault references |

---

## Phase 9: File Storage Modernization (Weeks 16–17)

**Goal**: Scalable, durable, geo-redundant file storage.

### 9.1 Blob Migration
| Task | Effort | Details |
|------|--------|---------|
| Create `IFileStorageService` abstraction | 1d | Interface for local/blob/CDN storage |
| Implement Azure Blob Storage provider | 3d | SAS tokens, container-per-clinic isolation |
| Implement AWS S3 provider | 2d | Alternative provider |
| Migrate existing `wwwroot/uploads/` files | 2d | Background migration job via Hangfire |
| Update all upload controllers to use `IFileStorageService` | 2d | Replace `IFormFile → wwwroot` with `IFormFile → Blob` |

### 9.2 CDN
| Task | Effort | Details |
|------|--------|---------|
| Add Azure CDN / CloudFront in front of blob storage | 1d | Cache static files at edge |
| Implement file URL versioning for cache busting | 4h | Append hash to file names |

---

## Phase 10: Advanced Features (Weeks 17–20)

**Goal**: Differentiated enterprise capabilities.

### 10.1 Document Intelligence
| Task | Effort | Details |
|------|--------|---------|
| Integrate Azure Form Recognizer / Document Intelligence | 1 week | Automatic expiry date extraction, document classification |
| Add OCR for scanned document processing | 1 week | Extract text from uploaded PDFs/images |

### 10.2 Advanced Search
| Task | Effort | Details |
|------|--------|---------|
| Add Azure Cognitive Search / Elasticsearch | 2 weeks | Full-text search across all documents, policies, HR records |
| Add faceted filtering and relevance tuning | 1 week | Search results with drill-down filters |

### 10.3 BI Integration
| Task | Effort | Details |
|------|--------|---------|
| Add Power BI Embedded integration | 2 weeks | Embeddable compliance dashboards |
| Add data export to OData feed | 1 week | Enable external BI tools to query compliance data |

---

## Phase 11: Internationalization & Accessibility (Weeks 20–21)

**Goal**: Complete RTL support and accessibility compliance.

### 11.1 RTL & i18n
| Task | Effort | Details |
|------|--------|---------|
| Audit all views for RTL correctness | 1 week | Bootstrap 5 RTL support, flip margins/paddings |
| Add language persistence to user profile | 1d | Currently cookie-based only, no DB preference |
| Add ICU message format for pluralization/gender | 2d | `IStringLocalizer` with resource files |

### 11.2 Accessibility
| Task | Effort | Details |
|------|--------|---------|
| Add ARIA labels to all forms and interactive elements | 1 week | WCAG 2.1 AA compliance |
| Add keyboard navigation support | 3d | Tab order, skip links, focus management |
| Add high-contrast theme support | 2d | CSS media query `prefers-contrast: more` |

---

## Phase 12: Data Retention & Compliance (Weeks 21–22)

**Goal**: GDPR/NCA data lifecycle management.

### 12.1 Data Lifecycle
| Task | Effort | Details |
|------|--------|---------|
| Implement data retention policies per entity | 2d | Configurable retention periods (audit logs: 1yr, notifications: 90d, etc.) |
| Implement data purge background job (Hangfire) | 2d | Soft-delete → hard-delete after retention period |
| Implement data export for regulatory requests | 2d | User data export in machine-readable format |

### 12.2 Consent Management
| Task | Effort | Details |
|------|--------|---------|
| Add consent tracking for users | 2d | Record consent grants with timestamps and versions |
| Add privacy policy versioning | 1d | Track which policy version user accepted |

---

## Summary: Timeline & Effort

| Phase | Duration | Total Effort | Risk Level | Business Value |
|-------|----------|-------------|-------------|---------------|
| **0 — Security Hardening** | 2 weeks | 6 days | **Critical** | **Immediate** — prevents data breach |
| **1 — Observability** | 2 weeks | 5 days | Low | High — enables ops |
| **2 — Background Jobs** | 3 weeks | 8 days | Medium | High — reliability |
| **3 — Caching & Performance** | 2 weeks | 8 days | Medium | High — UX |
| **4 — Multi-Tenancy** | 2 weeks | 7 days | **High** | **Immediate** — data isolation |
| **5 — Real Reporting** | 3 weeks | 11 days | Low | High — core feature |
| **6 — API Layer** | 3 weeks | 12 days | Medium | Medium — extensibility |
| **7 — Enterprise Auth** | 3 weeks | 9 days | Medium | Medium |
| **8 — DevOps** | 3 weeks | 10 days | Medium | High — deployability |
| **9 — File Storage** | 2 weeks | 9 days | Medium | Medium — scalability |
| **10 — Advanced Features** | 4 weeks | 20 days | **High** | Medium — differentiation |
| **11 — i18n & A11y** | 2 weeks | 10 days | Low | Medium |
| **12 — Data Retention** | 2 weeks | 6 days | Low | Medium — compliance |
| **Total** | **~29 weeks** | **~111 engineering days** | | |

### Recommended Phasing for Delivery

| Release | Contains | Timeline |
|---------|----------|----------|
| **v2.1 — Security Patch** | Phase 0 (all) + Phase 4.2 (soft-delete fix) | Week 2 |
| **v2.2 — Ops Ready** | Phase 1 (observability) + Phase 2 (Hangfire/email) | Week 6 |
| **v2.3 — Performance** | Phase 3 (caching) + Phase 8 (containerization) | Week 10 |
| **v2.4 — Enterprise** | Phase 5 (reporting) + Phase 6 (API) + Phase 7 (auth) | Week 16 |
| **v2.5 — Advanced** | Phase 9 (blob) + Phase 10 (AI search) + Phase 11–12 | Week 22 |
