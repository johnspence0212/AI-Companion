using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class SavedViewTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Duplicate_LetsUserChangeFilters_WithoutEditingSystemOriginal()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var listed = await client.GetFromJsonAsync<List<SavedViewDto>>(
                "/api/v1/views?entityType=Documents",
                JsonOptions);
            var system = Assert.Single(listed!, view => view.Id == SystemSavedViews.DocumentsAll);
            Assert.True(system.IsSystem);
            Assert.Empty(system.Filters);

            var forbidden = await MutateAsync(
                client,
                csrf,
                HttpMethod.Put,
                $"/api/v1/views/{system.Id}",
                new { name = "All Documents", filters = new Dictionary<string, string> { ["folder"] = "unfiled" } });
            Assert.Equal(HttpStatusCode.BadRequest, forbidden.StatusCode);

            var duplicated = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/views/{system.Id}/duplicate",
                new { name = "Reading list" });
            Assert.Equal(HttpStatusCode.Created, duplicated.StatusCode);
            var copy = await duplicated.Content.ReadFromJsonAsync<SavedViewDto>(JsonOptions);
            Assert.False(copy!.IsSystem);
            Assert.Equal("Reading list", copy.Name);
            Assert.Empty(copy.Filters);

            var updated = await MutateAsync(
                client,
                csrf,
                HttpMethod.Put,
                $"/api/v1/views/{copy.Id}",
                new
                {
                    name = "Reading list",
                    filters = new Dictionary<string, string> { ["folder"] = "unfiled" }
                });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            var changed = await updated.Content.ReadFromJsonAsync<SavedViewDto>(JsonOptions);
            Assert.Equal("unfiled", changed!.Filters["folder"]);

            var after = await client.GetFromJsonAsync<List<SavedViewDto>>(
                "/api/v1/views?entityType=Documents",
                JsonOptions);
            var original = Assert.Single(after!, view => view.Id == SystemSavedViews.DocumentsAll);
            Assert.True(original.IsSystem);
            Assert.Empty(original.Filters);
            var stored = Assert.Single(after!, view => view.Id == copy.Id);
            Assert.Equal("unfiled", stored.Filters["folder"]);
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
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/auth/change-password",
                new { currentPassword = AdminPassword, newPassword = "ReadyAdmin123!" })).StatusCode);
        return (client, await GetCsrfAsync(client));
    }

    private static async Task<CsrfDto> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CsrfDto>(JsonOptions))!;
    }

    private static Task<HttpResponseMessage> MutateAsync(
        HttpClient client,
        CsrfDto csrf,
        HttpMethod method,
        string path,
        object? body)
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
}
