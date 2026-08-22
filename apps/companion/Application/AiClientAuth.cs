using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseStarter.Companion.Application;

public static class AiClientAuth
{
    public const string Scheme = "AiClient";
    public const string Policy = "McpAiClient";
    public const string ClientIdClaim = "aiclient.id";
}

public sealed class AiClientAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    EnterpriseDbContext db,
    ISecurityAuditService audit,
    TimeProvider time)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim();
        if (!TryParse(token, out var clientId, out var secret))
        {
            await audit.WriteAsync("aiclient.auth.denied", "failed", details: "malformed-token");
            return AuthenticateResult.Fail("Invalid AI Client credential.");
        }

        var client = await db.Set<AiClient>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == clientId);
        if (client is null || client.ArchivedAt is not null || !VerifySecret(secret, client.SecretHash))
        {
            await audit.WriteAsync(
                "aiclient.auth.denied",
                "failed",
                client?.Id.ToString(),
                client is null ? "unknown-client" : client.ArchivedAt is not null ? "revoked" : "bad-secret");
            return AuthenticateResult.Fail("Invalid AI Client credential.");
        }

        client.LastUsedAt = time.GetUtcNow();
        await db.SaveChangesAsync();

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, client.OwnerUserId),
                new Claim(AiClientAuth.ClientIdClaim, client.Id.ToString("D")),
            ],
            AiClientAuth.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AiClientAuth.Scheme));
    }

    internal static string CreateSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    internal static string FormatToken(Guid clientId, string secret) => $"{clientId:D}.{secret}";

    internal static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes);
    }

    internal static bool TryParse(string token, out Guid clientId, out string secret)
    {
        clientId = Guid.Empty;
        secret = string.Empty;
        var separator = token.IndexOf('.', StringComparison.Ordinal);
        if (separator <= 0 || separator == token.Length - 1)
        {
            return false;
        }

        if (!Guid.TryParse(token[..separator], out clientId))
        {
            return false;
        }

        secret = token[(separator + 1)..];
        return secret.Length > 0;
    }

    private static bool VerifySecret(string secret, string storedHash) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(secret)),
            Convert.FromHexString(storedHash));
}
