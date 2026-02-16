using LoopMeet.Api.Contracts;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Api.Services.Invitations;

public sealed class InvitationQueryService
{
    private readonly LoopMeetDbContext _dbContext;

    public InvitationQueryService(LoopMeetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<InvitationResponse>> ListPendingAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations
            .Where(invitation => invitation.InvitedEmail == email && invitation.Status == "pending")
            .OrderBy(invitation => invitation.CreatedAt)
            .Select(invitation => new InvitationResponse
            {
                Id = invitation.Id,
                GroupId = invitation.GroupId,
                InvitedEmail = invitation.InvitedEmail,
                Status = invitation.Status
            })
            .ToListAsync(cancellationToken);
    }
}
