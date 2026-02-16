using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly LoopMeetDbContext _dbContext;

    public GroupRepository(LoopMeetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Groups.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> ListOwnedAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Groups
            .Where(group => group.OwnerUserId == ownerUserId)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> ListMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Memberships
            .Where(membership => membership.UserId == userId)
            .Join(_dbContext.Groups, membership => membership.GroupId, group => group.Id, (_, group) => group)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsNameForOwnerAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        return _dbContext.Groups.AnyAsync(group => group.OwnerUserId == ownerUserId && group.Name == name, cancellationToken);
    }

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        await _dbContext.Groups.AddAsync(group, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        _dbContext.Groups.Update(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
