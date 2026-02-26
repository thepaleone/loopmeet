namespace LoopMeet.App.Features.Invitations.Models;

public sealed class InvitationSummary
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }

    public string OwnerDisplayText =>
        !string.IsNullOrWhiteSpace(OwnerName)
            ? $"Owned by {OwnerName}"
            : (!string.IsNullOrWhiteSpace(OwnerEmail) ? $"Owned by {OwnerEmail}" : "Group owner");
}

public sealed class InvitationsResponse
{
    public List<InvitationSummary> Invitations { get; set; } = new();
}

public sealed class CreateInvitationRequest
{
    public string Email { get; set; } = string.Empty;
}
