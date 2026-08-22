# EnterpriseStarter reference deployment runbook

Compose is a reference single-host deployment: **PostgreSQL + one-shot migrator + API + nginx web**. Adapt secrets, TLS, backups, monitoring, availability, and recovery to the target production platform.

```bash
cp .env.docker.example .env
# Set POSTGRES_PASSWORD and SEED_ADMIN_PASSWORD (required; not the dev default)
docker compose up --build
# App: http://localhost:8080 (or WEB_PORT)
```

Local day-to-day development stays on Aspire (`dotnet run --project aspire/AppHost`).

---

## Seed admin

| Env | Maps to | Notes |
|-----|---------|-------|
| `SEED_ADMIN_EMAIL` | `Seed__AdminEmail` | Default `admin@enterprisestarter.local` |
| `SEED_ADMIN_PASSWORD` | `Seed__AdminPassword` | Required; use a unique temporary password |

Seeding runs in the one-shot **`migrator`** service. The bootstrap admin is assigned `Admin` and must change the password. There is no public registration endpoint; authorized admins create users with temporary passwords in the application.

---

## Migrations (multi-instance safe)

| Service | Role |
|---------|------|
| `migrator` | `Database__ApplyMigrationsOnStartup=true` + `ExitAfterMigrate=true` |
| `api` | `Database__ApplyMigrationsOnStartup=false` |

Scale API replicas only after the migrator completes. Cookie and CSRF protection across replicas require shared Data Protection keys (`Auth__DataProtectionKeysPath=/keys`).

---

## Backup / restore (Postgres)

```bash
# Backup
docker compose exec postgres pg_dump -U enterprise_starter enterprise_starter > backup.sql

# Restore (stops writers first in real prod)
docker compose exec -T postgres psql -U enterprise_starter enterprise_starter < backup.sql
```

Volume: `postgres_data`.

## TLS, cookies, proxies

- nginx sets `X-Forwarded-Proto` / `X-Forwarded-For`; API trusts forwarded headers.
- Local HTTP compose: `AUTH_COOKIE_SECURE_POLICY=SameAsRequest`.
- HTTPS at the edge: set `AUTH_COOKIE_SECURE_POLICY=Always` and ensure the proxy sends `X-Forwarded-Proto=https`.
- Preserve the same-origin `/api/v1` proxy path.
- Security headers are set on nginx and API responses.
- Production nginx serves an installable PWA of the same SPA (`manifest.webmanifest` + `sw.js`). The service worker is not cached; `/api` is never cached. Installability on phones requires HTTPS at the edge.
- Persist and protect the `dataprotection_keys` volume; losing it invalidates sessions and CSRF material.

---

## OpenTelemetry

Set when you have a collector:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_SERVICE_NAME=enterprise-starter-api
```

Empty endpoint disables the OTLP exporter. Production API also emits **JSON console logs** with correlation scope (`CorrelationId` / `RequestId`); responses include `X-Correlation-ID`.

---

## Smoke checks

```bash
curl -fsS http://localhost:8080/
docker compose exec api curl -fsS http://localhost:8080/health/ready
curl -fsS -c /tmp/enterprise-starter.cookies \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@enterprisestarter.local","password":"<SEED_ADMIN_PASSWORD>"}'
curl -fsS -b /tmp/enterprise-starter.cookies http://localhost:8080/api/v1/auth/me
```

After smoke testing, sign in through the web application and replace the bootstrap password. Confirm the security audit records the login and password change.
