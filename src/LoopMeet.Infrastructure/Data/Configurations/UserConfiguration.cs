using LoopMeet.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoopMeet.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(user => user.Email).IsRequired().HasMaxLength(320);
        builder.Property(user => user.Phone).HasMaxLength(32);
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
