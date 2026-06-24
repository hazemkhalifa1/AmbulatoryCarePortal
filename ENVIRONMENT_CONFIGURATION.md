# Environment Configuration Guide

## Overview

This project uses the standard ASP.NET Core configuration hierarchy:

```
appsettings.json
  → appsettings.{Environment}.json
    → User Secrets (Development only)
      → Environment Variables
        → Command-line arguments
```

All secrets and environment-specific values are externalized from source control.

---

## Development Setup

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or full instance)
- Redis (optional — falls back to in-memory cache)
- User Secrets initialized

### Quick Start

```powershell
# Restore and build
dotnet restore
dotnet build

# Initialize User Secrets (one-time)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=AmbulatoryCarePortalDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
dotnet user-secrets set "Security:AdminPassword" "YourDevPassword123!"

# Run with hot reload
dotnet watch run --project src/AmbulatoryCarePortal.Presentation
```

### User Secrets Reference

| Key | Example Value | Required |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | LocalDB connection string | Yes |
| `Redis:ConnectionString` | `localhost:6379` | No (falls back to MemoryCache) |
| `Security:AdminPassword` | Initial admin password | Yes |

### Development appsettings

`appsettings.Development.json` contains only logging overrides (Debug level). All secrets are in User Secrets. An example template is at `appsettings.Development.example.json`.

---

## Staging Setup

Staging mirrors production but uses non-production backing services.

### Environment Variables

| Variable | Description | Required |
|---|---|---|
| `DB_CONNECTION_STRING` | Full SQL Server connection string | Yes |
| `REDIS_CONNECTION_STRING` | Redis endpoint (host:port) | No |
| `ADMIN_PASSWORD` | Initial SuperAdmin password | Yes |
| `ConnectionStrings__DefaultConnection` | Alternative to DB_CONNECTION_STRING | Yes* |
| `Redis__ConnectionString` | Alternative to REDIS_CONNECTION_STRING | No |
| `Security__AdminPassword` | Alternative to ADMIN_PASSWORD | Yes* |

\* Must set either the underscore-delimited env var OR the custom variable name.

### Staging Overrides

`appsettings.Staging.json` overrides:
- Logging: `Warning` default, `Information` for Hosting Lifetime
- All connection strings and passwords set to empty (use env vars)

---

## Production Setup

### Required Environment Variables

| Variable | Description | Example | Required |
|---|---|---|---|
| `DB_CONNECTION_STRING` or `ConnectionStrings__DefaultConnection` | SQL Server connection string | `Server=prod.db.example.com;Database=AmbulatoryCarePortal;User Id=app;Password=...;TrustServerCertificate=True;` | Yes |
| `ADMIN_PASSWORD` or `Security__AdminPassword` | Initial SuperAdmin password | (minimum 8 chars, complex) | Yes |
| `REDIS_CONNECTION_STRING` or `Redis__ConnectionString` | Redis cache endpoint | `redis-cluster.example.com:6379` | Strongly recommended |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` | Yes |

### Optional Environment Variables

| Variable | Description | Default |
|---|---|---|
| `Logging__LogLevel__Default` | Default log level | `Warning` |

### Production appsettings

`appsettings.Production.json` overrides:
- Logging: `Warning` default, `Information` for Hosting Lifetime
- All connection strings and passwords set to empty (use env vars only)

---

## Deployment Checklist

### Pre-deployment

- [ ] Rotate any exposed credentials in git history
- [ ] Verify all required env vars are set in the deployment environment
- [ ] Configure Redis connection string (recommended for session/cache performance)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Verify database migration script is ready (`dotnet ef migrations script`)
- [ ] Set up Hangfire database schema (auto-created on first run)
- [ ] Configure SMTP settings via SuperAdmin Settings UI (DB-backed)
- [ ] Verify HTTPS certificate is configured (reverse proxy or Kestrel)

### Health Check Endpoints

| Endpoint | Purpose | Checks |
|---|---|---|
| `GET /health` | Load balancer readiness | Database + Redis |
| `GET /health/ready` | Kubernetes readiness probe | Database + Redis |
| `GET /health/live` | Kubernetes liveness probe | Application self-check |

### Monitoring

- Application logs: `logs/app-{date}.log` (30-day retention, 10MB per file)
- Database error log: `LogEvents` table in application database
- OpenTelemetry tracing is configured for ASP.NET Core, HttpClient, EF Core

### Rollback Plan

1. Revert to previous deployment slot / version
2. Database migrations are forward-only — rollback via restore if needed
3. Hangfire jobs persist in SQL Server — no data loss on redeploy

---

## Configuration Reference

### Sections in appsettings.json

| Section | Options Class | Source |
|---|---|---|
| `ConnectionStrings` | `DatabaseSettings` | appsettings + env vars |
| `EmailSettings` | `EmailSettings` | appsettings (overridden by DB settings) |
| `FileUploadSettings` | `FileUploadSettings` | appsettings |
| `NotificationSettings` | `NotificationSettings` | appsettings |
| `Redis` | `RedisSettings` | appsettings + env vars |
| `Security` | `SecuritySettings` | appsettings + env vars |
| `Serilog` | — | appsettings (read by Serilog) |

### Validation

All Options classes are validated at startup via `ValidateDataAnnotations()` + `ValidateOnStart()`. If required values are missing, the application fails immediately with a clear error message.
