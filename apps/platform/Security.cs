using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EnterpriseStarter.Platform;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(AppPermissions.PermissionClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

public sealed class PermissionClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var roles = await UserManager.GetRolesAsync(user);
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roles)
        {
            var role = await RoleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }
            foreach (var claim in await RoleManager.GetClaimsAsync(role))
            {
                if (claim.Type == AppPermissions.PermissionClaimType)
                {
                    permissions.Add(claim.Value);
                }
            }
        }
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(AppPermissions.PermissionClaimType, permission));
        }
        return identity;
    }
}

public interface ISecurityAuditService
{
    Task WriteAsync(
        string eventType,
        string outcome,
        string? subjectId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}

public sealed class SecurityAuditService(
    EnterpriseDbContext db,
    IHttpContextAccessor contextAccessor) : ISecurityAuditService
{
    public async Task WriteAsync(
        string eventType,
        string outcome,
        string? subjectId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var context = contextAccessor.HttpContext;
        db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            EventType = eventType,
            Outcome = outcome,
            ActorUserId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            ActorEmail = context?.User.FindFirstValue(ClaimTypes.Email),
            SubjectId = subjectId,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            Details = details
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
