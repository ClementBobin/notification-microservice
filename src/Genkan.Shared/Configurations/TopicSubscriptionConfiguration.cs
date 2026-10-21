using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genkan.Shared.Configurations;

public sealed class TopicSubscriptionConfiguration : IEntityTypeConfiguration<TopicSubscription>
{
    public void Configure(EntityTypeBuilder<TopicSubscription> e)
    {
        e.ToTable("topic_subscriptions", GenkanSchema.Name);
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.Topic, x.UserId }).IsUnique();
        e.HasIndex(x => x.UserId);
        e.Property(x => x.Topic).HasMaxLength(900);
    }
}
