# Agent entry point

Use `.cursor/rules/` for conventions, `.cursor/skills/` for task workflows, and
[ARCHITECTURE.md](.cursor/docs/ARCHITECTURE.md) for the implemented design.

## Implemented baseline

| Area | EnterpriseStarter decision |
|------|----------------------------|
| Architecture | Modular monolith with a thin host and reusable platform |
| Backend | `apps/api`, `apps/platform`, `apps/module-abstractions` |
| API | Versioned routes under `/api/v1` |
| Data | PostgreSQL only; one shared EF migration stream in `apps/platform/Migrations` |
| AuthN | Identity HttpOnly cookie plus CSRF validation |
| AuthZ | Global Identity roles containing code-defined permission claims |
| Users | Admin-created accounts with temporary passwords and forced password change |
| Modules | Explicit compile-time registry; `ModuleRegistry.Production` is empty |
| Audit | Persistent security audit events and permission-protected audit API |
| Local | Aspire AppHost |
| Reference deploy | Compose with Postgres, migrator, API, and nginx web |
| PWA | Production SPA is installable; same cookie session; `/api` never cached |

The starter has no business modules, public registration, or multitenancy.
OIDC, MFA, email, jobs, storage, and tenancy require a product decision and implementation.

## Start here

| Task | Resource |
|------|----------|
| Run the stack | `run-dev-environment` skill |
| Add a compile-time product module | `add-fullstack-entity` skill |
| Add an EF migration | `add-ef-migration` skill |
| Diagnose cookie or CSRF auth | `debug-auth-flow` skill |
| Add OIDC | `replace-auth` skill |
| Reproduce CI | `run-ci-locally` skill |
| Use the reference deployment | `run-docker-deploy` skill |

## Repository map

- `apps/api` — executable host and configuration
- `apps/platform` — Identity, global RBAC, controllers, EF model/migrations, security audit
- `apps/module-abstractions` — module contract and production registry
- `apps/web` — Vue SPA, auth store, admin views, empty web module registry
- `aspire/AppHost` — development orchestration
- `apps/api.tests` and `e2e` — integration and browser tests
- `.cursor/docs/FILE-INDEX.md` — detailed file map

## Commands

### Aspire (primary)

```bash
dotnet run --project aspire/AppHost
```

### apps/web

```bash
cd apps/web && npm run dev
cd apps/web && npm run build
cd apps/web && npm run lint
cd apps/web && npm run type-check
cd apps/web && npm run test:unit
```

### apps/api

```bash
cd apps/api && dotnet run
cd apps/api && dotnet build
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add MigrationName --project apps/platform --startup-project apps/api
```

### Tests

```bash
dotnet test apps/api.tests/Api.Tests.csproj
cd e2e && npm test
```

### Docker (optional deploy — not for dev)

```bash
cp .env.docker.example .env   # set POSTGRES_PASSWORD + SEED_ADMIN_PASSWORD
docker compose up --build
```

## Guardrails

- Keep `apps/api/Program.cs` thin; platform behavior belongs in `apps/platform`.
- Register product modules explicitly in both compile-time registries.
- Keep browser auth on the application cookie and CSRF protocol.
- Protect APIs with `[Authorize]` and permission policies.
- Use `fetch` through `apps/web/src/api/base/client.ts` and import UI through `@/ui`.
- Use EF migrations, never `EnsureCreated`.
- Keep PostgreSQL as the only supported provider.
- Do not commit `.env`, credentials, or database files.
- Use Aspire for development; treat Compose as a reference deployment.

## Cursor Cloud specific instructions

The startup update script runs `dotnet restore`, `npm ci` (in `apps/web` and `e2e`). Toolchain (.NET 10 SDK, Node 22, PostgreSQL 16) is baked into the VM snapshot; it is not reinstalled on startup.

- .NET SDK lives in `~/.dotnet` (on `PATH` via `~/.bashrc`). In non-interactive shells that skip `~/.bashrc`, call it as `~/.dotnet/dotnet`.
- Aspire (`dotnet run --project aspire/AppHost`) is NOT usable here: its Postgres resource needs a container runtime and Docker is not installed. Use the standalone flow instead (see `run-dev-environment` skill): run the API with `dotnet run --project apps/api --launch-profile http` (http://localhost:5000) and the web with `VITE_API_BASE_URL=/api/v1 VITE_API_PROXY_TARGET=http://localhost:5000 npm run dev` in `apps/web` (http://localhost:5173). Use the web URL for login so cookie/CSRF stay same-origin.
- PostgreSQL 16 runs as a local cluster, not a container, and does NOT auto-start on VM boot. Start it before running the API, tests, or e2e: `sudo pg_ctlcluster 16 main start`. Role/DBs are pre-created: user `enterprise_starter` / password `enterprise_starter` with databases `enterprise_starter` (dev), `enterprise_starter_tests` (api tests), `enterprisestarter_e2e` (Playwright). The default `appsettings` connection string already matches, so `dotnet run --project apps/api` works with no extra config once the cluster is up.
- Postgres data persists in the snapshot. The bootstrap admin (`admin@enterprisestarter.local`) forces a password change on first login; on this snapshot it was already changed to `NewAdminPassword123!` (documented seed is `AdminPassword123!`). To reset seed state, drop and recreate the `enterprise_starter` database and restart the API to re-migrate/seed.
- Playwright's `e2e` job additionally needs browser binaries: `cd e2e && npx playwright install chromium --with-deps` (not part of the update script).
