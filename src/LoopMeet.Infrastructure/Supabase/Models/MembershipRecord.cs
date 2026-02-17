using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LoopMeet.Infrastructure.Supabase.Models;

[Table("memberships")]
public sealed class MembershipRecord : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("role")]
    public string Role { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
