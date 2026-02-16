using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private readonly LoopMeetDbContext _dbContext;

    public InvitationRepository(LoopMeetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Invitations.FirstOrDefaultAsync(invitation => invitation.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Invitation>> ListPendingByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations
            .Where(invitation => invitation.InvitedEmail == email && invitation.Status == "pending")
            .OrderBy(invitation => invitation.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsPendingForEmailAsync(Guid groupId, string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Invitations.AnyAsync(invitation => invitation.GroupId == groupId && invitation.InvitedEmail == email && invitation.Status == "pending", cancellationToken);
    }

    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Invitations.AddAsync(invitation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Update(invitation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
