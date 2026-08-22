using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class ApiIntegrationTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task LoginAndMe_ReturnRolesPermissionsAndLifecycleState()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateClient();

        var login = await LoginAsync(client, AdminEmail, AdminPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loggedIn = await ReadAsync<UserDto>(login);
        Assert.True(loggedIn.MustChangePassword);
        Assert.Contains("Admin", loggedIn.Roles);
        Assert.Contains("users.manage", loggedIn.Permissions);
        Assert.Contains("roles.manage", loggedIn.Permissions);
        Assert.Contains("audit.read", loggedIn.Permissions);

        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(AdminEmail, (await ReadAsync<UserDto>(me)).Email);
    }

    [Fact]
    public async Task UnsafeAuthenticatedRequests_RequireValidCsrfToken()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, AdminEmail, AdminPassword)).StatusCode);

        var rejected = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var csrf = await GetCsrfAsync(client);
        var accepted = await SendAsync(client, HttpMethod.Post, "/api/v1/auth/logout", null, csrf);
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task TemporaryPassword_ForcesPasswordChangeBeforeOtherOperations()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, AdminEmail, AdminPassword)).StatusCode);
        var csrf = await GetCsrfAsync(client);

        var blocked = await SendAsync(
            client,
            HttpMethod.Put,
            "/api/v1/auth/profile",
            new { displayName = "Blocked" },
            csrf);
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

        var changed = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/change-password",
            new { currentPassword = AdminPassword, newPassword = "ChangedAdmin123!" },
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        csrf = await GetCsrfAsync(client);
        var profile = await SendAsync(
            client,
            HttpMethod.Put,
            "/api/v1/auth/profile",
            new { displayName = "Ready Administrator" },
            csrf);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var updated = await ReadAsync<UserDto>(profile);
        Assert.False(updated.MustChangePassword);
        Assert.Equal("Ready Administrator", updated.DisplayName);
    }

    [Fact]
    public async Task MultipleGlobalRoles_UnionPermissions()
    {
        await factory.ResetDatabaseAsync();
        var (admin, csrf) = await CreateReadyAdminAsync();
        using (admin)
        {
            var firstRoleName = $"UserReaders-{Guid.NewGuid():N}";
            var secondRoleName = $"RoleReaders-{Guid.NewGuid():N}";
            Assert.Equal(
                HttpStatusCode.Created,
                (await SendAsync(
                    admin,
                    HttpMethod.Post,
                    "/api/v1/roles",
                    new { name = firstRoleName, permissions = new[] { "users.read" } },
                    csrf)).StatusCode);
            Assert.Equal(
                HttpStatusCode.Created,
                (await SendAsync(
                    admin,
                    HttpMethod.Post,
                    "/api/v1/roles",
                    new { name = secondRoleName, permissions = new[] { "roles.read" } },
                    csrf)).StatusCode);

            var email = $"multi-{Guid.NewGuid():N}@test.local";
            var createdResponse = await SendAsync(
                admin,
                HttpMethod.Post,
                "/api/v1/users",
                new
                {
                    email,
                    displayName = "Multi Role",
                    temporaryPassword = "TemporaryUser123!",
                    roles = new[] { firstRoleName, secondRoleName }
                },
                csrf);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

            using var userClient = CreateClient();
            var login = await LoginAsync(userClient, email, "TemporaryUser123!");
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var user = await ReadAsync<UserDto>(login);
            Assert.Equal(2, user.Roles.Count);
            Assert.Contains(firstRoleName, user.Roles);
            Assert.Contains(secondRoleName, user.Roles);
            Assert.Contains("users.read", user.Permissions);
            Assert.Contains("roles.read", user.Permissions);
        }
    }

    [Fact]
    public async Task AdminRole_IsProtected_AndMemberRoleIsEditable()
    {
        await factory.ResetDatabaseAsync();
        var (admin, csrf) = await CreateReadyAdminAsync();
        using (admin)
        {
            var rolesResponse = await admin.GetAsync("/api/v1/roles?pageSize=100");
            Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);
            var roles = await ReadAsync<PagedDto<RoleDto>>(rolesResponse);

            var adminRole = Assert.Single(roles.Items, item => item.Name == "Admin");
            var update = await SendAsync(
                admin,
                HttpMethod.Put,
                $"/api/v1/roles/{adminRole.Id}",
                new { name = "Admin-changed", permissions = Array.Empty<string>() },
                csrf);
            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);

            var delete = await SendAsync(
                admin,
                HttpMethod.Delete,
                $"/api/v1/roles/{adminRole.Id}",
                null,
                csrf);
            Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);

            var memberRole = Assert.Single(roles.Items, item => item.Name == "Member");
            var memberUpdate = await SendAsync(
                admin,
                HttpMethod.Put,
                $"/api/v1/roles/{memberRole.Id}",
                new { name = "Member", permissions = Array.Empty<string>() },
                csrf);
            Assert.Equal(HttpStatusCode.OK, memberUpdate.StatusCode);
        }
    }

    [Fact]
    public async Task Deactivation_InvalidatesExistingSession_AndReactivationAllowsLogin()
    {
        await factory.ResetDatabaseAsync();
        var (admin, adminCsrf) = await CreateReadyAdminAsync();
        using (admin)
        {
            var email = $"lifecycle-{Guid.NewGuid():N}@test.local";
            var create = await SendAsync(
                admin,
                HttpMethod.Post,
                "/api/v1/users",
                new
                {
                    email,
                    displayName = "Lifecycle User",
                    temporaryPassword = "TemporaryUser123!",
                    roles = new[] { "Member" }
                },
                adminCsrf);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var user = await ReadAsync<UserDto>(create);

            using var userClient = CreateClient();
            Assert.Equal(
                HttpStatusCode.OK,
                (await LoginAsync(userClient, email, "TemporaryUser123!")).StatusCode);
            var userCsrf = await GetCsrfAsync(userClient);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendAsync(
                    userClient,
                    HttpMethod.Post,
                    "/api/v1/auth/change-password",
                    new
                    {
                        currentPassword = "TemporaryUser123!",
                        newPassword = "PermanentUser123!"
                    },
                    userCsrf)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await userClient.GetAsync("/api/v1/auth/me")).StatusCode);

            var deactivate = await SendAsync(
                admin,
                HttpMethod.Put,
                $"/api/v1/users/{user.Id}/active",
                new { isActive = false },
                adminCsrf);
            Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await userClient.GetAsync("/api/v1/auth/me")).StatusCode);
            using var disabledLogin = CreateClient();
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await LoginAsync(disabledLogin, email, "PermanentUser123!")).StatusCode);

            var reactivate = await SendAsync(
                admin,
                HttpMethod.Put,
                $"/api/v1/users/{user.Id}/active",
                new { isActive = true },
                adminCsrf);
            Assert.Equal(HttpStatusCode.NoContent, reactivate.StatusCode);
            using var relogin = CreateClient();
            Assert.Equal(
                HttpStatusCode.OK,
                (await LoginAsync(relogin, email, "PermanentUser123!")).StatusCode);
        }
    }

    [Fact]
    public async Task PagedAdministrationApis_ReturnAuditEvents()
    {
        await factory.ResetDatabaseAsync();
        var (admin, csrf) = await CreateReadyAdminAsync();
        using (admin)
        {
            for (var index = 0; index < 2; index++)
            {
                var response = await SendAsync(
                    admin,
                    HttpMethod.Post,
                    "/api/v1/users",
                    new
                    {
                        email = $"paged-{index}-{Guid.NewGuid():N}@test.local",
                        displayName = $"Paged {index}",
                        temporaryPassword = "TemporaryUser123!",
                        roles = new[] { "Member" }
                    },
                    csrf);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }

            var users = await ReadAsync<PagedDto<UserDto>>(
                await admin.GetAsync("/api/v1/users?page=1&pageSize=1"));
            Assert.Single(users.Items);
            Assert.Equal(1, users.PageSize);
            Assert.True(users.Total >= 3);

            var roles = await ReadAsync<PagedDto<RoleDto>>(
                await admin.GetAsync("/api/v1/roles?page=1&pageSize=1"));
            Assert.Single(roles.Items);
            Assert.True(roles.Total >= 2);

            var audit = await ReadAsync<PagedDto<AuditDto>>(
                await admin.GetAsync("/api/v1/audit?page=1&pageSize=100"));
            Assert.NotEmpty(audit.Items);
            Assert.Contains(audit.Items, item => item.EventType == "admin.user.created");
            Assert.Contains(audit.Items, item => item.EventType == "auth.login");
        }
    }

    [Fact]
    public async Task ApiV1_HasNoPublicRegistrationEndpoint()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "registration@test.local", password = "Password123!" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private async Task<(HttpClient Client, CsrfDto Csrf)> CreateReadyAdminAsync()
    {
        var client = CreateClient();
        var login = await LoginAsync(client, AdminEmail, AdminPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var csrf = await GetCsrfAsync(client);
        var changed = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/change-password",
            new { currentPassword = AdminPassword, newPassword = "ReadyAdmin123!" },
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        return (client, await GetCsrfAsync(client));
    }

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    private static async Task<CsrfDto> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync<CsrfDto>(response);
    }

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body,
        CsrfDto csrf)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(csrf.HeaderName, csrf.RequestToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return client.SendAsync(request);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return Assert.IsType<T>(value);
    }

    private sealed record CsrfDto(string HeaderName, string RequestToken);
    private sealed record UserDto(
        string Id,
        string Email,
        string? DisplayName,
        bool IsActive,
        bool MustChangePassword,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);
    private sealed record RoleDto(
        string Id,
        string Name,
        bool IsProtected,
        IReadOnlyList<string> Permissions);
    private sealed record AuditDto(long Id, string EventType, string Outcome);
    private sealed record PagedDto<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int Total);
}
