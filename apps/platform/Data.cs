using System.Linq.Expressions;
using System.Reflection;
using EnterpriseStarter.ModuleAbstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace EnterpriseStarter.Platform;

public interface IOwnerScope
{
    string? OwnerUserId { get; }
}

public sealed class HttpOwnerScope(IHttpContextAccessor accessor) : IOwnerScope
{
    public string? OwnerUserId =>
        accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}

public sealed class NullOwnerScope : IOwnerScope
{
    public static NullOwnerScope Instance { get; } = new();
    public string? OwnerUserId => null;
}

public sealed class EnterpriseDbContext : IdentityDbContext<ApplicationUser>
{
    private static readonly MethodInfo ApplyOwnershipFilterMethod =
        typeof(EnterpriseDbContext).GetMethod(
            nameof(ApplyOwnershipFilter),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly IReadOnlyList<IEntityModelContributor> _contributors;

    public EnterpriseDbContext(
        DbContextOptions<EnterpriseDbContext> options,
        IEnumerable<IEntityModelContributor>? contributors = null,
        IOwnerScope? ownerScope = null)
        : base(options)
    {
        _contributors = contributors?.ToArray() ?? [];
        OwnerScope = ownerScope ?? NullOwnerScope.Instance;
    }

    public IOwnerScope OwnerScope { get; }

    public string? OwnerUserId => OwnerScope.OwnerUserId;

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

        ApplyOwnershipFilters(builder);
    }

    private void ApplyOwnershipFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (clr is null || !typeof(IOwnedRecord).IsAssignableFrom(clr) || entityType.IsOwned())
            {
                continue;
            }

            ApplyOwnershipFilterMethod.MakeGenericMethod(clr).Invoke(this, [builder]);
        }
    }

    private void ApplyOwnershipFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, IOwnedRecord
    {
        Expression<Func<TEntity, bool>> filter = entity =>
            OwnerUserId != null && entity.OwnerUserId == OwnerUserId;
        builder.Entity<TEntity>().HasQueryFilter(filter);
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
            LoadProductionContributors());
    }

    private static IReadOnlyList<IEntityModelContributor> LoadProductionContributors()
    {
        try
        {
            var assembly = Assembly.Load("EnterpriseStarter.Companion");
            var registryType = assembly.GetType("EnterpriseStarter.Companion.ModuleRegistry");
            if (registryType is null)
            {
                return [];
            }

            var production = registryType.GetProperty("Production")?.GetValue(null)
                as IReadOnlyList<IEnterpriseModule>;
            return production?.SelectMany(module => module.ModelContributors).ToArray() ?? [];
        }
        catch (FileNotFoundException)
        {
            return [];
        }
    }
}
