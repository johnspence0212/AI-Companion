using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.ModuleAbstractions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Platform;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);
public sealed record UserResponse(
    string Id, string Email, string? DisplayName, bool IsActive, bool MustChangePassword,
    DateTime CreatedAt, DateTime? LastLoginAt, IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
public sealed record UpdateProfileRequest([StringLength(200)] string? DisplayName);
public sealed record CreateUserRequest(
    [Required, EmailAddress] string Email, string? DisplayName,
    [Required] string TemporaryPassword, IReadOnlyList<string>? Roles);
public sealed record ResetPasswordRequest([Required] string TemporaryPassword);
public sealed record SetRolesRequest([Required] IReadOnlyList<string> Roles);
public sealed record SetActiveRequest(bool IsActive);
public sealed record RoleResponse(
    string Id, string Name, bool IsProtected, IReadOnlyList<string> Permissions);
public sealed record SaveRoleRequest(
    [Required] string Name, [Required] IReadOnlyList<string> Permissions);
public sealed record AuditResponse(
    long Id, DateTime OccurredAt, string EventType, string Outcome, string? ActorUserId,
    string? ActorEmail, string? SubjectId, string? IpAddress, string? Details);

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    SignInManager<ApplicationUser> signInManager,
    ISecurityAuditService audit,
    IAntiforgery antiforgery,
    TimeProvider timeProvider,
    IEnumerable<IAfterSignInHandler> afterSignInHandlers) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult Antiforgery()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { headerName = tokens.HeaderName, requestToken = tokens.RequestToken });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            await audit.WriteAsync("auth.login", "failed", user?.Id, $"email={email}");
            return Unauthorized();
        }

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await audit.WriteAsync("auth.login", result.IsLockedOut ? "locked" : "failed", user.Id);
            if (result.IsLockedOut)
            {
                return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
                {
                    Status = StatusCodes.Status423Locked,
                    Title = "Account locked"
                });
            }
            return Unauthorized();
        }

        user.LastLoginAt = timeProvider.GetUtcNow().UtcDateTime;
        user.UpdatedAt = user.LastLoginAt.Value;
        await userManager.UpdateAsync(user);
        await audit.WriteAsync("auth.login", "succeeded", user.Id);
        foreach (var handler in afterSignInHandlers)
        {
            await handler.HandleAsync(user.Id, HttpContext.RequestAborted);
        }

        return Ok(await ToResponseAsync(user, userManager, roleManager));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = userManager.GetUserId(User);
        await audit.WriteAsync("auth.logout", "succeeded", userId);
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }
        return Ok(await ToResponseAsync(user, userManager, roleManager));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? null
            : request.DisplayName.Trim();
        user.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ValidationProblem(ToErrors(result));
        }

        await audit.WriteAsync("account.profile.updated", "succeeded", user.Id);
        return Ok(await ToResponseAsync(user, userManager, roleManager));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return ValidationProblem(ToErrors(result));
        }
        user.MustChangePassword = false;
        user.PasswordChangedAt = timeProvider.GetUtcNow().UtcDateTime;
        user.UpdatedAt = user.PasswordChangedAt.Value;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        await audit.WriteAsync("account.password.changed", "succeeded", user.Id);
        return NoContent();
    }

    internal static async Task<UserResponse> ToResponseAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> manager,
        RoleManager<IdentityRole> roleManager)
    {
        var roles = (await manager.GetRolesAsync(user)).ToArray();
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            foreach (var claim in await roleManager.GetClaimsAsync(role))
            {
                if (claim.Type == AppPermissions.PermissionClaimType)
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        return new(
            user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsActive,
            user.MustChangePassword, user.CreatedAt, user.LastLoginAt, roles,
            permissions.OrderBy(x => x).ToArray());
    }

    internal static ValidationProblemDetails ToErrors(IdentityResult result) =>
        new(result.Errors
            .GroupBy(x => x.Code)
            .ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray()));
}

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ISecurityAuditService audit,
    TimeProvider timeProvider) : ControllerBase
{
    [Authorize(Policy = AppPermissions.UsersRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> List(
        int page = 1, int pageSize = 25, string? search = null)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Email != null && x.Email.ToLower().Contains(term)) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(term)));
        }
        var total = await query.CountAsync();
        var users = await query.OrderBy(x => x.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var rows = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            rows.Add(await AuthController.ToResponseAsync(user, userManager, roleManager));
        }
        return Ok(new PagedResponse<UserResponse>(rows, page, pageSize, total));
    }

    [Authorize(Policy = AppPermissions.UsersManage)]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        var roles = request.Roles is { Count: > 0 } ? request.Roles.Distinct().ToArray() : [AppRoles.Member];
        if (!await RolesExistAsync(roles))
        {
            return BadRequest(Problem(title: "One or more roles do not exist."));
        }
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var email = request.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName?.Trim(),
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var result = await userManager.CreateAsync(user, request.TemporaryPassword);
        if (!result.Succeeded)
        {
            return ValidationProblem(AuthController.ToErrors(result));
        }
        result = await userManager.AddToRolesAsync(user, roles);
        if (!result.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return ValidationProblem(AuthController.ToErrors(result));
        }
        await audit.WriteAsync("admin.user.created", "succeeded", user.Id, $"roles={string.Join(',', roles)}");
        return CreatedAtAction(
            nameof(Get),
            new { userId = user.Id },
            await AuthController.ToResponseAsync(user, userManager, roleManager));
    }

    [Authorize(Policy = AppPermissions.UsersRead)]
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserResponse>> Get(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null
            ? NotFound()
            : Ok(await AuthController.ToResponseAsync(user, userManager, roleManager));
    }

    [Authorize(Policy = AppPermissions.UsersManage)]
    [HttpPost("{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userId, ResetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.TemporaryPassword);
        if (!result.Succeeded) return ValidationProblem(AuthController.ToErrors(result));
        user.MustChangePassword = true;
        user.PasswordChangedAt = timeProvider.GetUtcNow().UtcDateTime;
        user.UpdatedAt = user.PasswordChangedAt.Value;
        await userManager.UpdateSecurityStampAsync(user);
        await audit.WriteAsync("admin.user.password-reset", "succeeded", user.Id);
        return NoContent();
    }

    [Authorize(Policy = AppPermissions.UsersManage)]
    [HttpPut("{userId}/roles")]
    public async Task<IActionResult> SetRoles(string userId, SetRolesRequest request)
    {
        var roles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (roles.Length == 0 || !await RolesExistAsync(roles))
        {
            return BadRequest(Problem(title: "At least one valid role is required."));
        }
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        var current = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, current);
        if (!remove.Succeeded) return ValidationProblem(AuthController.ToErrors(remove));
        var add = await userManager.AddToRolesAsync(user, roles);
        if (!add.Succeeded)
        {
            await userManager.AddToRolesAsync(user, current);
            return ValidationProblem(AuthController.ToErrors(add));
        }
        await userManager.UpdateSecurityStampAsync(user);
        await audit.WriteAsync("admin.user.roles-changed", "succeeded", user.Id, $"roles={string.Join(',', roles)}");
        return NoContent();
    }

    [Authorize(Policy = AppPermissions.UsersManage)]
    [HttpPut("{userId}/active")]
    public async Task<IActionResult> SetActive(string userId, SetActiveRequest request)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (user.Id == userManager.GetUserId(User) && !request.IsActive)
        {
            return BadRequest(Problem(title: "You cannot deactivate your own account."));
        }
        user.IsActive = request.IsActive;
        user.DeactivatedAt = request.IsActive ? null : timeProvider.GetUtcNow().UtcDateTime;
        user.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return ValidationProblem(AuthController.ToErrors(result));
        await userManager.UpdateSecurityStampAsync(user);
        await audit.WriteAsync(
            request.IsActive ? "admin.user.activated" : "admin.user.deactivated",
            "succeeded", user.Id);
        return NoContent();
    }

    private async Task<bool> RolesExistAsync(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role)) return false;
        }
        return true;
    }

    internal static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
}

[ApiController]
[Authorize]
[Route("api/v1/roles")]
public sealed class RolesController(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ISecurityAuditService audit,
    PermissionCatalog permissionCatalog) : ControllerBase
{
    [Authorize(Policy = AppPermissions.RolesRead)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<RoleResponse>>> List(int page = 1, int pageSize = 25)
    {
        (page, pageSize) = UsersController.NormalizePage(page, pageSize);
        var total = await roleManager.Roles.CountAsync();
        var roles = await roleManager.Roles.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var rows = new List<RoleResponse>();
        foreach (var role in roles) rows.Add(await ToResponseAsync(role));
        return Ok(new PagedResponse<RoleResponse>(rows, page, pageSize, total));
    }

    [Authorize(Policy = AppPermissions.RolesRead)]
    [HttpGet("permissions")]
    public IActionResult Permissions() => Ok(permissionCatalog.Definitions);

    [Authorize(Policy = AppPermissions.RolesManage)]
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(SaveRoleRequest request)
    {
        var error = Validate(request);
        if (error is not null) return BadRequest(Problem(title: error));
        if (AppRoles.Protected.Contains(request.Name, StringComparer.OrdinalIgnoreCase))
            return Conflict(Problem(title: "That role name is protected."));
        var role = new IdentityRole(request.Name.Trim());
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded) return ValidationProblem(AuthController.ToErrors(result));
        await ReplacePermissionsAsync(role, request.Permissions);
        await audit.WriteAsync("admin.role.created", "succeeded", role.Id, $"name={role.Name}");
        return CreatedAtAction(nameof(List), await ToResponseAsync(role));
    }

    [Authorize(Policy = AppPermissions.RolesManage)]
    [HttpPut("{roleId}")]
    public async Task<ActionResult<RoleResponse>> Update(string roleId, SaveRoleRequest request)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return NotFound();
        if (IsProtected(role)) return BadRequest(Problem(title: "Protected roles cannot be modified."));
        var error = Validate(request);
        if (error is not null) return BadRequest(Problem(title: error));
        role.Name = request.Name.Trim();
        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded) return ValidationProblem(AuthController.ToErrors(result));
        await ReplacePermissionsAsync(role, request.Permissions);
        await InvalidateUsersAsync(role.Name);
        await audit.WriteAsync("admin.role.updated", "succeeded", role.Id, $"name={role.Name}");
        return Ok(await ToResponseAsync(role));
    }

    [Authorize(Policy = AppPermissions.RolesManage)]
    [HttpDelete("{roleId}")]
    public async Task<IActionResult> Delete(string roleId)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return NotFound();
        if (IsProtected(role)) return BadRequest(Problem(title: "Protected roles cannot be deleted."));
        if ((await userManager.GetUsersInRoleAsync(role.Name!)).Count > 0)
            return Conflict(Problem(title: "Role is assigned to users."));
        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded) return ValidationProblem(AuthController.ToErrors(result));
        await audit.WriteAsync("admin.role.deleted", "succeeded", role.Id, $"name={role.Name}");
        return NoContent();
    }

    private string? Validate(SaveRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Role name is required.";
        var unknown = request.Permissions
            .Except(permissionCatalog.All, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return unknown.Length == 0 ? null : $"Unknown permissions: {string.Join(", ", unknown)}";
    }

    private static bool IsProtected(IdentityRole role) =>
        role.Name is not null && AppRoles.Protected.Contains(role.Name, StringComparer.OrdinalIgnoreCase);

    private async Task ReplacePermissionsAsync(IdentityRole role, IEnumerable<string> permissions)
    {
        foreach (var claim in (await roleManager.GetClaimsAsync(role))
                     .Where(x => x.Type == AppPermissions.PermissionClaimType))
            await roleManager.RemoveClaimAsync(role, claim);
        foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
            await roleManager.AddClaimAsync(role, new Claim(AppPermissions.PermissionClaimType, permission));
    }

    private async Task InvalidateUsersAsync(string? roleName)
    {
        if (roleName is null) return;
        foreach (var user in await userManager.GetUsersInRoleAsync(roleName))
            await userManager.UpdateSecurityStampAsync(user);
    }

    private async Task<RoleResponse> ToResponseAsync(IdentityRole role) =>
        new(
            role.Id, role.Name ?? string.Empty, IsProtected(role),
            (await roleManager.GetClaimsAsync(role))
                .Where(x => x.Type == AppPermissions.PermissionClaimType)
                .Select(x => x.Value).OrderBy(x => x).ToArray());
}

[ApiController]
[Authorize(Policy = AppPermissions.AuditRead)]
[Route("api/v1/audit")]
public sealed class AuditController(EnterpriseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditResponse>>> List(
        int page = 1, int pageSize = 50, string? eventType = null)
    {
        (page, pageSize) = UsersController.NormalizePage(page, pageSize);
        var query = db.SecurityAuditEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(x => x.EventType == eventType);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditResponse(
                x.Id, x.OccurredAt, x.EventType, x.Outcome, x.ActorUserId,
                x.ActorEmail, x.SubjectId, x.IpAddress, x.Details))
            .ToListAsync();
        return Ok(new PagedResponse<AuditResponse>(rows, page, pageSize, total));
    }
}
