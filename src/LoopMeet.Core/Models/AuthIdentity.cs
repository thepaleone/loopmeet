namespace LoopMeet.Core.Models;

public sealed class AuthIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderSubject { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
