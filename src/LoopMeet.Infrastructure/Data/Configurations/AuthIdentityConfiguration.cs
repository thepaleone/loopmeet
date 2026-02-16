using LoopMeet.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoopMeet.Infrastructure.Data.Configurations;

public sealed class AuthIdentityConfiguration : IEntityTypeConfiguration<AuthIdentity>
{
    public void Configure(EntityTypeBuilder<AuthIdentity> builder)
    {
        builder.HasKey(identity => identity.Id);
        builder.Property(identity => identity.Provider).IsRequired().HasMaxLength(50);
        builder.Property(identity => identity.ProviderSubject).IsRequired().HasMaxLength(200);
        builder.HasIndex(identity => new { identity.Provider, identity.ProviderSubject }).IsUnique();
    }
}
