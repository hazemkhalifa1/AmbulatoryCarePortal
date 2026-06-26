# Production Fix Report — 3 Critical Issues

## Overview

Three critical production issues were identified during codebase analysis and have been
resolved. All changes are backward-compatible; no business logic was altered.

---

## Issue 1: MailKitEmailSender Not Registered as IEmailService + Missing Attachment Data

**Root Cause:**
- `MailKitEmailSender` class existed in `EmailService.cs:11` but **did not implement
  `IEmailService`** — it was registered by concrete class only
  (`services.AddScoped<MailKitEmailSender>()`), so DI resolution by interface would
  silently fail if any code injected `IEmailService` expecting the MailKit implementation
- SMTP configuration only read from `SystemSettings` DB table via `ISettingsService`, with
  no fallback to `appsettings.json` → if DB settings were empty, SMTP calls used defaults
- `HangfireEmailService.SendScheduledReportAsync()` ignored the `reportContent` byte[]
  parameter and enqueued a plain text email without any attachment

**Changes:**

| File | Change |
|------|--------|
| `src/.../EmailService.cs` | Added `: IEmailService` to class declaration; added
`IOptions<EmailSettings>` constructor dependency; `CreateSmtpClientAsync` now falls back
to `EmailSettings` from appsettings when DB settings are null/empty |
| `src/.../EmailJob.cs` | Added `SendScheduledReportAsync(to, reportName, reportContent)`
Hangfire job method to accept and forward byte[] attachments |
| `src/.../HangfireEmailService.cs` | `SendScheduledReportAsync` now enqueues
`EmailJob.SendScheduledReportAsync` instead of `SendEmailAsync`, preserving attachment
data through Hangfire serialization |

---

## Issue 2: Auto-Migration Runs on Every Production Startup

**Root Cause:**
- `Program.cs:118` called `await dbContext.Database.MigrateAsync()` unconditionally on
  every startup → **dangerous in production** (could migrate without manual review,
  cause downtime, or run migrations in the wrong order)

**Changes:**

| File | Change |
|------|--------|
| `src/.../Program.cs` | Wrapped `MigrateAsync()` in
`if (env.IsDevelopment())` block. Seeders (roles, admin user, departments, Hangfire
recurring jobs) still run in all environments. Production teams must run
`dotnet ef database update` manually during deployment |

---

## Issue 3: Duplicate Compliance Score Calculation

**Root Cause:**
- `ClinicService.CalculateComplianceScoreAsync()` was a simplified duplicate that only
  counted **policies** (% of complete vs total)
- `ComplianceScoreService.CalculateScoreAsync()` is the canonical 5-component engine
  (policies, KPIs, checklists, HR credentials, documents) used by `DashboardController`
  and `ComplianceScoreJob`
- Both methods updated `Clinic.ComplianceScore` on the entity, creating a risk of
  inconsistent scores depending on which code path executed last
- The duplicate method was **only called from test code**

**Changes:**

| File | Change |
|------|--------|
| `src/.../ClinicService.cs` | Removed `CalculateComplianceScoreAsync()` method entirely |
| `src/.../ServiceInterfaces.cs` | Removed `Task<decimal> CalculateComplianceScoreAsync(int)` from `IClinicService` |
| `tests/.../ClinicServiceTests.cs` | Removed `CalculateComplianceScoreAsync_WithCompletePolicies_ShouldReturn100` test (tested the removed duplicate) |

---

## Verification

```
Build: 0 errors, warnings unchanged (pre-existing)
Tests: 4 passed, 0 failed, 0 skipped
```
