using LoopMeet.Core.Models;

namespace LoopMeet.Core.Meetups;

/// <summary>Keyset position in a newest-first meetup listing: the last row examined.</summary>
public readonly record struct MeetupPageCursor(DateTimeOffset ScheduledAt, Guid Id);

public sealed record MeetupPage(IReadOnlyList<Meetup> Items, MeetupPageCursor? NextCursor);
