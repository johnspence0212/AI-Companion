---
name: run-docker-deploy
description: Builds and runs the optional Docker Compose deployment stack (Postgres + api + nginx web). Use when deploying, self-hosting, containerizing, or running docker compose — not for local dev (use run-dev-environment / Aspire instead).
---

# Run Docker Deploy

Docker is **optional**. Local dev stays on Aspire or standalone `dotnet run` / `npm run dev`. Use this skill only when the user wants containerized deployment.

## Quick start

```bash
cd /path/to/EnterpriseStarter
cp .env.docker.example .env
# Set POSTGRES_PASSWORD and SEED_ADMIN_PASSWORD (required).

docker compose up --build
```

Open `http://localhost:8080` (or `WEB_PORT` from `.env`). The web image is an installable PWA of the same SPA. Chromium on localhost HTTP can still install; **phones need HTTPS** at the edge (see the deploy runbook).

## Stack

| Service | Image | Notes |
|---------|-------|-------|
| `postgres` | postgres:16-alpine | Volume `postgres_data` |
| `migrator` | same API image | One-shot migrate + seed; exits |
| `api` | .NET publish | No migrate-on-startup; DataProtection keys volume |
| `web` | nginx + Vite `dist` | Public port; proxies `/api` → `api:8080` |

Build context is the **repo root** (not `apps/api` or `apps/web` alone).

## Key files

| File | Role |
|------|------|
| `docker-compose.yml` | Postgres + migrator + api + web |
| `.env.docker.example` | Template for root `.env` (gitignored) |
| `.cursor/docs/DEPLOY-RUNBOOK.md` | Seed, backup, TLS/cookies, OTEL |
| `apps/api/Dockerfile` | Multi-stage API build; copies `Directory.Build.props` |
| `apps/web/Dockerfile` | Node build → nginx |
| `apps/web/nginx.conf` | SPA fallback + `/api` proxy + security headers |
| `.dockerignore` | Excludes `node_modules`, `bin/obj`, `.env`, etc. |

## Environment (compose → API)

Set in root `.env`; compose maps to ASP.NET config:

```
POSTGRES_PASSWORD=...             # required
SEED_ADMIN_PASSWORD=...           # required; not the dev default
WEB_PORT=8080
WEB_ORIGIN=http://localhost:8080  # CORS
AUTH_COOKIE_SECURE_POLICY=SameAsRequest  # Always when TLS at edge
# OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

Compose sets `DisableHttpsRedirection=true` (TLS terminates at nginx/load balancer, not the API container). API still enables HSTS header + forwarded-header trust.

Web build arg is `/api`; the client normalizes it to `/api/v1`.

Ops details: [.cursor/docs/DEPLOY-RUNBOOK.md](../../docs/DEPLOY-RUNBOOK.md).

## Verify

```bash
curl -fsS http://localhost:8080/                    # SPA HTML
curl -fsS -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@enterprisestarter.local","password":"<SEED_ADMIN_PASSWORD>"}'
```

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `POSTGRES_PASSWORD` / `SEED_ADMIN_PASSWORD` required | Copy `.env.docker.example` → `.env` and set values |
| API unhealthy | `docker compose logs api` / `postgres`; wait for healthchecks |
| API build fails | Ensure `Directory.Build.props` is copied in `apps/api/Dockerfile` |
| 502 on `/api` | Wait for api healthcheck; `docker compose logs api` |
| Stale web API URL | Rebuild web image (`VITE_API_BASE_URL` is build-time) |

## Do not

- Use Docker Compose for day-to-day dev (no Vite HMR, no Aspire dashboard)
- Commit root `.env` with real secrets
- Expose the API port publicly without TLS in production
- Ship AppHost to production — it orchestrates dev only

See [reference.md](reference.md) for Dockerfile and nginx details.
