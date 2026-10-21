using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genkan.Shared.Configurations;

public sealed class DeviceGroupMemberConfiguration : IEntityTypeConfiguration<DeviceGroupMember>
{
    public void Configure(EntityTypeBuilder<DeviceGroupMember> e)
    {
        e.ToTable("device_group_members", GenkanSchema.Name);
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.GroupName, x.UserId }).IsUnique();
        e.Property(x => x.GroupName).HasMaxLength(100);
    }
}
