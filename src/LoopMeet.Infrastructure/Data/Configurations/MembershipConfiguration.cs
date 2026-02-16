using LoopMeet.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoopMeet.Infrastructure.Data.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Role).IsRequired().HasMaxLength(32);
        builder.HasIndex(membership => new { membership.GroupId, membership.UserId }).IsUnique();
    }
}
