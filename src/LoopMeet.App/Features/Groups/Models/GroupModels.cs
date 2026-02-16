using LoopMeet.App.Features.Invitations.Models;

namespace LoopMeet.App.Features.Groups.Models;

public sealed class GroupSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
}

public sealed class GroupMember
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class GroupDetail : GroupSummary
{
    public List<GroupMember> Members { get; set; } = new();
}

public sealed class GroupsResponse
{
    public List<GroupSummary> Owned { get; set; } = new();
    public List<GroupSummary> Member { get; set; } = new();
    public List<InvitationSummary> PendingInvitations { get; set; } = new();
}
