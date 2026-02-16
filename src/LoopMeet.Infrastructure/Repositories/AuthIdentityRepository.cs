using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class AuthIdentityRepository : IAuthIdentityRepository
{
    private readonly LoopMeetDbContext _dbContext;

    public AuthIdentityRepository(LoopMeetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AuthIdentity?> GetByProviderAsync(string provider, string providerSubject, CancellationToken cancellationToken = default)
    {
        return _dbContext.AuthIdentities.FirstOrDefaultAsync(
            identity => identity.Provider == provider && identity.ProviderSubject == providerSubject,
            cancellationToken);
    }

    public async Task AddAsync(AuthIdentity identity, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuthIdentities.AddAsync(identity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
