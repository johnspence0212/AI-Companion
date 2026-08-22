using System.Net;
using System.Net.Http.Json;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class CompanionOwnershipTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";

    [Fact]
    public async Task ForeignProjectId_ReturnsNotFound_NotForbidden()
    {
        await factory.ResetDatabaseAsync();
        var (admin, csrf) = await CreateReadyAdminAsync();
        using (admin)
        {
            var email = $"owner-{Guid.NewGuid():N}@test.local";
            var create = await SendAsync(
                admin,
                HttpMethod.Post,
                "/api/v1/users",
                new
                {
                    email,
                    displayName = "Project Owner",
                    temporaryPassword = "TemporaryUser123!",
                    roles = new[] { "Member" }
                },
                csrf);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var owner = await create.Content.ReadFromJsonAsync<CreatedUserDto>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(owner);
            Assert.False(string.IsNullOrWhiteSpace(owner.Id));

            var projectId = Guid.NewGuid();
            var contextId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EnterpriseDbContext>();
                db.Set<Document>().Add(new Document
                {
                    Id = contextId,
                    OwnerUserId = owner.Id,
                    Title = "Context",
                    IsProjectContext = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                db.Set<Project>().Add(new Project
                {
                    Id = projectId,
                    OwnerUserId = owner.Id,
                    Name = "Owned",
                    Slug = "owned",
                    ContextDocumentId = contextId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await db.SaveChangesAsync();
            }

            var forbiddenOrHidden = await admin.GetAsync($"/api/v1/projects/{projectId}");
            Assert.Equal(HttpStatusCode.NotFound, forbiddenOrHidden.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, forbiddenOrHidden.StatusCode);

            using var member = factory.CreateClient(
                new WebApplicationFactoryClientOptions { HandleCookies = true });
            Assert.Equal(
                HttpStatusCode.OK,
                (await member.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    new { email, password = "TemporaryUser123!" })).StatusCode);
            var memberCsrf = await GetCsrfAsync(member);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendAsync(
                    member,
                    HttpMethod.Post,
                    "/api/v1/auth/change-password",
                    new { currentPassword = "TemporaryUser123!", newPassword = "ReadyMember123!" },
                    memberCsrf)).StatusCode);

            var visible = await member.GetAsync($"/api/v1/projects/{projectId}");
            Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        }
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private async Task<(HttpClient Client, CsrfDto Csrf)> CreateReadyAdminAsync()
    {
        var client = CreateClient();
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = AdminEmail, password = AdminPassword })).StatusCode);
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

    private static async Task<CsrfDto> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CsrfDto>())!;
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

    private sealed record CsrfDto(string HeaderName, string RequestToken);

    private sealed record CreatedUserDto(string Id);
}
