using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryAuthIdentityRepository : IAuthIdentityRepository
{
    private readonly InMemoryStore _store;

    public InMemoryAuthIdentityRepository(InMemoryStore store)
    {
        _store = store;
    }

    public Task<AuthIdentity?> GetByProviderAsync(string provider, string providerSubject, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.AuthIdentities.FirstOrDefault(identity =>
                identity.Provider == provider && identity.ProviderSubject == providerSubject));
        }
    }

    public Task AddAsync(AuthIdentity identity, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.AuthIdentities.Add(identity);
        }

        return Task.CompletedTask;
    }
}
