using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class AiClientMcpTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task AiClientSecret_AuthenticatesMcp_AndIsRejectedAfterRevoke()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var createdResponse = await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/ai-clients",
                new { name = "Cursor" },
                csrf);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = await createdResponse.Content.ReadFromJsonAsync<CreatedAiClientDto>(JsonOptions);
            Assert.NotNull(created);
            Assert.False(string.IsNullOrWhiteSpace(created.Secret));
            Assert.DoesNotContain(created.Secret, created.SecretHash ?? string.Empty, StringComparison.Ordinal);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EnterpriseDbContext>();
                var stored = await db.Set<AiClient>().IgnoreQueryFilters()
                    .SingleAsync(item => item.Id == created.Id);
                Assert.NotEqual(created.Secret, stored.SecretHash);
                Assert.DoesNotContain(created.Secret.Split('.')[1], stored.SecretHash, StringComparison.OrdinalIgnoreCase);
            }

            var list = await client.GetAsync("/api/v1/ai-clients");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            var listed = await list.Content.ReadAsStringAsync();
            Assert.DoesNotContain(created.Secret, listed, StringComparison.Ordinal);

            using var mcp = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var accepted = await PostMcpAsync(mcp, created.Secret);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

            using var cookieOnly = factory.CreateClient(
                new WebApplicationFactoryClientOptions { HandleCookies = true });
            Assert.Equal(
                HttpStatusCode.OK,
                (await cookieOnly.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    new { email = AdminEmail, password = "ReadyAdmin123!" })).StatusCode);
            var cookieCall = await PostMcpAsync(cookieOnly, bearer: null);
            Assert.Equal(HttpStatusCode.Unauthorized, cookieCall.StatusCode);

            var revoked = await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/ai-clients/{created.Id}/revoke",
                null,
                csrf);
            Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);

            var rejected = await PostMcpAsync(mcp, created.Secret);
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
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
        return (await response.Content.ReadFromJsonAsync<CsrfDto>(JsonOptions))!;
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

    private static Task<HttpResponseMessage> PostMcpAsync(HttpClient client, string? bearer)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"tests","version":"1.0"}}}""",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return client.SendAsync(request);
    }

    private sealed record CsrfDto(string HeaderName, string RequestToken);

    private sealed record CreatedAiClientDto(Guid Id, string Name, string Secret, string? SecretHash);
}
