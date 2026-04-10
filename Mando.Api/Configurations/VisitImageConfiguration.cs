using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class VisitImageConfiguration : IEntityTypeConfiguration<VisitImage>
{
    public void Configure(EntityTypeBuilder<VisitImage> builder)
    {
        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StoredFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.RelativePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SlotNumber)
            .IsRequired();

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_VisitImages_SlotNumber_Range",
                "[SlotNumber] >= 1 AND [SlotNumber] <= 5");
        });

        builder.HasOne(x => x.Visit)
            .WithMany()
            .HasForeignKey(x => x.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.VisitId);
        builder.HasIndex(x => x.UploadedByUserId);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.VisitId, x.SlotNumber })
            .IsUnique();
    }
}
