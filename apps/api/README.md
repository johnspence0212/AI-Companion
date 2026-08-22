# apps/api — EnterpriseStarter host

Thin .NET host for the EnterpriseStarter platform and explicitly registered compile-time modules.

## Run

Prefer Aspire (starts Postgres + API + web):

```bash
dotnet run --project aspire/AppHost
```

Standalone (Postgres must already be running):

```bash
dotnet run
```

| Endpoint | Notes |
|----------|-------|
| http://localhost:5000 | API |
| `/scalar` | OpenAPI UI (Development) |
| `/health` | Liveness (Development via ServiceDefaults) |
| `/health/ready` | DB readiness |

## Configuration

| Section | Purpose |
|---------|---------|
| `Database:Provider` | Must be `PostgreSQL` |
| `Database:ConnectionString` | When Aspire does not inject `ConnectionStrings:enterprisestarterdb` |
| `Database:ApplyMigrationsOnStartup` | Dev default `true`; Compose API uses `false` |
| `Database:ExitAfterMigrate` | Compose `migrator` one-shot |
| `ForwardedHeaders:Enabled` | Trust `X-Forwarded-*` behind nginx |
| `WebOrigin` / `WebOrigins` | CORS |
| `Auth` | Cookie name/lifetime/secure policy and Data Protection path |
| `Seed` | Bootstrap admin email and temporary password |

## Auth (Identity cookies)

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/v1/auth/csrf` | Antiforgery request token |
| POST | `/api/v1/auth/login` | HttpOnly session cookie |
| POST | `/api/v1/auth/logout` | Ends the session; CSRF required |
| GET | `/api/v1/auth/me` | Current user, global roles, permissions |
| PUT | `/api/v1/auth/profile` | Update profile; CSRF required |
| POST | `/api/v1/auth/change-password` | Clear required password change |

There is no public registration endpoint. Admins create users with temporary passwords.

## Global RBAC and administration

Roles contain permission claims. `Admin` receives all registered permissions; `Member` receives product-module permissions.

| Method | Path | Notes |
|--------|------|-------|
| GET/POST/PUT | `/api/v1/users*` | User read/manage permissions |
| GET/POST/PUT/DELETE | `/api/v1/roles*` | Role read/manage permissions |
| GET | `/api/v1/audit` | `audit.read` |

## Modules

`ModuleRegistry.Production` registers the Companion module. Product modules implement `IEnterpriseModule` and are registered explicitly at compile time. Runtime module discovery and per-customer toggles are not provided.

## Migrations

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add MigrationName --project apps/platform --startup-project apps/api
```

Compose uses a dedicated **migrator** service so API replicas do not race migrations. See [.cursor/docs/DEPLOY-RUNBOOK.md](../../.cursor/docs/DEPLOY-RUNBOOK.md).

## Tests

Requires PostgreSQL (`enterprise_starter_tests` by default):

```bash
dotnet test ../api.tests/Api.Tests.csproj
```
