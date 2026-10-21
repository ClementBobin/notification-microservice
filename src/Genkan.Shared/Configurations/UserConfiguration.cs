using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genkan.Shared.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.Email).IsUnique();
        e.Property(x => x.Email).HasMaxLength(320);
        e.Property(x => x.DisplayName).HasMaxLength(200);
        e.Property(x => x.Role).HasMaxLength(20);
    }
}
