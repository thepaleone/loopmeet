using LoopMeet.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoopMeet.Infrastructure.Data.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.InvitedEmail).IsRequired().HasMaxLength(320);
        builder.Property(invitation => invitation.Status).IsRequired().HasMaxLength(20);
        builder.HasIndex(invitation => new { invitation.GroupId, invitation.InvitedEmail, invitation.Status });
    }
}
