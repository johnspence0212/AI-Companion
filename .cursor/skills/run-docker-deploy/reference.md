# Docker Deploy Reference

## Dev vs deploy

| Concern | Local dev | Docker deploy |
|---------|-----------|---------------|
| Entry | `aspire/AppHost` or standalone api/web | `docker compose up` |
| Web | Vite (HMR) + `/api` proxy | nginx static `dist` + `/api` proxy |
| API URL | Client resolves `/api/v1` | Client resolves `/api/v1` |
| Database | Aspire Postgres | Compose `postgres` + volume |
| Migrations | API startup (dev) | One-shot `migrator` service |
| OpenAPI / Scalar | Dev `/scalar` | Not mapped in Production |
| Health | `/health` (dev), `/health/ready` | API checks `/health/ready` |
| Cookies | Same-origin `/api` | nginx `X-Forwarded-*` + API forwarded headers |

## API Dockerfile notes

- Build context: repository root
- Must copy `Directory.Build.props` and `aspire/ServiceDefaults/`
- Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`, port `8080`
- `DisableHttpsRedirection` expected when behind nginx (HSTS still applied)

## Web Dockerfile notes

- `ARG VITE_API_BASE_URL=/api` before build; client normalizes to `/api/v1`
- nginx proxies `/api/` → `http://api:8080/api/` with forwarded proto/for
- Security headers on nginx responses
- SPA: `try_files $uri $uri/ /index.html`

## Compose services

```yaml
services:
  postgres:   # postgres:16-alpine + postgres_data
  migrator:   # ApplyMigrationsOnStartup + ExitAfterMigrate
  api:        # ApplyMigrationsOnStartup=false; dataprotection_keys volume
  web:        # ports WEB_PORT:80
```

Required `.env` keys: `POSTGRES_PASSWORD`, `SEED_ADMIN_PASSWORD`.  
Ops: `.cursor/docs/DEPLOY-RUNBOOK.md`.
