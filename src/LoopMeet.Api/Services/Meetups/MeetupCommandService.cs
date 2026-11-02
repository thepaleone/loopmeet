using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Meetups;
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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MeetupCommandService> _logger;

    public MeetupCommandService(
        IMeetupRepository meetupRepository,
        ICacheService cacheService,
        TimeProvider timeProvider,
        ILogger<MeetupCommandService> logger)
    {
        _meetupRepository = meetupRepository;
        _cacheService = cacheService;
        _timeProvider = timeProvider;
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

        if (request.ScheduledAt <= _timeProvider.GetUtcNow())
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
        var now = _timeProvider.GetUtcNow();
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
            Timezone = NormalizeTimezone(request.Timezone),
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _meetupRepository.AddAsync(meetup, cancellationToken);

        await _cacheService.RemoveAsync($"meetups:{groupId}");
        await _cacheService.RemoveAsync($"meetups:past:{groupId}");
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

        // The future-time rule guards upcoming meetups only, and only when their
        // time actually changes: a past meetup may be corrected to any time, and
        // resending the stored time is not a change.
        var now = _timeProvider.GetUtcNow();
        if (request.ScheduledAt is { } requested && requested != existing.ScheduledAt)
        {
            var wasPast = MeetupLifecycle.IsPast(existing.ScheduledAt, existing.Timezone, now);
            if (!wasPast && requested <= now)
            {
                _logger.LogWarning(
                    "Update meetup invalid schedule {MeetupId} in group {GroupId} wasPast={WasPast}",
                    meetupId,
                    groupId,
                    wasPast);
                return new MeetupCommandResult(MeetupCommandStatus.InvalidSchedule, null);
            }
            existing.ScheduledAt = requested;
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
        if (request.Timezone is not null)
        {
            existing.Timezone = NormalizeTimezone(request.Timezone);
        }
        existing.UpdatedAt = now;

        await _meetupRepository.UpdateAsync(existing, cancellationToken);

        await _cacheService.RemoveAsync($"meetups:{groupId}");
        await _cacheService.RemoveAsync($"meetups:past:{groupId}");
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
        await _cacheService.RemoveAsync($"meetups:past:{groupId}");
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
            Timezone = meetup.Timezone,
            CreatedByUserId = meetup.CreatedByUserId
        };
    }

    private static string? NormalizeTimezone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > 64 ? null : trimmed;
    }
}
