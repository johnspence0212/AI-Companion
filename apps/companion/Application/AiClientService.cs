using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record AiClientCreated(Guid Id, string Name, string Secret, DateTimeOffset CreatedAt);

public sealed record AiClientSummary(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset? ArchivedAt);

public sealed class AiClientService(EnterpriseDbContext db, ISecurityAuditService audit, TimeProvider time)
{
    public async Task<AiClientCreated> CreateAsync(string ownerUserId, string name, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var secret = AiClientAuthenticationHandler.CreateSecret();
        var client = new AiClient
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = name.Trim(),
            SecretHash = AiClientAuthenticationHandler.HashSecret(secret),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<AiClient>().Add(client);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("aiclient.created", "succeeded", client.Id.ToString(), $"name={client.Name}");
        return new AiClientCreated(
            client.Id,
            client.Name,
            AiClientAuthenticationHandler.FormatToken(client.Id, secret),
            client.CreatedAt);
    }

    public async Task<IReadOnlyList<AiClientSummary>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Set<AiClient>()
            .AsNoTracking()
            .OrderBy(client => client.Name)
            .Select(client => new AiClientSummary(client.Id, client.Name, client.CreatedAt, client.ArchivedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await db.Set<AiClient>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (client is null)
        {
            return false;
        }

        if (client.ArchivedAt is null)
        {
            client.ArchivedAt = time.GetUtcNow();
            client.UpdatedAt = client.ArchivedAt.Value;
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("aiclient.revoked", "succeeded", client.Id.ToString());
        }

        return true;
    }
}
