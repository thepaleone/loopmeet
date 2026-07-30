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
    public string? Timezone { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? GroupName { get; set; }
    public string? CreatedByDisplayName { get; set; }

    /// <summary>Owner of this meetup's group — the sole input to edit gating.</summary>
    public Guid GroupOwnerUserId { get; set; }

    public bool HasLocation => !string.IsNullOrWhiteSpace(PlaceName);

    /// <summary>
    /// Whether the location can actually be handed to a maps app. A place name
    /// without coordinates is displayable but not openable.
    /// </summary>
    public bool CanOpenLocation => Latitude is not null && Longitude is not null;

    /// <summary>Organizer with the FR-011 placeholder already applied.</summary>
    public string OrganizerDisplay => MeetupOrganizerText.Format(CreatedByDisplayName);

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
    public string? Timezone { get; set; }
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
    public string? Timezone { get; set; }
}
