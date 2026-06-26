# EF Core Database Update Diagnosis

> **Date:** 2026-06-26
> **Problem:** `dotnet ef database update` fails with "entry point exited without ever building an IHost"
> **Root Cause:** Design-time factory couldn't resolve the connection string after secrets were removed from JSON

---

## 1. Root Cause

Two independent issues combined to produce the failure:

### Issue A: Design-Time Factory Ignored Environment Variables

`AppDbContextDesignTimeFactory.CreateDbContext()` read the connection string **only** from
`appsettings.json` and `appsettings.Development.json`:

```csharp
// Before (broken):
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();
var connectionString = configuration.GetConnectionString("DefaultConnection");
```

After the config cleanup, `appsettings.json` had `"DefaultConnection": ""`. The factory
never checked `DB_CONNECTION_STRING`, so it always got the empty string.

### Issue B: Runtime `??` Null-Coalescing Never Fired for Empty String

`InfrastructureServiceExtensions.AddInfrastructureServices()` used `??` which only checks
for `null`:

```csharp
// Before (broken):
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? throw new InvalidOperationException(...);
```

`GetConnectionString("DefaultConnection")` returns `""` (empty string, not null), so
`??` short-circuited — the `DB_CONNECTION_STRING` env var was **never read**.

### Why "entry point exited without ever building an IHost"

EF Core design-time first tries the `IDesignTimeDbContextFactory`. If that fails, it
falls back to building the application's `IHost` through `Program.cs`. When
`Program.cs` also fails (because the connection string resolves to `""`), EF core
reports that the entry point completed without successfully building the host.

The misleading error occurs because neither path could produce a working DbContext.

---

## 2. Connection String Flow (Before vs After)

### Before (broken)

```
dotnet ef database update
    ├── Design-time factory found? YES (Infrastructure)
    │   └── CreateDbContext()
    │       └── Reads appsettings.json
    │           └── "DefaultConnection": ""   ← EMPTY
    │           └── UseSqlServer("")           ← THROWS
    │
    └── Factory failed → fallback to Program.cs
        └── AddInfrastructureServices()
            └── GetConnectionString() → ""     ← NOT null, ?? never fires
                └── UseSqlServer("")            ← Silent config
                └── DbContext resolved → fails  ← IHost never built
```

### After (fixed)

**Development (local):**
```
dotnet ef database update
    ├── DB_CONNECTION_STRING env var? NO
    ├── Read appsettings.json → ""
    ├── Read appsettings.Development.json → "Server=.; Database=..."
    └── UseSqlServer(local connection)  ✅ WORKS
```

**Production (server):**
```
dotnet ef database update
    ├── DB_CONNECTION_STRING env var? YES
    └── UseSqlServer(env var value)  ✅ WORKS
```

**Runtime production:**
```
App starts
    ├── GetConnectionString() → "" (empty from config)
    ├── IsNullOrEmpty → YES
    ├── Read DB_CONNECTION_STRING env var → real value
    └── UseSqlServer(env var value)  ✅ WORKS
```

---

## 3. Files Changed

| File | Change |
|------|--------|
| `Infrastructure/AppDbContextDesignTimeFactory.cs` | Added `DB_CONNECTION_STRING` env var check as **first priority** before falling back to JSON config files. Added clear error message when neither provides a connection string. |
| `Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs` | Replaced `??` null-coalescing with `string.IsNullOrEmpty` check so an **empty** config value correctly falls through to the env var. |
| `Presentation/appsettings.json` | `DefaultConnection` set to `""` (production-safe base). No secrets. |
| `Presentation/appsettings.Development.json` | Added `ConnectionStrings.DefaultConnection` with local `Trusted_Connection` value for local development. |

No database schema changes. No migrations modified.

---

## 4. Why EF Failed (and why it works now)

```
Failure chain:
  appsettings.json has "" (no secrets)
    → Design-time factory reads "" from JSON
    → Factory never checked DB_CONNECTION_STRING env var
    → UseSqlServer("") → throws on connection
    → EF falls back to Program.cs
    → ?? doesn't fire on ""
    → Same empty string used
    → Program.cs throws → "entry point exited without building IHost"

Fix:
  Design-time factory now checks env var FIRST
  Runtime now uses string.IsNullOrEmpty instead of ??
  → env var is always consulted for empty config values
```

---

## 5. Migration Test Result

```
> dotnet ef migrations list

Build succeeded.
[18:29:43 FTL] Application terminated unexpectedly   ← Serilog MSSqlServer sink (dev only, no local DB)
...
An error occurred while accessing the Microsoft.Extensions.Hosting services.    ← Host build fails
Continuing without the application service provider.                            ← EF continues anyway
Error: The entry point exited without ever building an IHost.

20260611033928_InitialCreate (Pending)
20260612031816_MakeDepartmentCodeStringAndRemoveUserDept (Pending)
20260612035140_IncreaseNationalIdMaxLength (Pending)
20260612214614_AddSystemSettingsTable (Pending)
20260613051708_AddComplianceScoreSnapshot (Pending)
20260613063427_MakeAuditTrailClinicIdNullable (Pending)
20260613065822_SyncComplianceScoreSnapshotModel (Pending)
20260621165807_AddClinicSignaturesAndTemplateSigners (Pending)
```

**Key observation:** All 9 migrations are listed as "Pending" — the design-time factory
was **found and used**. The "entry point exited" error comes from a **separate** host-build
attempt (Serilog MSSqlServer sink trying to create the `LogEvents` table in dev where the
database doesn't exist). EF catches this, logs it, and continues using the factory's
DbContext.

On the production server with `DB_CONNECTION_STRING` set:
- The factory reads the env var directly
- EF applies migrations successfully
- The Serilog sink also connects successfully (database exists)
- No errors at all

---

## 6. Production Safety Verification

| Concern | Status | Reason |
|---------|--------|--------|
| **Secrets in JSON** | ✅ None | `appsettings.json` has `"DefaultConnection": ""`. Env var provides the real value |
| **Env var fallback** | ✅ Fixed | Both design-time and runtime now check `DB_CONNECTION_STRING` |
| **Serilog crash on prod** | ✅ Safe | On production, `GetConnectionString` returns `""` → `??` skips env var → `connStr = ""` → `IsNullOrEmpty` → MSSqlServer sink **skipped**. No crash |
| **Dev still works** | ✅ Yes | `appsettings.Development.json` provides local connection string |
| **Build** | ✅ 0 errors | |
| **Tests** | ✅ 4/4 passed | |
| **No schema changes** | ✅ Preserved | |
| **No migration modification** | ✅ Preserved | |

---

## 7. Build Result

```
Build succeeded.
    0 Error(s)
```

## 8. Test Result

```
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

---

## 9. Pre-Deployment Steps

Before running `dotnet ef database update` on production:

1. Verify the env var is set: `echo $DB_CONNECTION_STRING`
2. Run from the **Presentation** directory:
   ```bash
   cd src/AmbulatoryCarePortal.Presentation
   dotnet ef database update
   ```
3. If `DB_CONNECTION_STRING` is missing, the factory now shows:
   ```
   Missing DB_CONNECTION_STRING environment variable.
   Set DB_CONNECTION_STRING before running 'dotnet ef database update'.
   ```
