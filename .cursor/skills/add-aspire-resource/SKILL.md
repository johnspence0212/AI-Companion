---
name: add-aspire-resource
description: Adds databases, caches, or container resources to the Aspire AppHost and wires them to the API project. Use when integrating Redis, Postgres, RabbitMQ, or new Aspire-hosted services.
---

# Add Aspire Resource

Edit `aspire/AppHost/Program.cs`. See [reference.md](reference.md).

## Workflow

1. Add Aspire hosting package to `aspire/AppHost/AppHost.csproj` if needed
2. Declare resource in `Program.cs` (e.g. `AddPostgres`, `AddRedis`)
3. Call `.WithReference(resource)` on `api` project
4. Map connection string in API via `IConfiguration` or Aspire service discovery
5. Update `apps/api` to consume the connection (EF, `IDistributedCache`, etc.)
6. Run AppHost and verify dashboard + health checks

## Example: PostgreSQL

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var db = postgres.AddDatabase("enterprisestarterdb");

var api = builder.AddProject<Projects.EnterpriseStarter_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("Database__Provider", "PostgreSQL")
    .WithHttpHealthCheck("/health");
```

API reads `ConnectionStrings:enterprisestarterdb` (Aspire) or `Database:ConnectionString`. PostgreSQL is the only supported database provider.

## Example: Redis cache

```csharp
var cache = builder.AddRedis("cache");
var api = builder.AddProject<Projects.EnterpriseStarter_Api>("api")
    .WithReference(cache);
```

Register platform integrations in `AddEnterprisePlatform` or a compile-time product module, depending on ownership.

## Verify

```bash
dotnet run --project aspire/AppHost
```

Confirm resource is healthy in dashboard; API `/health/ready` passes.
