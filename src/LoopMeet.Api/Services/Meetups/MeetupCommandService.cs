using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Meetups;

public enum MeetupCommandStatus
{
    Success,
    NotFound,
    InvalidTitle,
    InvalidSchedule,
    InvalidLocation
}

public sealed record MeetupCommandResult(MeetupCommandStatus Status, MeetupResponse? Meetup);

public sealed class MeetupCommandService
{
    private readonly IMeetupRepository _meetupRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MeetupCommandService> _logger;

    public MeetupCommandService(
        IMeetupRepository meetupRepository,
        ICacheService cacheService,
        ILogger<MeetupCommandService> logger)
    {
        _meetupRepository = meetupRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<MeetupCommandResult> CreateAsync(
        Guid userId,
        Guid groupId,
        CreateMeetupRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedTitle = request.Title.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTitle) || trimmedTitle.Length > 200)
        {
            _logger.LogWarning("Create meetup invalid title for group {GroupId} by {UserId}", groupId, userId);
            return new MeetupCommandResult(MeetupCommandStatus.InvalidTitle, null);
        }

        if (request.ScheduledAt <= DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Create meetup invalid schedule for group {GroupId} by {UserId}", groupId, userId);
            return new MeetupCommandResult(MeetupCommandStatus.InvalidSchedule, null);
        }

        if (!string.IsNullOrWhiteSpace(request.PlaceName) && (request.Latitude is null || request.Longitude is null))
        {
            _logger.LogWarning("Create meetup invalid location for group {GroupId} by {UserId}", groupId, userId);
            return new MeetupCommandResult(MeetupCommandStatus.InvalidLocation, null);
        }

        _logger.LogInformation("Creating meetup for group {GroupId} by {UserId}", groupId, userId);
        var now = DateTimeOffset.UtcNow;
        var meetup = new Meetup
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            CreatedByUserId = userId,
            Title = trimmedTitle,
            ScheduledAt = request.ScheduledAt,
            PlaceName = request.PlaceName,
            PlaceAddress = request.PlaceAddress,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PlaceId = request.PlaceId,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _meetupRepository.AddAsync(meetup, cancellationToken);

        await _cacheService.RemoveAsync($"meetups:{groupId}");
        _logger.LogInformation("Created meetup {MeetupId} for group {GroupId} by {UserId}", created.Id, groupId, userId);

        return new MeetupCommandResult(MeetupCommandStatus.Success, MapResponse(created));
    }

    public async Task<MeetupCommandResult> UpdateAsync(
        Guid groupId,
        Guid meetupId,
        UpdateMeetupRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _meetupRepository.GetByIdAsync(meetupId, cancellationToken);
        if (existing is null || existing.GroupId != groupId)
        {
            _logger.LogWarning("Update meetup not found {MeetupId} in group {GroupId}", meetupId, groupId);
            return new MeetupCommandResult(MeetupCommandStatus.NotFound, null);
        }

        if (request.Title is not null)
        {
            var trimmedTitle = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(trimmedTitle) || trimmedTitle.Length > 200)
            {
                _logger.LogWarning("Update meetup invalid title {MeetupId} in group {GroupId}", meetupId, groupId);
                return new MeetupCommandResult(MeetupCommandStatus.InvalidTitle, null);
            }
            existing.Title = trimmedTitle;
        }

        if (request.ScheduledAt is not null)
        {
            if (request.ScheduledAt.Value <= DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Update meetup invalid schedule {MeetupId} in group {GroupId}", meetupId, groupId);
                return new MeetupCommandResult(MeetupCommandStatus.InvalidSchedule, null);
            }
            existing.ScheduledAt = request.ScheduledAt.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.PlaceName) && (request.Latitude is null || request.Longitude is null))
        {
            _logger.LogWarning("Update meetup invalid location {MeetupId} in group {GroupId}", meetupId, groupId);
            return new MeetupCommandResult(MeetupCommandStatus.InvalidLocation, null);
        }

        existing.PlaceName = request.PlaceName;
        existing.PlaceAddress = request.PlaceAddress;
        existing.Latitude = request.Latitude;
        existing.Longitude = request.Longitude;
        existing.PlaceId = request.PlaceId;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _meetupRepository.UpdateAsync(existing, cancellationToken);

        await _cacheService.RemoveAsync($"meetups:{groupId}");
        _logger.LogInformation("Updated meetup {MeetupId} in group {GroupId}", meetupId, groupId);

        return new MeetupCommandResult(MeetupCommandStatus.Success, MapResponse(existing));
    }

    public async Task<MeetupCommandResult> DeleteAsync(
        Guid groupId,
        Guid meetupId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _meetupRepository.GetByIdAsync(meetupId, cancellationToken);
        if (existing is null || existing.GroupId != groupId)
        {
            _logger.LogWarning("Delete meetup not found {MeetupId} in group {GroupId}", meetupId, groupId);
            return new MeetupCommandResult(MeetupCommandStatus.NotFound, null);
        }

        await _meetupRepository.DeleteAsync(meetupId, cancellationToken);

        await _cacheService.RemoveAsync($"meetups:{groupId}");
        _logger.LogInformation("Deleted meetup {MeetupId} from group {GroupId}", meetupId, groupId);

        return new MeetupCommandResult(MeetupCommandStatus.Success, null);
    }

    private static MeetupResponse MapResponse(Meetup meetup)
    {
        return new MeetupResponse
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
            CreatedByUserId = meetup.CreatedByUserId
        };
    }
}
