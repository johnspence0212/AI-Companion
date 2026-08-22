# Aspire Reference

## Key files

| File | Purpose |
|------|---------|
| `aspire/AppHost/Program.cs` | Resource + project wiring |
| `aspire/AppHost/AppHost.csproj` | Aspire hosting packages |
| `aspire/ServiceDefaults/Extensions.cs` | Shared telemetry, health, HTTP defaults |
| `apps/api/Program.cs` | `AddServiceDefaults()` |
| `apps/platform/PlatformExtensions.cs` | Platform EF / external services |

## Current AppHost wiring

```csharp
var postgres = builder.AddPostgres("postgres").WithDataVolume();
var db = postgres.AddDatabase("enterprisestarterdb");

var api = builder.AddProject<Projects.EnterpriseStarter_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("Database__Provider", "PostgreSQL")
    .WithHttpHealthCheck("/health");

var web = builder.AddViteApp("web", "../../apps/web")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", "/api")
    .WithEnvironment("VITE_API_PROXY_TARGET", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints();
```

## Common packages (AppHost.csproj)

```xml
<PackageReference Include="Aspire.Hosting.PostgreSQL" />
<PackageReference Include="Aspire.Hosting.Redis" />
<PackageReference Include="Aspire.Hosting.RabbitMQ" />
```

## API consumption patterns

- **EF PostgreSQL:** `ConnectionStrings:enterprisestarterdb` from Aspire
- **Redis:** `builder.AddRedisClient("cache")` in API when using Aspire client integrations

## Docs

- [.NET Aspire hosting](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview)
