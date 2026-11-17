using LoopMeet.Api.Services.Cache;
using LoopMeet.Api.Services.Meetups;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Groups;

/// <summary>
/// Leaving a group (feature 023 US2). The database function decides; this service clears the
/// caches that would otherwise show the old membership for up to 30 seconds (research D9).
/// </summary>
public sealed class GroupMembershipCommandService
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GroupMembershipCommandService> _logger;

    public GroupMembershipCommandService(
        IMembershipRepository membershipRepository,
        ICacheService cacheService,
        ILogger<GroupMembershipCommandService> logger)
    {
        _membershipRepository = membershipRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<MembershipExitResult> LeaveAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        // Read before leaving: afterwards the leaver can no longer list the group's members.
        var members = await _membershipRepository.ListMembersAsync(groupId, cancellationToken);

        var result = await _membershipRepository.LeaveAsync(userId, groupId, cancellationToken);
        if (result != MembershipExitResult.Done)
        {
            _logger.LogWarning("GroupLeaveRefused for {UserId} in {GroupId}: {Result}", userId, groupId, result);
            return result;
        }

        await InvalidateAsync(groupId, userId, members);
        _logger.LogInformation("GroupLeft {UserId} from {GroupId}", userId, groupId);
        return result;
    }

    private async Task InvalidateAsync(Guid groupId, Guid personId, IReadOnlyList<Membership> members)
    {
        await _cacheService.RemoveAsync($"group-detail:{groupId}");
        await _cacheService.RemoveAsync($"meetups:{groupId}");
        await _cacheService.RemoveAsync(MeetupQueryService.HomeCacheKey(personId));
        await _cacheService.RemoveAsync($"groups:{personId}");
        foreach (var member in members.Where(member => member.UserId != personId))
        {
            await _cacheService.RemoveAsync($"groups:{member.UserId}");
        }
    }
}
