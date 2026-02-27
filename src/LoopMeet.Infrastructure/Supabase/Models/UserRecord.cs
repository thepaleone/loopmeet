using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LoopMeet.Infrastructure.Supabase.Models;

[Table("user_profiles")]
public sealed class UserRecord : BaseModel
{
    [PrimaryKey("id", true)]
    public Guid Id { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("social_avatar_url")]
    public string? SocialAvatarUrl { get; set; }

    [Column("avatar_override_url")]
    public string? AvatarOverrideUrl { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
