using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SemaphoreSlim _resetLock = new(1, 1);

    private static readonly string DefaultConnection =
        "Host=localhost;Port=5432;Database=enterprise_starter_tests;Username=enterprise_starter;Password=enterprise_starter";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        var connectionString = Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")
            ?? DefaultConnection;

        builder.UseSetting("Database:Provider", "PostgreSQL");
        builder.UseSetting("Database:ConnectionString", connectionString);
        builder.UseSetting("ConnectionStrings:enterprisestarterdb", connectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "10000");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "PostgreSQL",
                ["Database:ConnectionString"] = connectionString,
                ["ConnectionStrings:enterprisestarterdb"] = connectionString,
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Seed:AdminEmail"] = "admin@enterprisestarter.local",
                ["Seed:AdminPassword"] = "AdminPassword123!",
                ["RateLimiting:GlobalPermitLimit"] = "10000",
                ["RateLimiting:AuthPermitLimit"] = "10000"
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _resetLock.WaitAsync();
        try
        {
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EnterpriseDbContext>();
                await db.Database.ExecuteSqlRawAsync(
                    """DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;""");
            }

            await Services.InitializeEnterpriseDatabaseAsync();
        }
        finally
        {
            _resetLock.Release();
        }
    }
}
