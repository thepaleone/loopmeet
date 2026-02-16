using LoopMeet.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Infrastructure.Data;

public sealed class LoopMeetDbContext : DbContext
{
    public LoopMeetDbContext(DbContextOptions<LoopMeetDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<AuthIdentity> AuthIdentities => Set<AuthIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LoopMeetDbContext).Assembly);
    }
}
