using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using EnterpriseStarter.ModuleAbstractions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace EnterpriseStarter.Platform;

public static class PlatformExtensions
{
    public const string DatabaseConnectionName = "enterprisestarterdb";

    public static IServiceCollection AddEnterprisePlatform(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyList<IEnterpriseModule> modules)
    {
        ValidateModules(modules);
        var permissionCatalog = new PermissionCatalog(modules);
        services.AddSingleton(permissionCatalog);
        foreach (var module in modules)
        {
            services.AddSingleton(typeof(IEnterpriseModule), module);
            foreach (var contributor in module.ModelContributors)
            {
                services.AddSingleton(typeof(IEntityModelContributor), contributor);
            }
        }

        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly)
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddOpenApi("v1", options =>
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title =
                    configuration["OpenApi:Title"] ?? "EnterpriseStarter API";
                document.Info.Version =
                    configuration["OpenApi:Version"] ?? "v1";
                return Task.CompletedTask;
            }));

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new();
        var dataProtection = services.AddDataProtection().SetApplicationName("EnterpriseStarter");
        if (!string.IsNullOrWhiteSpace(auth.DataProtectionKeysPath))
        {
            Directory.CreateDirectory(auth.DataProtectionKeysPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(auth.DataProtectionKeysPath));
        }

        var connection = configuration.GetConnectionString(DatabaseConnectionName)
            ?? configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException(
                $"PostgreSQL connection required at ConnectionStrings:{DatabaseConnectionName} or Database:ConnectionString.");
        var provider = configuration["Database:Provider"] ?? "PostgreSQL";
        if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("EnterpriseStarter supports PostgreSQL only.");
        services.AddDbContext<EnterpriseDbContext>(options =>
        {
            options.UseNpgsql(connection);
            options.ReplaceService<IModelCacheKeyFactory, ModuleModelCacheKeyFactory>();
        });
        services.AddHealthChecks().AddDbContextCheck<EnterpriseDbContext>(tags: ["ready"]);

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<EnterpriseDbContext>()
            .AddClaimsPrincipalFactory<PermissionClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = auth.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = ParseSecurePolicy(auth.CookieSecurePolicy);
            options.ExpireTimeSpan = TimeSpan.FromHours(Math.Max(1, auth.CookieExpireHours));
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthorization(options =>
        {
            foreach (var permission in permissionCatalog.All)
                options.AddPolicy(permission, policy => policy.AddRequirements(new PermissionRequirement(permission)));
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "enterprise_starter_csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = ParseSecurePolicy(auth.CookieSecurePolicy);
        });

        AddRateLimiting(services, configuration);
        AddCors(services, configuration);
        foreach (var module in modules)
            module.AddServices(services, configuration);
        return services;
    }

    public static async Task InitializeEnterpriseDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<EnterpriseDbContext>();
        await db.Database.MigrateAsync();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var permissionCatalog = provider.GetRequiredService<PermissionCatalog>();
        await EnsureRoleAsync(roleManager, AppRoles.Admin, permissionCatalog.All, synchronizePermissions: true);
        await EnsureRoleAsync(roleManager, AppRoles.Member, [], synchronizePermissions: false);

        var seed = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<SeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
            return;
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = seed.AdminEmail.Trim().ToLowerInvariant();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = seed.AdminDisplayName,
                IsActive = true,
                MustChangePassword = true
            };
            var created = await userManager.CreateAsync(admin, seed.AdminPassword);
            if (!created.Succeeded)
                throw new InvalidOperationException(
                    $"Bootstrap admin failed: {string.Join("; ", created.Errors.Select(x => x.Description))}");
        }
        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
    }

    public static WebApplication UseEnterprisePlatform(
        this WebApplication app,
        IReadOnlyList<IEnterpriseModule> modules)
    {
        if (app.Configuration.GetValue("ForwardedHeaders:Enabled", true))
            app.UseForwardedHeaders();
        app.Use(async (context, next) =>
        {
            var correlationId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
            app.UseHsts();
            if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection"))
                app.UseHttpsRedirection();
        }
        app.UseStatusCodePages();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/openapi/{documentName}.json");
            app.MapScalarApiReference();
        }
        app.UseCors("AllowWeb");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<AntiforgeryValidationMiddleware>();
        app.UseMiddleware<MustChangePasswordMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        foreach (var module in modules)
            module.MapEndpoints(app);
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        return app;
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> manager,
        string name,
        IReadOnlyList<string> permissions,
        bool synchronizePermissions)
    {
        var role = await manager.FindByNameAsync(name);
        if (role is null)
        {
            role = new IdentityRole(name);
            var created = await manager.CreateAsync(role);
            if (!created.Succeeded)
                throw new InvalidOperationException($"Could not create protected role {name}.");
        }
        if (!synchronizePermissions)
        {
            return;
        }

        var claims = await manager.GetClaimsAsync(role);
        foreach (var claim in claims.Where(x =>
                     x.Type == AppPermissions.PermissionClaimType && !permissions.Contains(x.Value)))
            await manager.RemoveClaimAsync(role, claim);
        foreach (var permission in permissions.Where(p =>
                     claims.All(x => x.Type != AppPermissions.PermissionClaimType || x.Value != p)))
            await manager.AddClaimAsync(role, new Claim(AppPermissions.PermissionClaimType, permission));
    }

    private static void ValidateModules(IReadOnlyList<IEnterpriseModule> modules)
    {
        var duplicateModule = modules
            .GroupBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateModule is not null)
        {
            throw new InvalidOperationException(
                $"Module '{duplicateModule.Key}' is registered more than once.");
        }

        var contributors = modules.SelectMany(module => module.ModelContributors).ToArray();
        if (contributors.Any(contributor => string.IsNullOrWhiteSpace(contributor.Key)))
        {
            throw new InvalidOperationException("Entity model contributor keys cannot be empty.");
        }

        var duplicateContributor = contributors
            .GroupBy(contributor => contributor.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateContributor is not null)
        {
            throw new InvalidOperationException(
                $"Entity model contributor '{duplicateContributor.Key}' is registered more than once.");
        }
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, limits.GlobalPermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, limits.GlobalWindowSeconds)),
                        QueueLimit = 0
                    }));
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, limits.AuthPermitLimit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, limits.AuthWindowSeconds)),
                    QueueLimit = 0
                }));
        });
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("WebOrigins").Get<string[]>()
            ?? [configuration["WebOrigin"] ?? "http://localhost:5173"];
        services.AddCors(options => options.AddPolicy("AllowWeb", policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
    }

    private static CookieSecurePolicy ParseSecurePolicy(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "always" => CookieSecurePolicy.Always,
            "none" => CookieSecurePolicy.None,
            _ => CookieSecurePolicy.SameAsRequest
        };
}

public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly HashSet<PathString> AllowedPaths =
    [
        new("/api/v1/auth/csrf"),
        new("/api/v1/auth/me"),
        new("/api/v1/auth/change-password"),
        new("/api/v1/auth/logout")
    ];

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !AllowedPaths.Contains(context.Request.Path))
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user?.MustChangePassword == true)
            {
                await Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Password change required").ExecuteAsync(context);
                return;
            }
        }

        await next(context);
    }
}

public sealed class AntiforgeryValidationMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var loginPath = context.Request.Path.Equals("/api/v1/auth/login");
        if (context.User.Identity?.IsAuthenticated == true
            && !SafeMethods.Contains(context.Request.Method)
            && !loginPath)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid antiforgery token").ExecuteAsync(context);
                return;
            }
        }
        await next(context);
    }
}
