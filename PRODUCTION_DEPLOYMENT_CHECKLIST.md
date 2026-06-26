# Production Deployment Checklist

## Before Deployment

### Configuration

- [ ] **Connection Strings** — Set `ConnectionStrings:DefaultConnection` in
  `appsettings.Production.json` or `DB_CONNECTION_STRING` environment variable
- [ ] **Email Settings** — Configure `EmailSettings` section with valid SMTP
  server, port, credentials, sender address
- [ ] **Admin Password** — Set `Security:AdminPassword` or `ADMIN_PASSWORD`
  environment variable (used by startup seeder)
- [ ] **Redis** — Set `RedisSettings:ConnectionString` for Hangfire and caching
- [ ] **File Upload Limits** — Review `FileUploadSettings` section (max size,
  allowed extensions, base path)

### Environment Variables

Required:
- `DB_CONNECTION_STRING` — SQL Server connection string
- `ADMIN_PASSWORD` — Initial admin password (minimum complexity)

Strongly Recommended:
- `ASPNETCORE_ENVIRONMENT=Production`
- `RedisSettings__ConnectionString`

### Database

- [ ] **Run migrations manually** — `dotnet ef database update`
  (auto-migration is disabled in production)
- [ ] **Verify seed data** — Role permissions, admin user, and departments seed
  on first startup
- [ ] **Backup** — Take a full database backup before first production deploy
- [ ] **Connection pool** — Verify connection string includes `Max Pool Size=100`
  and `TrustServerCertificate=true` if using self-signed certs

### Security

- [ ] **HTTPS** — TLS certificate configured on reverse proxy / load balancer
- [ ] **HSTS** — Enabled by default in production (`app.UseHsts()` in Program.cs)
- [ ] **Rate limiting** — Verify rate limiter configuration
- [ ] **CORS** — Verify `AllowedOrigins` configuration
- [ ] **Cookies** — Verify cookie SameSite mode and secure flag
- [ ] **Security headers** — These are applied by `SecurityHeadersMiddleware`

### Infrastructure

- [ ] **Redis** — Running and reachable by the application
- [ ] **Hangfire** — Uses Redis as job storage; verify ServerName configuration
- [ ] **SMTP** — Verify SMTP server is reachable, port is open, credentials valid
- [ ] **File storage** — Verify `wwwroot/uploads/` directory exists and is
  writable by the application pool

### Logging & Monitoring

- [ ] **Logging sinks** — Console (stdout) for containerized deployments; file
  sink writes to `logs/app-.log`; MSSqlServer sink writes to `LogEvents` table
- [ ] **Health checks** — Available at `/health` (ready) and `/health/live` (live)
  after deployment
- [ ] **Correlation ID** — Enriched on all log entries via `LogContextEnrichmentMiddleware`

## During Deployment

- [ ] **Stop old version** — Graceful shutdown via `app.Run()`
- [ ] **Deploy artifacts** — Publish output to deployment directory
- [ ] **Configure IIS/Kestrel** — Bindings, app pool identity, file permissions
- [ ] **Start new version** — Monitor startup logs for errors

## After Deployment

- [ ] **Health check** — `GET /health` returns 200 OK
- [ ] **Liveness check** — `GET /health/live` returns 200 OK
- [ ] **Hangfire dashboard** — Available at `/hangfire` (restricted by
  `HangfireDashboardAuthorizationFilter`)
- [ ] **First login** — Verify admin login with configured admin password
- [ ] **Email test** — Use test email feature to verify SMTP is working
- [ ] **Background jobs** — Verify recurring jobs appear in Hangfire dashboard
  (document expiry, compliance score, alerts, weekly digest)
- [ ] **Redis connectivity** — Verify cache is populated after first requests
- [ ] **Logs** — Check `logs/app-.log` for errors or warnings

## Rollback Plan

- [ ] **Database rollback** — `dotnet ef database update <previous-migration>`
- [ ] **Binary rollback** — Deploy previous build version
- [ ] **Backup restore** — Full database restore from pre-deployment backup

## Monitoring Alerts

Recommended alert thresholds:
- 5xx responses > 1% of traffic in 5-minute window
- Hangfire job failure rate > 0
- Health check endpoint returns non-200
- SMTP send failure rate > 0
- Database connection pool exhaustion
