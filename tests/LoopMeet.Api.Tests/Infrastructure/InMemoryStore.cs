using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryStore
{
    public List<User> Users { get; } = new();
    public List<Group> Groups { get; } = new();
    public List<Membership> Memberships { get; } = new();
    public List<Invitation> Invitations { get; } = new();
    public List<AuthIdentity> AuthIdentities { get; } = new();
    public object SyncRoot { get; } = new();
}
