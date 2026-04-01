using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LoopMeet.Infrastructure.Supabase.Models;

[Table("meetups")]
public sealed class MeetupRecord : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("scheduled_at")]
    public DateTimeOffset ScheduledAt { get; set; }

    [Column("place_name")]
    public string? PlaceName { get; set; }

    [Column("place_address")]
    public string? PlaceAddress { get; set; }

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [Column("place_id")]
    public string? PlaceId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
