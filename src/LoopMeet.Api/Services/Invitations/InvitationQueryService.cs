using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Invitations;

public sealed class InvitationQueryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly IInvitationRepository _invitationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<InvitationQueryService> _logger;

    public InvitationQueryService(
        IInvitationRepository invitationRepository,
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        ICacheService cacheService,
        ILogger<InvitationQueryService> logger)
    {
        _invitationRepository = invitationRepository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InvitationResponse>> ListPendingAsync(string email, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"pending-invitations:{email}";
        _logger.LogInformation("Loading pending invitations for current user");
        var cached = await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var invitations = await _invitationRepository.ListPendingByEmailAsync(email, cancellationToken);
            var groupIds = invitations.Select(invitation => invitation.GroupId).Distinct().ToList();
            var groups = await _groupRepository.ListByIdsAsync(groupIds, cancellationToken);
            var groupLookup = groups.ToDictionary(group => group.Id, group => group);
            var ownerIds = groups.Select(group => group.OwnerUserId).Distinct().ToList();
            var senderIds = invitations
                .Where(invitation => invitation.InvitedByUserId.HasValue)
                .Select(invitation => invitation.InvitedByUserId!.Value)
                .Distinct()
                .ToList();
            var lookupUserIds = ownerIds
                .Concat(senderIds)
                .Distinct()
                .ToList();
            var users = await _userRepository.ListByIdsAsync(lookupUserIds, cancellationToken);
            var userLookup = users.ToDictionary(user => user.Id, user => user);
            var responses = invitations
                .OrderBy(invitation => invitation.CreatedAt)
                .Select(invitation =>
                {
                    groupLookup.TryGetValue(invitation.GroupId, out var group);
                    User? owner = null;
                    if (group is not null)
                    {
                        userLookup.TryGetValue(group.OwnerUserId, out owner);
                    }
                    User? sender = null;
                    if (invitation.InvitedByUserId.HasValue)
                    {
                        userLookup.TryGetValue(invitation.InvitedByUserId.Value, out sender);
                    }

                    sender ??= owner;

                    return new InvitationResponse
                    {
                        Id = invitation.Id,
                        GroupId = invitation.GroupId,
                        GroupName = group?.Name ?? string.Empty,
                        OwnerName = owner?.DisplayName ?? string.Empty,
                        OwnerEmail = owner?.Email ?? string.Empty,
                        SenderName = sender?.DisplayName ?? string.Empty,
                        SenderEmail = sender?.Email ?? string.Empty,
                        InvitedEmail = invitation.InvitedEmail,
                        Status = invitation.Status,
                        CreatedAt = invitation.CreatedAt
                    };
                })
                .ToList();
            _logger.LogInformation("Loaded pending invitations count={Count}", responses.Count);
            return responses;
        });

        return cached ?? new List<InvitationResponse>();
    }
}
