# PostgreSQL reference

## Configuration (`apps/api/appsettings.json`)

```json
"Database": {
  "Provider": "PostgreSQL",
  "ConnectionString": "Host=localhost;Port=5432;Database=enterprise_starter;Username=enterprise_starter;Password=enterprise_starter",
  "ApplyMigrationsOnStartup": true,
  "ExitAfterMigrate": false
}
```

Aspire overrides via `ConnectionStrings:enterprisestarterdb`.

## AppHost (`aspire/AppHost/Program.cs`)

```csharp
var postgres = builder.AddPostgres("postgres").WithDataVolume();
var db = postgres.AddDatabase("enterprisestarterdb");
var api = builder.AddProject<Projects.EnterpriseStarter_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("Database__Provider", "PostgreSQL");
```

## Compose (`docker-compose.yml`)

| Service | Role |
|---------|------|
| `postgres` | Postgres 16 + `postgres_data` |
| `migrator` | One-shot migrate + seed; exits |
| `api` | No migrate-on-startup; DataProtection keys volume |
| `web` | nginx SPA + `/api` proxy |

## Related files

- `apps/platform/Data.cs` — `EnterpriseDbContext` and design-time factory
- `apps/platform/PlatformExtensions.cs` — migrate, protected roles, bootstrap admin
- `apps/platform/Migrations/` — shared PostgreSQL migrations
- `.env.docker.example` — `POSTGRES_PASSWORD`, `SEED_ADMIN_PASSWORD`, optional OTEL
- `.cursor/docs/DEPLOY-RUNBOOK.md` — backup and deployment operations
