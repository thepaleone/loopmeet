namespace LoopMeet.Api.Contracts;

public sealed class MeetupResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; init; }
    public string? PlaceName { get; init; }
    public string? PlaceAddress { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? PlaceId { get; init; }
    public string? Timezone { get; init; }
    public Guid CreatedByUserId { get; init; }
}

public sealed class CreateMeetupRequest
{
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; init; }
    public string? PlaceName { get; init; }
    public string? PlaceAddress { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? PlaceId { get; init; }
    public string? Timezone { get; init; }
}

public sealed class UpdateMeetupRequest
{
    public string? Title { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
    public string? PlaceName { get; init; }
    public string? PlaceAddress { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? PlaceId { get; init; }
    public string? Timezone { get; init; }
}

public sealed class MeetupsResponse
{
    public IReadOnlyList<MeetupListItemResponse> Meetups { get; init; } = Array.Empty<MeetupListItemResponse>();
}

/// <summary>
/// Read model for both meetup list endpoints. Carries display data resolved at
/// read time (group name, organizer display name, group owner) so a client can
/// present a meetup — and decide whether to offer editing — from one call.
/// <see cref="MeetupResponse"/> remains the create/update echo and deliberately
/// carries none of it.
/// </summary>
public sealed class MeetupListItemResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; init; }
    public string? PlaceName { get; init; }
    public string? PlaceAddress { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? PlaceId { get; init; }
    public string? Timezone { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string GroupName { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the member who created the meetup. Empty when that user is
    /// no longer a member of the group, or has no profile row; callers supply
    /// their own placeholder rather than receiving one here.
    /// </summary>
    public string CreatedByDisplayName { get; init; } = string.Empty;

    /// <summary>Owner of the meetup's group — the sole input to edit gating.</summary>
    public Guid GroupOwnerUserId { get; init; }
}

public sealed class UpcomingMeetupsResponse
{
    public IReadOnlyList<MeetupListItemResponse> Meetups { get; init; } = Array.Empty<MeetupListItemResponse>();
}
