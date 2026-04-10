using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class VisitActionHistoryConfiguration : IEntityTypeConfiguration<VisitActionHistory>
{
    public void Configure(EntityTypeBuilder<VisitActionHistory> builder)
    {
        builder.Property(x => x.PerformedByUserFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.VisitId, x.ActionAtUtc });

        builder.HasOne(x => x.Visit)
            .WithMany()
            .HasForeignKey(x => x.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}