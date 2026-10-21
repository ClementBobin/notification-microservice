using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genkan.Shared.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> e)
    {
        e.ToTable("device_tokens", GenkanSchema.Name);
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.Token).IsUnique();
        e.HasIndex(x => x.UserId);
        e.Property(x => x.Token).HasMaxLength(1024);
        e.Property(x => x.Platform).HasMaxLength(20);
    }
}
