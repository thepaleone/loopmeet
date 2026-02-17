using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;

namespace LoopMeet.Api.Services.Invitations;

public sealed class InvitationQueryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly IInvitationRepository _invitationRepository;
    private readonly ICacheService _cacheService;

    public InvitationQueryService(IInvitationRepository invitationRepository, ICacheService cacheService)
    {
        _invitationRepository = invitationRepository;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<InvitationResponse>> ListPendingAsync(string email, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"pending-invitations:{email}";
        var cached = await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var invitations = await _invitationRepository.ListPendingByEmailAsync(email, cancellationToken);
            return invitations
                .OrderBy(invitation => invitation.CreatedAt)
                .Select(invitation => new InvitationResponse
                {
                    Id = invitation.Id,
                    GroupId = invitation.GroupId,
                    InvitedEmail = invitation.InvitedEmail,
                    Status = invitation.Status
                })
                .ToList();
        });

        return cached ?? new List<InvitationResponse>();
    }
}
