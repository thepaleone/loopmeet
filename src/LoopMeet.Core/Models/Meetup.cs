namespace LoopMeet.Core.Models;

public sealed class Meetup
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public string? PlaceName { get; set; }
    public string? PlaceAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? PlaceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
