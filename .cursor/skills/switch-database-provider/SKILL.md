---
name: switch-database-provider
description: Documents EnterpriseStarter's PostgreSQL-only connection, migration, Aspire, Compose, and test wiring.
---

# Database (PostgreSQL-only)

EnterpriseStarter is PostgreSQL-only.

## Configuration

`apps/platform/PlatformExtensions.cs` resolves:

1. `ConnectionStrings:enterprisestarterdb` (Aspire resource reference)
2. else `Database:ConnectionString`

`Database:Provider` must be `PostgreSQL`.

| Flag | Role |
|------|------|
| `Database:ApplyMigrationsOnStartup` | Dev/Aspire default `true` |
| `Database:ExitAfterMigrate` | Compose one-shot `migrator` |

## Local (Aspire)

```bash
dotnet run --project aspire/AppHost
```

AppHost starts Postgres with a data volume and injects the connection string into the API.

## Standalone API (no Aspire)

```
Database__Provider=PostgreSQL
Database__ConnectionString=Host=localhost;Port=5432;Database=enterprise_starter;Username=enterprise_starter;Password=enterprise_starter
```

## Docker Compose

```bash
cp .env.docker.example .env   # POSTGRES_PASSWORD + SEED_ADMIN_PASSWORD
docker compose up --build
```

`migrator` applies migrations + seed; `api` sets `ApplyMigrationsOnStartup=false`.

## Migrations

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add MigrationName --project apps/platform --startup-project apps/api
```

## Tests

- API tests default to PostgreSQL `enterprise_starter_tests`
- E2E defaults to PostgreSQL `enterprisestarter_e2e`
- CI: Postgres service containers on `api` and `e2e` jobs

## Do not

- Use `EnsureCreated()`
- Reintroduce SQLite/SQL Server as the default
- Race migrations across multiple API replicas
- Commit production passwords
