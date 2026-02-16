using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

public interface IAuthIdentityRepository
{
    Task<AuthIdentity?> GetByProviderAsync(string provider, string providerSubject, CancellationToken cancellationToken = default);
    Task AddAsync(AuthIdentity identity, CancellationToken cancellationToken = default);
}
