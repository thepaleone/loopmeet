using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LoopMeet.Infrastructure.Supabase.Models;

[Table("account_deletions")]
public sealed class AccountDeletionRecord : BaseModel
{
    [PrimaryKey("user_id", false)]
    public Guid UserId { get; set; }

    [Column("requested_at")]
    public DateTimeOffset RequestedAt { get; set; }

    [Column("purge_after")]
    public DateTimeOffset PurgeAfter { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;
}
