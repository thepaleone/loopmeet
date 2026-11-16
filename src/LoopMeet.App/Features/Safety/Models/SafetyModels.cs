namespace LoopMeet.App.Features.Safety.Models;

/// <summary>POST /reports. A null <see cref="ReportedUserId"/> reports the group itself.</summary>
public sealed class ReportRequest
{
    public Guid GroupId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class ReportResult
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

/// <summary>A person the report form offers, or the group itself when <see cref="UserId"/> is null.</summary>
public sealed record ReportPersonOption(Guid? UserId, string Name)
{
    public const string GroupItself = "The group itself (name or meetups)";

    public override string ToString() => Name;
}
