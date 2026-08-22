using System.Net;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public class DeployHardeningTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient(new() { HandleCookies = true });

    [Fact]
    public async Task Ready_ReturnsOk_WithSecurityAndCorrelationHeaders()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
    }

    [Fact]
    public async Task ForwardedProto_IsHonoredForHttpsDetection()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
