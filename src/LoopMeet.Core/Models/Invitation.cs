namespace LoopMeet.Core.Models;

public sealed class Invitation
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public Guid? InvitedUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}
