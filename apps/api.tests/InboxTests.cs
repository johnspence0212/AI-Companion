using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class InboxTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Process_CanCreateDocument_OrAttachExistingIssue()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var captured = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/inbox",
                new { text = "Write the importer notes\n\nKeep the ESPN mapping." });
            Assert.Equal(HttpStatusCode.Created, captured.StatusCode);
            var note = await captured.Content.ReadFromJsonAsync<InboxItemDto>(JsonOptions);
            Assert.Equal(InboxStatus.Open, note!.Status);

            var processed = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/inbox/{note.Id}/process",
                new { createDocument = true, title = "Importer notes" });
            Assert.Equal(HttpStatusCode.OK, processed.StatusCode);
            var asDocument = await processed.Content.ReadFromJsonAsync<InboxItemDto>(JsonOptions);
            Assert.Equal(InboxStatus.Processed, asDocument!.Status);
            Assert.NotNull(asDocument.DocumentId);
            Assert.Null(asDocument.IssueId);

            var document = await client.GetFromJsonAsync<DocumentDto>(
                $"/api/v1/documents/{asDocument.DocumentId}",
                JsonOptions);
            Assert.Equal("Importer notes", document!.Title);
            Assert.Equal("Write the importer notes\n\nKeep the ESPN mapping.", document.Body);

            var projectId = await BootstrapProjectIdAsync(client);
            var issueResponse = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/projects/{projectId}/issues",
                new { title = "Existing work", status = IssueStatus.Ready });
            var issue = await issueResponse.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);
            Assert.NotNull(issue);

            var thought = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/inbox",
                new { text = "This belongs on the existing Issue." })).Content
                .ReadFromJsonAsync<InboxItemDto>(JsonOptions);

            var attached = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/inbox/{thought!.Id}/process",
                new { issueId = issue.Id });
            Assert.Equal(HttpStatusCode.OK, attached.StatusCode);
            var linked = await attached.Content.ReadFromJsonAsync<InboxItemDto>(JsonOptions);
            Assert.Equal(InboxStatus.Processed, linked!.Status);
            Assert.Equal(issue.Id, linked.IssueId);
            Assert.Null(linked.DocumentId);

            var unchanged = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{issue.Id}", JsonOptions);
            Assert.Equal(issue.Title, unchanged!.Title);
            Assert.Equal(issue.Status, unchanged.Status);
            Assert.Equal(issue.Version, unchanged.Version);
            Assert.Equal(issue.Description, unchanged.Description);
        }
    }

    [Fact]
    public async Task Archive_DismissesWithoutCreatingATarget()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var captured = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/inbox",
                new { text = "Noise I will dismiss." })).Content
                .ReadFromJsonAsync<InboxItemDto>(JsonOptions);

            var archived = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/inbox/{captured!.Id}/archive",
                null);
            Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
            var dismissed = await archived.Content.ReadFromJsonAsync<InboxItemDto>(JsonOptions);
            Assert.Equal(InboxStatus.Archived, dismissed!.Status);
            Assert.Null(dismissed.DocumentId);
            Assert.Null(dismissed.IssueId);
            Assert.NotNull(dismissed.ArchivedAt);

            var open = await client.GetFromJsonAsync<List<InboxItemDto>>("/api/v1/inbox", JsonOptions);
            Assert.DoesNotContain(open!, item => item.Id == captured.Id);

            var stored = await client.GetFromJsonAsync<InboxItemDto>($"/api/v1/inbox/{captured.Id}", JsonOptions);
            Assert.Equal(InboxStatus.Archived, stored!.Status);
            Assert.Null(stored.DocumentId);
            Assert.Null(stored.IssueId);

            var documents = await client.GetFromJsonAsync<List<DocumentDto>>("/api/v1/documents", JsonOptions);
            Assert.DoesNotContain(documents!, item => item.Body.Contains("Noise I will dismiss.", StringComparison.Ordinal));
        }
    }

    private async Task<Guid> BootstrapProjectIdAsync(HttpClient client)
    {
        var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/api/v1/projects", JsonOptions);
        Assert.NotNull(projects);
        Assert.NotEmpty(projects);
        return projects[0].Id;
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
