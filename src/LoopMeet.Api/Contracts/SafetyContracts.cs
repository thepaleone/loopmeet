namespace LoopMeet.Api.Contracts;

/// <summary>POST /reports (specs/023-safety-controls/contracts/api.md).</summary>
public sealed class ReportRequest
{
    public Guid GroupId { get; init; }

    /// <summary>Null reports the group itself.</summary>
    public Guid? ReportedUserId { get; init; }

    public string? Reason { get; init; }

    public string? Details { get; init; }
}

public sealed class ReportResponse
{
    public Guid Id { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }
}
