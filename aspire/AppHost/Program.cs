var builder = DistributedApplication.CreateBuilder(args);

// Fixed dev credentials (match appsettings + CI). Random Aspire passwords break
// persisted WithDataVolume() data when the volume was initialized with an older secret.
var postgresUser = builder.AddParameter("postgres-user", "enterprise_starter", publishValueAsDefault: true);
var postgresPassword = builder.AddParameter("postgres-password", "enterprise_starter", secret: true);

var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
    .WithDataVolume();

var db = postgres.AddDatabase("enterprisestarterdb");

var api = builder.AddProject<Projects.EnterpriseStarter_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("Database__Provider", "PostgreSQL")
    .WithHttpHealthCheck("/health");

// Same-origin /api via Vite proxy keeps auth and antiforgery cookies aligned.
var web = builder.AddViteApp("web", "../../apps/web")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", "/api")
    .WithEnvironment("VITE_API_PROXY_TARGET", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
