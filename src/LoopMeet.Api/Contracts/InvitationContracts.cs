namespace LoopMeet.Api.Contracts;

public sealed class InvitationResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string InvitedEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class CreateInvitationRequest
{
    public string Email { get; init; } = string.Empty;
}

public sealed class InvitationsResponse
{
    public IReadOnlyList<InvitationResponse> Invitations { get; init; } = Array.Empty<InvitationResponse>();
}
