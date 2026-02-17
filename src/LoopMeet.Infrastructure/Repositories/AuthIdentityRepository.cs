using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Operator = Supabase.Postgrest.Constants.Operator;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class AuthIdentityRepository : IAuthIdentityRepository
{
    private readonly Client _client;

    public AuthIdentityRepository(Client client)
    {
        _client = client;
    }

    public Task<AuthIdentity?> GetByProviderAsync(string provider, string providerSubject, CancellationToken cancellationToken = default)
    {
        return GetByProviderInternalAsync(provider, providerSubject);
    }

    public async Task AddAsync(AuthIdentity identity, CancellationToken cancellationToken = default)
    {
        var record = Map(identity);
        await _client.From<AuthIdentityRecord>().Insert(record);
    }

    private async Task<AuthIdentity?> GetByProviderInternalAsync(string provider, string providerSubject)
    {
        var response = await _client
            .From<AuthIdentityRecord>()
            .Filter("provider", Operator.Equals, provider)
            .Filter("provider_subject", Operator.Equals, providerSubject)
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    private static AuthIdentity Map(AuthIdentityRecord record)
    {
        return new AuthIdentity
        {
            Id = record.Id,
            UserId = record.UserId,
            Provider = record.Provider,
            ProviderSubject = record.ProviderSubject,
            CreatedAt = record.CreatedAt
        };
    }

    private static AuthIdentityRecord Map(AuthIdentity identity)
    {
        return new AuthIdentityRecord
        {
            Id = identity.Id,
            UserId = identity.UserId,
            Provider = identity.Provider,
            ProviderSubject = identity.ProviderSubject,
            CreatedAt = identity.CreatedAt
        };
    }
}
