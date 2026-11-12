namespace LoopMeet.App.Features.Account.Models;

public sealed class AccountDeletionRequest
{
    public string Confirmation { get; set; } = string.Empty;
}

public sealed class AccountDeletionStatusResponse
{
    /// <summary><c>none</c>, <c>pending</c> or <c>purging</c>.</summary>
    public string Status { get; set; } = "none";
    public DateTimeOffset? RequestedAt { get; set; }
    public DateTimeOffset? DeletesAt { get; set; }
}

public sealed class AccountDeletionPreviewResponse
{
    public DateTimeOffset DeletesAt { get; set; }
    public List<OwnedGroupSummary> OwnedGroups { get; set; } = new();
}

public sealed class OwnedGroupSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }

    public string Description => MemberCount == 1 ? $"{Name} · 1 member" : $"{Name} · {MemberCount} members";
}
