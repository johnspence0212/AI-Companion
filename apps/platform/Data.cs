using EnterpriseStarter.ModuleAbstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace EnterpriseStarter.Platform;

public sealed class EnterpriseDbContext(
    DbContextOptions<EnterpriseDbContext> options,
    IEnumerable<IEntityModelContributor>? contributors = null)
    : IdentityDbContext<ApplicationUser>(options)
{
    private readonly IReadOnlyList<IEntityModelContributor> _contributors =
        contributors?.ToArray() ?? [];

    internal string ModelContributorKey => string.Join(
        "|",
        _contributors.Select(contributor => contributor.Key).OrderBy(key => key, StringComparer.Ordinal));

    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.HasIndex(x => x.IsActive);
        });
        builder.Entity<SecurityAuditEvent>(entity =>
        {
            entity.ToTable("SecurityAuditEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Outcome).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ActorUserId).HasMaxLength(450);
            entity.Property(x => x.ActorEmail).HasMaxLength(256);
            entity.Property(x => x.SubjectId).HasMaxLength(450);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.HasIndex(x => x.OccurredAt);
            entity.HasIndex(x => x.EventType);
            entity.HasIndex(x => x.ActorUserId);
        });
        foreach (var contributor in _contributors)
        {
            contributor.Configure(builder);
        }
    }
}

public sealed class ModuleModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is EnterpriseDbContext enterprise
            ? (context.GetType(), enterprise.ModelContributorKey, designTime)
            : (object)(context.GetType(), designTime);
}

public sealed class EnterpriseDbContextFactory : IDesignTimeDbContextFactory<EnterpriseDbContext>
{
    public EnterpriseDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Directory.Exists(Path.Combine(basePath, "apps", "api"))
            ? Path.Combine(basePath, "apps", "api")
            : Path.GetFullPath(Path.Combine(basePath, "..", "api"));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString(PlatformExtensions.DatabaseConnectionName)
            ?? configuration["Database:ConnectionString"]
            ?? "Host=localhost;Port=5432;Database=enterprise_starter;Username=enterprise_starter;Password=enterprise_starter";
        return new EnterpriseDbContext(
            new DbContextOptionsBuilder<EnterpriseDbContext>().UseNpgsql(connection).Options,
            []);
    }
}
