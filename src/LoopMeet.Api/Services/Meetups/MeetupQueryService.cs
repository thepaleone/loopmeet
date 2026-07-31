using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Meetups;

public sealed class MeetupQueryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly IMeetupRepository _meetupRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MeetupQueryService> _logger;

    public MeetupQueryService(
        IMeetupRepository meetupRepository,
        IGroupRepository groupRepository,
        IMembershipRepository membershipRepository,
        IUserRepository userRepository,
        ICacheService cacheService,
        ILogger<MeetupQueryService> logger)
    {
        _meetupRepository = meetupRepository;
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<MeetupsResponse> GetGroupMeetupsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"meetups:{groupId}";
        _logger.LogInformation("Loading meetups for group {GroupId}", groupId);
        return await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var meetups = await _meetupRepository.ListUpcomingByGroupAsync(groupId, cancellationToken);
            var items = await ProjectAsync(meetups, cancellationToken);

            _logger.LogInformation(
                "Loaded meetups for group {GroupId} count={Count} organizersResolved={Resolved} organizersUnresolved={Unresolved}",
                groupId,
                items.Count,
                items.Count(item => !string.IsNullOrEmpty(item.CreatedByDisplayName)),
                items.Count(item => string.IsNullOrEmpty(item.CreatedByDisplayName)));
            return new MeetupsResponse { Meetups = items };
        }) ?? new MeetupsResponse();
    }

    public async Task<UpcomingMeetupsResponse> GetUpcomingForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"home-meetups:{userId}";
        _logger.LogInformation("Loading upcoming meetups for user {UserId}", userId);
        return await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var meetups = await _meetupRepository.ListUpcomingByUserAsync(userId, cancellationToken);
            var items = await ProjectAsync(meetups, cancellationToken);

            _logger.LogInformation(
                "Loaded upcoming meetups for user {UserId} count={Count} organizersResolved={Resolved} organizersUnresolved={Unresolved}",
                userId,
                items.Count,
                items.Count(item => !string.IsNullOrEmpty(item.CreatedByDisplayName)),
                items.Count(item => string.IsNullOrEmpty(item.CreatedByDisplayName)));
            return new UpcomingMeetupsResponse { Meetups = items };
        }) ?? new UpcomingMeetupsResponse();
    }

    /// <summary>
    /// Resolves group name, group owner, and organizer display name for a set of
    /// meetups using three lookups total, independent of how many meetups there
    /// are. An organizer resolves to a name only while they are still a member of
    /// the meetup's group — group members already see each other's names in the
    /// member list, and that is the basis on which this name is disclosed.
    /// </summary>
    private async Task<List<MeetupListItemResponse>> ProjectAsync(
        IReadOnlyList<Meetup> meetups,
        CancellationToken cancellationToken)
    {
        if (meetups.Count == 0)
        {
            return new List<MeetupListItemResponse>();
        }

        var groupIds = meetups.Select(meetup => meetup.GroupId).Distinct().ToList();

        var groups = await _groupRepository.ListByIdsAsync(groupIds, cancellationToken);
        var groupLookup = groups.ToDictionary(group => group.Id);

        var memberships = await _membershipRepository.ListMembersByGroupsAsync(groupIds, cancellationToken);
        var membershipPairs = memberships
            .Select(membership => (membership.GroupId, membership.UserId))
            .ToHashSet();

        var creatorIds = meetups
            .Where(meetup => membershipPairs.Contains((meetup.GroupId, meetup.CreatedByUserId)))
            .Select(meetup => meetup.CreatedByUserId)
            .Distinct()
            .ToList();
        var creators = await _userRepository.ListByIdsAsync(creatorIds, cancellationToken);
        var creatorLookup = creators.ToDictionary(user => user.Id);

        return meetups
            .Select(meetup =>
            {
                groupLookup.TryGetValue(meetup.GroupId, out var group);

                var organizerName = string.Empty;
                if (membershipPairs.Contains((meetup.GroupId, meetup.CreatedByUserId))
                    && creatorLookup.TryGetValue(meetup.CreatedByUserId, out var creator))
                {
                    organizerName = creator.DisplayName;
                }

                return new MeetupListItemResponse
                {
                    Id = meetup.Id,
                    GroupId = meetup.GroupId,
                    Title = meetup.Title,
                    ScheduledAt = meetup.ScheduledAt,
                    PlaceName = meetup.PlaceName,
                    PlaceAddress = meetup.PlaceAddress,
                    Latitude = meetup.Latitude,
                    Longitude = meetup.Longitude,
                    PlaceId = meetup.PlaceId,
                    Timezone = meetup.Timezone,
                    CreatedByUserId = meetup.CreatedByUserId,
                    GroupName = group?.Name ?? string.Empty,
                    CreatedByDisplayName = organizerName,
                    GroupOwnerUserId = group?.OwnerUserId ?? Guid.Empty
                };
            })
            .ToList();
    }
}
