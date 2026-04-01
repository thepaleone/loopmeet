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
}

public sealed class MeetupsResponse
{
    public IReadOnlyList<MeetupResponse> Meetups { get; init; } = Array.Empty<MeetupResponse>();
}

public sealed class UpcomingMeetupResponse
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
    public Guid CreatedByUserId { get; init; }
    public string GroupName { get; init; } = string.Empty;
}

public sealed class UpcomingMeetupsResponse
{
    public IReadOnlyList<UpcomingMeetupResponse> Meetups { get; init; } = Array.Empty<UpcomingMeetupResponse>();
}
