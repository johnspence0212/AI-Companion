using EnterpriseStarter.Companion;
using EnterpriseStarter.ModuleAbstractions;
using EnterpriseStarter.Platform;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
IReadOnlyList<IEnterpriseModule> modules = ModuleRegistry.Production;
builder.Services.AddEnterprisePlatform(builder.Configuration, modules);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole();
}

var app = builder.Build();

var database = app.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new();

if (database.ApplyMigrationsOnStartup)
{
    await app.Services.InitializeEnterpriseDatabaseAsync();
}

if (database.ExitAfterMigrate)
{
    return;
}

app.UseEnterprisePlatform(modules);
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
