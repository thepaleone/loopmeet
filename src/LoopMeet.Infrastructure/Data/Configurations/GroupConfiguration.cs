using LoopMeet.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoopMeet.Infrastructure.Data.Configurations;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(group => group.Id);
        builder.Property(group => group.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(group => new { group.OwnerUserId, group.Name }).IsUnique();
    }
}
