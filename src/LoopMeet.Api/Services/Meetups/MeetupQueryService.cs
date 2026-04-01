using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;

namespace LoopMeet.Api.Services.Meetups;

public sealed class MeetupQueryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly IMeetupRepository _meetupRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MeetupQueryService> _logger;

    public MeetupQueryService(
        IMeetupRepository meetupRepository,
        ICacheService cacheService,
        ILogger<MeetupQueryService> logger)
    {
        _meetupRepository = meetupRepository;
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
            var items = meetups
                .Select(m => new MeetupResponse
                {
                    Id = m.Id,
                    GroupId = m.GroupId,
                    Title = m.Title,
                    ScheduledAt = m.ScheduledAt,
                    PlaceName = m.PlaceName,
                    PlaceAddress = m.PlaceAddress,
                    Latitude = m.Latitude,
                    Longitude = m.Longitude,
                    PlaceId = m.PlaceId,
                    CreatedByUserId = m.CreatedByUserId
                })
                .ToList();

            _logger.LogInformation("Loaded meetups for group {GroupId} count={Count}", groupId, items.Count);
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
            var items = meetups
                .Select(m => new UpcomingMeetupResponse
                {
                    Id = m.Meetup.Id,
                    GroupId = m.Meetup.GroupId,
                    Title = m.Meetup.Title,
                    ScheduledAt = m.Meetup.ScheduledAt,
                    PlaceName = m.Meetup.PlaceName,
                    PlaceAddress = m.Meetup.PlaceAddress,
                    Latitude = m.Meetup.Latitude,
                    Longitude = m.Meetup.Longitude,
                    PlaceId = m.Meetup.PlaceId,
                    CreatedByUserId = m.Meetup.CreatedByUserId,
                    GroupName = m.GroupName
                })
                .ToList();

            _logger.LogInformation("Loaded upcoming meetups for user {UserId} count={Count}", userId, items.Count);
            return new UpcomingMeetupsResponse { Meetups = items };
        }) ?? new UpcomingMeetupsResponse();
    }
}
