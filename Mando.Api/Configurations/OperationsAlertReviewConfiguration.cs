using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class OperationsAlertReviewConfiguration : IEntityTypeConfiguration<OperationsAlertReview>
{
    public void Configure(EntityTypeBuilder<OperationsAlertReview> builder)
    {
        builder.Property(x => x.AlertKey)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.AlertFingerprint)
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(x => x.ShortReasonSnapshot)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.Property(x => x.ReviewedByUserFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => new { x.AlertFingerprint, x.ReviewedAtUtc });
        builder.HasIndex(x => new { x.AlertKey, x.ReviewedAtUtc });
        builder.HasIndex(x => new { x.Category, x.EntityType, x.EntityId });

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}