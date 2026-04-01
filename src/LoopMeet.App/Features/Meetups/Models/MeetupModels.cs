namespace LoopMeet.App.Features.Meetups.Models;

public sealed class MeetupSummary
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public string? PlaceName { get; set; }
    public string? PlaceAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? PlaceId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? GroupName { get; set; }

    public bool HasLocation => !string.IsNullOrWhiteSpace(PlaceName);
    public string LocationDisplay => HasLocation ? PlaceName! : "TBD";
    public string DateDisplay => ScheduledAt.LocalDateTime.ToString("ddd, MMM d");
    public string TimeDisplay => ScheduledAt.LocalDateTime.ToString("h:mm tt");
    public string DateTimeDisplay => $"{DateDisplay} at {TimeDisplay}";
}

public sealed class MeetupsResponse
{
    public List<MeetupSummary> Meetups { get; set; } = new();
}

public sealed class UpcomingMeetupsResponse
{
    public List<MeetupSummary> Meetups { get; set; } = new();
}

public sealed class CreateMeetupRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public string? PlaceName { get; set; }
    public string? PlaceAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? PlaceId { get; set; }
}

public sealed class UpdateMeetupRequest
{
    public string? Title { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public string? PlaceName { get; set; }
    public string? PlaceAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? PlaceId { get; set; }
}
