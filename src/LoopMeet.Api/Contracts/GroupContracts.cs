namespace LoopMeet.Api.Contracts;

public class GroupSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid OwnerUserId { get; init; }
    public int MemberCount { get; init; }
}

public sealed class CreateGroupRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateGroupRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed class GroupMemberResponse
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class GroupDetailResponse : GroupSummaryResponse
{
    public IReadOnlyList<GroupMemberResponse> Members { get; init; } = Array.Empty<GroupMemberResponse>();
}

public sealed class GroupsResponse
{
    public IReadOnlyList<GroupSummaryResponse> Owned { get; init; } = Array.Empty<GroupSummaryResponse>();
    public IReadOnlyList<GroupSummaryResponse> Member { get; init; } = Array.Empty<GroupSummaryResponse>();
    public IReadOnlyList<InvitationResponse> PendingInvitations { get; set; } = Array.Empty<InvitationResponse>();
}
