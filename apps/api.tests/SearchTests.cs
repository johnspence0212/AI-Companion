using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class SearchTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string UniqueToken = "kalshioauthtoken99";
    private const string FencedBody =
        $$"""
        # Notes

        ```python
        def greet(name):
            return f"hello {name}"
        {{UniqueToken}}
        ```
        """;

    [Fact]
    public async Task Search_FindsUniqueTokenInsideFencedCode_AndDoesNotAlterMarkdown()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var created = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/documents",
                new { title = "Code notes", body = FencedBody, slug = "code-search-notes" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var document = await created.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
            Assert.Equal(FencedBody, document!.Body);

            var before = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{document.Id}", JsonOptions);
            Assert.Equal(FencedBody, before!.Body);

            var searchResponse = await client.GetAsync($"/api/v1/search?q={UniqueToken}&type=document");
            var searchBody = await searchResponse.Content.ReadAsStringAsync();
            Assert.True(searchResponse.IsSuccessStatusCode, searchBody);
            var results = JsonSerializer.Deserialize<SearchResultsDto>(searchBody, JsonOptions);
            Assert.NotNull(results);
            Assert.Contains(results.Documents, hit => hit.Id == document.Id);
            Assert.Empty(results.Projects);
            Assert.Empty(results.Issues);

            var after = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{document.Id}", JsonOptions);
            Assert.Equal(FencedBody, after!.Body);
            Assert.Equal(before.RevisionId, after.RevisionId);
            Assert.Equal(before.Title, after.Title);
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
