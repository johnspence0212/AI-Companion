using Microsoft.AspNetCore.Identity;
using EnterpriseStarter.ModuleAbstractions;

namespace EnterpriseStarter.Platform;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
}

public sealed class SecurityAuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string? SubjectId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
    public static IReadOnlyList<string> Protected { get; } = [Admin];
}

public sealed record PermissionDefinition(string Key, string Label, string Group);

public static class AppPermissions
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string AuditRead = "audit.read";
    public const string PermissionClaimType = "permission";

    public static IReadOnlyList<PermissionDefinition> Catalog { get; } =
    [
        new(UsersRead, "View users", "Administration"),
        new(UsersManage, "Manage users", "Administration"),
        new(RolesRead, "View roles", "Administration"),
        new(RolesManage, "Manage roles", "Administration"),
        new(AuditRead, "View security audit", "Administration")
    ];

    public static IReadOnlyList<string> All { get; } = Catalog.Select(x => x.Key).ToArray();
}

public sealed class PermissionCatalog
{
    public PermissionCatalog(IReadOnlyList<IEnterpriseModule> modules)
    {
        var definitions = AppPermissions.Catalog
            .Concat(modules.SelectMany(module => module.Permissions)
                .Select(permission => new PermissionDefinition(
                    permission.Key,
                    permission.Label,
                    permission.Group)))
            .ToArray();

        var duplicate = definitions
            .GroupBy(definition => definition.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Permission '{duplicate.Key}' is registered more than once.");
        }

        Definitions = definitions;
        All = definitions.Select(definition => definition.Key).ToArray();
    }

    public IReadOnlyList<PermissionDefinition> Definitions { get; }
    public IReadOnlyList<string> All { get; }
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public bool ApplyMigrationsOnStartup { get; set; } = true;
    public bool ExitAfterMigrate { get; set; }
}

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public string CookieName { get; set; } = "enterprise_starter_auth";
    public int CookieExpireHours { get; set; } = 8;
    public string CookieSecurePolicy { get; set; } = "SameAsRequest";
    public string? DataProtectionKeysPath { get; set; }
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";
    public string AdminEmail { get; set; } = "admin@enterprisestarter.local";
    public string? AdminPassword { get; set; }
    public string AdminDisplayName { get; set; } = "Administrator";
}

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";
    public int GlobalPermitLimit { get; set; } = 300;
    public int GlobalWindowSeconds { get; set; } = 60;
    public int AuthPermitLimit { get; set; } = 10;
    public int AuthWindowSeconds { get; set; } = 60;
}
