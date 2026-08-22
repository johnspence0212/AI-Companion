---
name: add-ef-migration
description: Creates EnterpriseStarter shared PostgreSQL migrations in apps/platform.
disable-model-invocation: true
---

# Add EF Migration

## Prerequisites

- EF tools: `dotnet tool install --global dotnet-ef` (or use local tool)
- Model or module contributor changes are ready

## Create migration

```bash
# from repository root
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add <MigrationName> \
  --project apps/platform \
  --startup-project apps/api
```

Review generated files in `apps/platform/Migrations/`.

## Apply locally

**Automatic (dev/Aspire default):** API startup calls `InitializeEnterpriseDatabaseAsync` when `Database:ApplyMigrationsOnStartup` is true.

**Compose:** one-shot `migrator` service applies migrations; API replicas skip migrate-on-startup.

**Manual:**
```bash
dotnet ef database update --project apps/platform --startup-project apps/api
```

## Design-time factory

`EnterpriseDbContextFactory` in `apps/platform/Data.cs` supplies the PostgreSQL context when AppHost is not running.

## Provider note

EnterpriseStarter is PostgreSQL-only. Platform and compile-time modules share this migration stream.

## Checklist

```
- [ ] Model + Configuration updated
- [ ] Model contributor registered (if a module adds entities)
- [ ] migration add <Name>
- [ ] Review Up/Down for destructive changes
- [ ] dotnet build EnterpriseStarter.sln
- [ ] Run API and verify schema
```

## Never

- `Database.EnsureCreated()`
- Delete production DB without backup
- Edit applied migration history in shared environments — add new migration instead
