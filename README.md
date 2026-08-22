# EnterpriseStarter

EnterpriseStarter is a Vue 3 and .NET modular-monolith foundation for enterprise products. It ships a secure administration platform—not a sample business domain—with ASP.NET Identity, global permission RBAC, cookie and CSRF protection, PostgreSQL, security auditing, and an empty compile-time module registry.

The starter ships administration and empty module registries only — no business modules, public registration, or multitenancy. OIDC, MFA, email, background jobs, storage, and tenancy are deliberate product extensions.

See [the implemented plan](.cursor/docs/ENTERPRISE-PLAN.md) and [architecture overview](.cursor/docs/ARCHITECTURE.md).

## Quick start (Aspire)

**Prerequisites:** .NET 10 SDK, Node.js 22+, npm, Aspire workload (`dotnet workload install aspire`)

```bash
git clone <this-repo> my-app
cd my-app   # repository root — required

dotnet run --project aspire/AppHost

# HTTP-only profile (no dev HTTPS cert):
dotnet run --project aspire/AppHost --launch-profile http
```

Open the Aspire dashboard for **postgres**, **api**, and **web** URLs. Prefer the proxied web URL so auth cookies stay same-origin (`VITE_API_BASE_URL=/api`).

Default seeded admin (Development):

| | |
|--|--|
| Email | `admin@enterprisestarter.local` |
| Password | `AdminPassword123!` |
| First login | Requires a password change |

## Repository layout

```
├── apps/
│   ├── web/                 # Vue 3 SPA and typed fetch client
│   ├── api/                 # Thin executable host
│   ├── platform/            # Identity, RBAC, data, audit, controllers
│   ├── companion/           # AI Companion product module
│   ├── module-abstractions/ # Compile-time module contract
│   └── api.tests/           # API integration tests
├── aspire/
│   ├── AppHost/         # Local: Postgres + API + Vite (not for production)
│   └── ServiceDefaults/ # OTEL, health, resilience
├── e2e/                 # Playwright
├── docker-compose.yml   # Deploy: postgres + migrator + api + nginx web
├── .cursor/docs/        # ENTERPRISE-PLAN, DEPLOY-RUNBOOK, FILE-INDEX, …
└── EnterpriseStarter.sln
```

## Stack

| Layer | Technology |
|-------|------------|
| Web | Vue 3, Pinia, Vue Router, Tailwind 4, template library `@/ui`; production builds are installable PWAs (same SPA) |
| API client | Native `fetch`, cookie credentials, automatic CSRF header |
| API | .NET 10, versioned `/api/v1`, Scalar in development |
| Platform | `apps/platform`: Identity, global roles/permissions, audit, EF Core |
| AuthN | HttpOnly Identity cookie + antiforgery token |
| AuthZ | Global Identity roles with code-defined permission claims |
| Modules | Explicit compile-time registration; Companion is in the production registry |
| DB | **PostgreSQL only** (Aspire local + Compose deploy) |
| Orchestration | Aspire AppHost (dev); Compose (deploy) |

## Individual apps (debugging)

Postgres must be reachable (prefer Aspire).

```bash
# API only — http://localhost:5000
cd apps/api && dotnet run

# Web only — http://localhost:5173 (proxy /api → API)
cd apps/web && cp .env.example .env && npm install && npm run dev
```

Prefer `VITE_API_BASE_URL=/api/v1` with `VITE_API_PROXY_TARGET=http://localhost:5000` so cookies remain same-origin.

## Progressive Web App

Same SPA in a browser tab or installed. Cookie + CSRF do not change. Offline data and push are not included.

| Goal | How |
|------|-----|
| Day-to-day development | `dotnet run --project aspire/AppHost` or `npm run dev` — **no** service worker |
| Try install on localhost | `cd apps/web && npm run dev:pwa` (same Vite proxy; Chromium Install in the address bar, or **Install app** in the account menu / sign-in screen) |
| Closest to production | `cd apps/web && npm run build && npm run preview` |
| Production / Compose | `npm run build` inside the web image; nginx serves `manifest.webmanifest` + `sw.js`. Phones need **HTTPS** at the edge |
| Safari / iOS | No in-app prompt — Share → Add to Home Screen |

Details: [apps/web/README.md](apps/web/README.md).

## Docker deployment

Production-style stack: **Postgres + one-shot migrator + API + nginx**.

```bash
cp .env.docker.example .env
# Set POSTGRES_PASSWORD and SEED_ADMIN_PASSWORD (not the dev default)

docker compose up --build
```

Open `http://localhost:8080` (or `WEB_PORT`). Ops details: [.cursor/docs/DEPLOY-RUNBOOK.md](.cursor/docs/DEPLOY-RUNBOOK.md).

## Tests

```bash
dotnet test apps/api.tests/Api.Tests.csproj   # needs PostgreSQL
cd apps/web && npm run test:unit
cd e2e && npm install && npm test             # needs PostgreSQL
```

## Documentation

| Doc | Purpose |
|-----|---------|
| [.cursor/docs/ENTERPRISE-PLAN.md](.cursor/docs/ENTERPRISE-PLAN.md) | Implemented scope and product decisions |
| [.cursor/docs/ARCHITECTURE.md](.cursor/docs/ARCHITECTURE.md) | Runtime, project boundaries, security, modules |
| [.cursor/docs/adr/](.cursor/docs/adr/) | Architecture decision records |
| [.cursor/docs/DEPLOY-RUNBOOK.md](.cursor/docs/DEPLOY-RUNBOOK.md) | Reference Compose deployment operations |
| [.cursor/docs/PHASE-6-BACKLOG.md](.cursor/docs/PHASE-6-BACKLOG.md) | Optional product extensions |
| [.cursor/docs/FILE-INDEX.md](.cursor/docs/FILE-INDEX.md) | Key file map |
| [apps/web/README.md](apps/web/README.md) / [apps/api/README.md](apps/api/README.md) / [aspire/README.md](aspire/README.md) | Per-project |
| [AGENTS.md](AGENTS.md) | AI agent entry point |
| `.cursor/rules/` + `.cursor/skills/` | Conventions and task skills |

## License

MIT
