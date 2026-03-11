using LoopMeet.App.Features.Invitations.Models;

namespace LoopMeet.App.Features.Groups.Models;

public class GroupSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public int MemberCount { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string OwnerAvatarUrl { get; set; } = string.Empty;

    public string MemberCountText => MemberCount == 1 ? "1 member" : $"{MemberCount} members";
    public string OwnerInitial => OwnerDisplayName.Length > 0
        ? OwnerDisplayName[0].ToString().ToUpperInvariant()
        : "?";
    public bool HasOwnerAvatar => !string.IsNullOrWhiteSpace(OwnerAvatarUrl);
}

public sealed class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
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
