using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Meetups;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Meetups;

public sealed class MeetupQueryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    public const int DefaultPastPageSize = 20;
    private readonly IMeetupRepository _meetupRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MeetupQueryService> _logger;

    public MeetupQueryService(
        IMeetupRepository meetupRepository,
        IGroupRepository groupRepository,
        IMembershipRepository membershipRepository,
        IUserRepository userRepository,
        ICacheService cacheService,
        TimeProvider timeProvider,
        ILogger<MeetupQueryService> logger)
    {
        _meetupRepository = meetupRepository;
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _cacheService = cacheService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<MeetupsResponse> GetGroupMeetupsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"meetups:{groupId}";
        _logger.LogInformation("Loading meetups for group {GroupId}", groupId);
        return await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var now = _timeProvider.GetUtcNow();
            var meetups = await _meetupRepository.ListUpcomingByGroupAsync(groupId, now, cancellationToken);
            var items = await ProjectAsync(meetups, now, cancellationToken);

            _logger.LogInformation(
                "Loaded meetups for group {GroupId} count={Count} organizersResolved={Resolved} organizersUnresolved={Unresolved}",
                groupId,
                items.Count,
                items.Count(item => !string.IsNullOrEmpty(item.CreatedByDisplayName)),
                items.Count(item => string.IsNullOrEmpty(item.CreatedByDisplayName)));
            return new MeetupsResponse { Meetups = items };
        }) ?? new MeetupsResponse();
    }

    /// <summary>The per-user Home list key; MeetupCommandService clears it for every member on a change.</summary>
    public static string HomeCacheKey(Guid userId) => $"home-meetups:{userId}";

    public async Task<UpcomingMeetupsResponse> GetUpcomingForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = HomeCacheKey(userId);
        _logger.LogInformation("Loading upcoming meetups for user {UserId}", userId);
        return await _cacheService.GetOrSetAsync(cacheKey, CacheTtl, async () =>
        {
            var now = _timeProvider.GetUtcNow();
            var meetups = await _meetupRepository.ListUpcomingByUserAsync(userId, now, cancellationToken);
            var items = await ProjectAsync(meetups, now, cancellationToken);

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
    /// Newest-first page of a group's past meetups. Only the default-sized first page
    /// is cached; requests carrying a cursor always read through.
    /// </summary>
    public async Task<PastMeetupsResponse> GetPastGroupMeetupsAsync(
        Guid groupId,
        MeetupPageCursor? before,
        int limit,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Loading past meetups for group {GroupId} cursor={HasCursor} limit={Limit}",
            groupId,
            before is not null,
            limit);

        async Task<PastMeetupsResponse> LoadAsync()
        {
            var now = _timeProvider.GetUtcNow();
            var page = await _meetupRepository.ListPastByGroupAsync(groupId, now, before, limit, cancellationToken);
            var items = await ProjectAsync(page.Items, now, cancellationToken);

            _logger.LogInformation(
                "Loaded past meetups for group {GroupId} count={Count} hasMore={HasMore}",
                groupId,
                items.Count,
                page.NextCursor is not null);
            return new PastMeetupsResponse
            {
                Meetups = items,
                NextCursor = page.NextCursor is { } next ? MeetupCursorCodec.Encode(next) : null
            };
        }

        if (before is not null || limit != DefaultPastPageSize)
        {
            return await LoadAsync();
        }

        return await _cacheService.GetOrSetAsync($"meetups:past:{groupId}", CacheTtl, LoadAsync)
            ?? new PastMeetupsResponse();
    }

    /// <summary>
    /// One meetup of the group, upcoming or past; null when it does not exist or
    /// belongs to another group. Not cached, so it reflects edits immediately.
    /// </summary>
    public async Task<MeetupListItemResponse?> GetGroupMeetupAsync(
        Guid groupId,
        Guid meetupId,
        CancellationToken cancellationToken = default)
    {
        var meetup = await _meetupRepository.GetByIdAsync(meetupId, cancellationToken);
        if (meetup is null || meetup.GroupId != groupId)
        {
            return null;
        }

        var items = await ProjectAsync(new[] { meetup }, _timeProvider.GetUtcNow(), cancellationToken);
        return items[0];
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
        DateTimeOffset now,
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
                    GroupOwnerUserId = group?.OwnerUserId ?? Guid.Empty,
                    IsPast = MeetupLifecycle.IsPast(meetup.ScheduledAt, meetup.Timezone, now)
                };
            })
            .ToList();
    }
}
