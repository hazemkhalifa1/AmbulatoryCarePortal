# Appsettings Configuration Audit

## 1. Current Configuration Analysis

### 1.1 Configuration Files

| File | Path | Role |
|------|------|------|
| `appsettings.json` | `src/AmbulatoryCarePortal.Presentation/appsettings.json` | Base/shared configuration (tracked by Git) |
| `appsettings.Development.json` | `src/AmbulatoryCarePortal.Presentation/appsettings.Development.json` | Development overrides (gitignored) |
| `appsettings.Production.json` | `src/AmbulatoryCarePortal.Presentation/appsettings.Production.json` | Production overrides (tracked by Git, **NEW**) |
| `launchSettings.json` | `Properties/launchSettings.json` | VS launch profile (gitignored, local only) |
| `launchSettings.example.json` | `Properties/launchSettings.example.json` | Template reference file (**NEW**) |
| User Secrets | `~/.microsoft/usersecrets/...` | Developer secrets (**NEW, initialized**) |

### 1.2 Configuration Key Inventory

| Key | Section | Used By | Source | Status |
|-----|---------|---------|--------|--------|
| `ConnectionStrings:DefaultConnection` | Database | `InfrastructureServiceExtensions`, `HangfireServiceExtensions`, `Program.cs` (Serilog), `AppDbContextDesignTimeFactory` | appsettings | Kept |
| `EmailSettings:SmtpServer` | Email | (not used by code — DB-backed SystemSetting used instead) | appsettings | Kept |
| `EmailSettings:SmtpPort` | Email | same | appsettings | Kept |
| `EmailSettings:EnableSsl` | Email | same | appsettings | Kept |
| `EmailSettings:SenderEmail` | Email | same | appsettings | Kept |
| `EmailSettings:SenderName` | Email | same | appsettings | Kept |
| `EmailSettings:Username` | Email | same | appsettings | Kept |
| `EmailSettings:Password` | Email | same | appsettings | Kept |
| `FileUploadSettings:MaxFileSizeBytes` | Storage | (not used by any code) | appsettings | Kept |
| `FileUploadSettings:AllowedExtensions` | Storage | same | appsettings | Kept |
| `FileUploadSettings:BasePath` | Storage | same | appsettings | Kept |
| `NotificationSettings:ExpiryWarningDays` | Notification | (used by DB-backed SystemSetting) | appsettings | Kept |
| `NotificationSettings:CheckIntervalMinutes` | Notification | `NotificationBackgroundService` | appsettings | Kept |
| `Redis:ConnectionString` | Cache | `CachingServiceExtensions` | **NEW** — was env-var only | Added |
| `Security:AdminPassword` | Security | `Program.cs` (admin seed) | **NEW** — was flat key + env var only | Added |
| `AdminPassword` | (flat key) | `Program.cs` (backward compat) | env var / launchSettings | Kept as fallback |
| `Serilog:*` | Logging | Serilog bootstrap | appsettings | Kept |
| `Logging:*` | Logging | ASP.NET Core logging | appsettings | Kept |
| `AllowedHosts` | General | ASP.NET Core | appsettings | Kept |

### 1.3 Configuration Access Patterns (Before)

| File | Pattern | Risk |
|------|---------|------|
| `NotificationBackgroundService.cs` | `IConfiguration` injection + `GetValue<int>()` | Tight coupling to raw IConfiguration |
| `CachingServiceExtensions.cs` | `configuration.GetValue<string>()` | Acceptable for DI extension method |
| `InfrastructureServiceExtensions.cs` | `configuration.GetConnectionString()` | Acceptable for DI extension method |
| `HangfireServiceExtensions.cs` | `configuration.GetConnectionString()` | Acceptable for DI extension method |
| `Program.cs` | `builder.Configuration["AdminPassword"]` | Fragile flat key access |
| `Program.cs` | `ctx.Configuration.GetConnectionString()` | Acceptable for Serilog setup |

### 1.4 Configuration Access Patterns (After)

| File | Pattern | Improvement |
|------|---------|-------------|
| `NotificationBackgroundService.cs` | `IOptions<NotificationSettings>` | Strongly typed, testable, no IConfiguration coupling |
| `CachingServiceExtensions.cs` | `configuration.GetValue<string>()` | Kept (static ext method — not suitable for DI) |
| `InfrastructureServiceExtensions.cs` | `configuration.GetConnectionString()` | Kept (static ext method) |
| `HangfireServiceExtensions.cs` | `configuration.GetConnectionString()` | Kept (static ext method) |
| `Program.cs` | `GetSection("Security")["AdminPassword"]` + fallback chain | Structured access with backward compat |
| `Program.cs` | `ctx.Configuration.GetConnectionString()` | Kept |

---

## 2. Problems Found (Before)

### Problem 1: Flat/Unstructured Configuration Keys
- `AdminPassword` was read via `builder.Configuration["AdminPassword"]` — a flat key with no parent section, making it harder to find and organize.

### Problem 2: Missing Keys
- `Redis:ConnectionString` was read by `CachingServiceExtensions` but never defined in `appsettings.json` — relied solely on environment variables with no documented baseline.

### Problem 3: No Strongly-Typed Options Classes
- Zero `IOptions<T>` usage. Every config consumer either injected raw `IConfiguration` or used `GetValue<()>` with magic strings.

### Problem 4: No `appsettings.Production.json`
- Only base + development overrides existed. Production overrides were impossible without environment variables.

### Problem 5: Secrets in `launchSettings.json`
- `AdminPassword` was hardcoded in `Properties/launchSettings.json` environment variables (`CbahiAdmin@2024`).

### Problem 6: No User Secrets Initialization
- `UserSecretsId` was not set, preventing the use of .NET Secret Manager for local development secrets.

### Problem 7: No Environment Variable Documentation
- `DB_CONNECTION_STRING`, `REDIS_CONNECTION_STRING`, `ADMIN_PASSWORD` env vars were used but undocumented.

---

## 3. Improvements Applied

### Improvement 1: Strongly-Typed Options Classes
**Files created:** `src/AmbulatoryCarePortal.Application/Settings/`

| Class | Section | Const Name |
|-------|---------|------------|
| `DatabaseSettings` | `ConnectionStrings` | `DatabaseSettings.SectionName` |
| `EmailSettings` | `EmailSettings` | `EmailSettings.SectionName` |
| `FileUploadSettings` | `FileUploadSettings` | `FileUploadSettings.SectionName` |
| `NotificationSettings` | `NotificationSettings` | `NotificationSettings.SectionName` |
| `RedisSettings` | `Redis` | `RedisSettings.SectionName` |
| `SecuritySettings` | `Security` | `SecuritySettings.SectionName` |

Each class is a plain POCO with default values matching the existing `appsettings.json` defaults. They include a `public const string SectionName` for safe registration.

### Improvement 2: DI Registration via `services.Configure<T>()`
**File modified:** `src/AmbulatoryCarePortal.Presentation/Program.cs`

All 6 Options classes are registered:
```csharp
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection(DatabaseSettings.SectionName));
builder.Services.Configure<NotificationSettings>(builder.Configuration.GetSection(NotificationSettings.SectionName));
// ... etc
```

### Improvement 3: `NotificationBackgroundService` Refactored
**File modified:** `src/AmbulatoryCarePortal.Application/Services/NotificationBackgroundService.cs`

- Removed `IConfiguration` dependency
- Injected `IOptions<NotificationSettings>` instead
- Changed `_configuration.GetValue<int>("NotificationSettings:CheckIntervalMinutes", 60)` to `_notificationSettings.Value.CheckIntervalMinutes`

### Improvement 4: Structured `AdminPassword` Access
**File modified:** `src/AmbulatoryCarePortal.Presentation/Program.cs`

Changed from flat key access to a fallback chain:
1. `Security:AdminPassword` (new structured section)
2. `AdminPassword` (legacy flat key — backward compatible)
3. `ADMIN_PASSWORD` (environment variable)
4. Throws with descriptive message

### Improvement 5: New Configuration Sections Added
**File modified:** `src/AmbulatoryCarePortal.Presentation/appsettings.json`

Added two new sections (with empty defaults for safety):
- `Redis.ConnectionString`
- `Security.AdminPassword`

All existing sections kept exactly as-is.

### Improvement 6: `appsettings.Production.json` Created
**File created:** `src/AmbulatoryCarePortal.Presentation/appsettings.Production.json`

Production-appropriate overrides:
- `Logging:LogLevel` — reduced to Warning for Microsoft, Information for Lifetime
- Empty placeholders for all secrets (ConnectionStrings, Email, Redis, Security)
- Proper override structure that layers on top of base `appsettings.json`

### Improvement 7: Appsettings Development Updated
**File modified:** `src/AmbulatoryCarePortal.Presentation/appsettings.Development.json`

Added Redis and Security sections for local development:
- `Redis.ConnectionString` = `localhost:6379`
- `Security.AdminPassword` = preserved from original launchSettings

### Improvement 8: `launchSettings.json` Secrets Removed
**File modified:** `src/AmbulatoryCarePortal.Presentation/Properties/launchSettings.json`

- Removed `AdminPassword` environment variable (now set via `appsettings.Development.json`)
- Created `launchSettings.example.json` as a safe template reference
- Original launchSettings backed up to `$env:TEMP\launchSettings.json.bak`

### Improvement 9: User Secrets Initialized
```bash
dotnet user-secrets init -p src/AmbulatoryCarePortal.Presentation
```

`UserSecretsId` = `1bd94c5f-3a5f-483d-b7e9-17789b3a197f`

Developers can now set secrets via:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>"
dotnet user-secrets set "Security:AdminPassword" "<value>"
dotnet user-secrets set "Redis:ConnectionString" "<value>"
```

---

## 4. Files Changed

### New Files
| File | Purpose |
|------|---------|
| `src/AmbulatoryCarePortal.Application/Settings/DatabaseSettings.cs` | Options class |
| `src/AmbulatoryCarePortal.Application/Settings/NotificationSettings.cs` | Options class |
| `src/AmbulatoryCarePortal.Application/Settings/FileUploadSettings.cs` | Options class |
| `src/AmbulatoryCarePortal.Application/Settings/RedisSettings.cs` | Options class |
| `src/AmbulatoryCarePortal.Application/Settings/SecuritySettings.cs` | Options class |
| `src/AmbulatoryCarePortal.Application/Settings/EmailSettings.cs` | Options class |
| `src/AmbulatoryCarePortal.Presentation/appsettings.Production.json` | Production overrides |
| `src/AmbulatoryCarePortal.Presentation/Properties/launchSettings.example.json` | Launch settings template |

### Modified Files
| File | Change |
|------|--------|
| `src/AmbulatoryCarePortal.Presentation/appsettings.json` | Added `Redis` and `Security` sections |
| `src/AmbulatoryCarePortal.Presentation/appsettings.Development.json` | Added `Redis` and `Security` sections |
| `src/AmbulatoryCarePortal.Presentation/Program.cs` | Added `services.Configure<T>` registrations, updated AdminPassword access |
| `src/AmbulatoryCarePortal.Application/Services/NotificationBackgroundService.cs` | Replaced `IConfiguration` with `IOptions<NotificationSettings>` |
| `src/AmbulatoryCarePortal.Presentation/Properties/launchSettings.json` | Removed `AdminPassword` env var |

---

## 5. Security Improvements

| Issue | Before | After |
|-------|--------|-------|
| `AdminPassword` hardcoded | In `launchSettings.json` (gitignored but visible locally) | In `appsettings.Development.json` (gitignored) + User Secrets |
| Redis connection string | Only via env var `REDIS_CONNECTION_STRING` | Documented in `appsettings.Development.json` + env var |
| Production secrets | No structure for production overrides | `appsettings.Production.json` with empty placeholders — secrets via env vars |
| User Secrets | Not initialized | `UserSecretsId` set — `dotnet user-secrets set` ready |
| Environment variables | Undocumented | Documented in this audit |
| `launchSettings.json` template | None | `launchSettings.example.json` with safe placeholders |

### Env-var Reference

| Environment Variable | Config Override | Used In |
|---------------------|-----------------|---------|
| `DB_CONNECTION_STRING` | `ConnectionStrings:DefaultConnection` | `InfrastructureServiceExtensions`, `Program.cs` |
| `REDIS_CONNECTION_STRING` | `Redis:ConnectionString` | `CachingServiceExtensions` |
| `ADMIN_PASSWORD` | `Security:AdminPassword` | `Program.cs` |

---

## 6. Migration Notes

### Backward Compatibility
- All existing `appsettings.json` keys are preserved (nothing removed)
- `builder.Configuration["AdminPassword"]` still works as a fallback
- Environment variables `DB_CONNECTION_STRING` and `REDIS_CONNECTION_STRING` continue to work
- All existing services, controllers, views, and background jobs continue functioning

### What Changed Behavior
- `NotificationBackgroundService` now reads `CheckIntervalMinutes` from `IOptions<NotificationSettings>` — functionally identical (same config section, same default)

### Developer Setup After Upgrade
```bash
# 1. Restore launchSettings.json (if needed)
copy "$env:TEMP\launchSettings.json.bak" "src/AmbulatoryCarePortal.Presentation\Properties\launchSettings.json"

# 2. Set User Secrets for any overrides
dotnet user-secrets set "Security:AdminPassword" "CbahiAdmin@2024" -p src/AmbulatoryCarePortal.Presentation

# 3. Build
dotnet build
```

### Deployment Checklist
- Set `ConnectionStrings:DefaultConnection` via env var `DB_CONNECTION_STRING`
- Set `Redis:ConnectionString` via env var `REDIS_CONNECTION_STRING` (or configure `appsettings.Production.json`)
- Set `Security:AdminPassword` via env var `ADMIN_PASSWORD` (or configure `appsettings.Production.json`)
- Configure SMTP via the SuperAdmin Settings UI (DB-backed SystemSetting table, NOT appsettings)

---

## 7. Build Result

```
Build succeeded with 0 errors, 129 warnings
```

All 129 warnings are pre-existing (CS8618, CS8604, CS8602, CS8603, CS1998, CS8714, CS8621 in ViewModels and Controllers) — none were introduced by this refactoring.
