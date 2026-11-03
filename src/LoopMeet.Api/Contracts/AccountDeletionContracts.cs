using System.Text.Json.Serialization;

namespace LoopMeet.Api.Contracts;

public sealed class AccountDeletionRequest
{
    public string? Confirmation { get; init; }
}

public sealed class AccountDeletionStatusResponse
{
    /// <summary><c>none</c>, <c>pending</c> or <c>purging</c>.</summary>
    public string Status { get; init; } = AccountDeletionStatusNames.None;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? RequestedAt { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DeletesAt { get; init; }
}

public static class AccountDeletionStatusNames
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string Purging = "purging";
}

public sealed class AccountDeletionPreviewResponse
{
    public DateTimeOffset DeletesAt { get; init; }
    public List<OwnedGroupSummary> OwnedGroups { get; init; } = new();
}

public sealed class OwnedGroupSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MemberCount { get; init; }
}

/// <summary>The gate's 403 body for every other endpoint while the caller is pending deletion.</summary>
public sealed class AccountPendingDeletionResponse
{
    public string Code { get; init; } = "account_pending_deletion";
    public string Message { get; init; } = "This account is scheduled for deletion.";
    public DateTimeOffset DeletesAt { get; init; }
}
