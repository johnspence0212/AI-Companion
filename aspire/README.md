# Aspire orchestration (local dev)

AppHost starts **Postgres + API + Vite** with service discovery. **Do not deploy AppHost to production** — use Docker Compose (`run-docker-deploy` / [DEPLOY-RUNBOOK](../.cursor/docs/DEPLOY-RUNBOOK.md)).

## Prerequisites

- .NET 10 SDK + ASP.NET Core 10 runtime
- Aspire workload: `dotnet workload install aspire`
- Node.js 22+ and npm

## Run

From the **repository root**:

```bash
dotnet run --project aspire/AppHost

# If HTTPS dev cert is not trusted:
dotnet run --project aspire/AppHost --launch-profile http
```

Dashboard shows URLs for `postgres`, `api`, and `web`.

**PostgreSQL dev credentials:** user `enterprise_starter`, password `enterprise_starter` (fixed in `AppHost/Program.cs` so the data volume stays compatible across restarts). If PostgreSQL is unhealthy after a credential change, remove the stale development volume and restart:

```bash
docker rm -f $(docker ps -aq --filter name=postgres-) 2>/dev/null
docker volume rm $(docker volume ls -q --filter name=apphost- --filter name=postgres-data) 2>/dev/null
```

| Injection | Purpose |
|-----------|---------|
| `ConnectionStrings:enterprisestarterdb` | API EF connection |
| `VITE_API_BASE_URL=/api` | Web normalizes to `/api/v1`; same-origin proxy |
| `VITE_API_PROXY_TARGET` | Proxies `/api` to the API endpoint |

Use the **proxied web** URL from the dashboard so login cookies work.

## Projects

| Project | Role |
|---------|------|
| `aspire/AppHost` | Orchestrator (dev only) |
| `aspire/ServiceDefaults` | OpenTelemetry, health, resilience |
| `apps/api` | Thin host; references platform, module abstractions, ServiceDefaults |
| `apps/platform` | Identity, RBAC, data, audit, and migrations |
| `apps/module-abstractions` | Compile-time module contract and empty production registry |

## OpenTelemetry

ServiceDefaults exports OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Empty = disabled. Same variable is wired for Compose in `.env.docker.example`.

## Adding resources

Skill: `add-aspire-resource` in `.cursor/skills/`.
