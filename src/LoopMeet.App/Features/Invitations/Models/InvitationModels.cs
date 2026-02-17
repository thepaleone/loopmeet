namespace LoopMeet.App.Features.Invitations.Models;

public sealed class InvitationSummary
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class InvitationsResponse
{
    public List<InvitationSummary> Invitations { get; set; } = new();
}

public sealed class CreateInvitationRequest
{
    public string Email { get; set; } = string.Empty;
}
