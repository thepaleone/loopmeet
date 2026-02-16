namespace LoopMeet.Core.Models;

public sealed class Group
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
