using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class MembershipRepository : IMembershipRepository
{
    private readonly LoopMeetDbContext _dbContext;

    public MembershipRepository(LoopMeetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Membership?> GetByUserAndGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Memberships.FirstOrDefaultAsync(membership => membership.UserId == userId && membership.GroupId == groupId, cancellationToken);
    }

    public async Task<IReadOnlyList<Membership>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Memberships
            .Where(membership => membership.GroupId == groupId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        await _dbContext.Memberships.AddAsync(membership, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
