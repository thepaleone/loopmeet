namespace LoopMeet.App.Features.Meetups;

/// <summary>
/// Presentation of a meetup's organizer. The API returns an empty display name
/// when the creator can no longer be resolved (they left the group, or have no
/// profile); the user-facing placeholder lives here rather than in the response
/// body so it stays with the rest of the app's copy.
/// </summary>
public static class MeetupOrganizerText
{
    public const string UnknownOrganizer = "A group member";

    /// <summary>
    /// Never renders a blank organizer or an internal identifier (FR-011).
    /// </summary>
    public static string Format(string? displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? UnknownOrganizer : displayName;
}
