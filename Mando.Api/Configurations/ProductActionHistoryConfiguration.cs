using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class ProductActionHistoryConfiguration : IEntityTypeConfiguration<ProductActionHistory>
{
    public void Configure(EntityTypeBuilder<ProductActionHistory> builder)
    {
        builder.Property(x => x.NewName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PreviousName)
            .HasMaxLength(200);

        builder.Property(x => x.NewCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PreviousCode)
            .HasMaxLength(100);

        builder.Property(x => x.PreviousUnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.NewUnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PerformedByUserFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.ProductId, x.ActionAtUtc });

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ActionHistories)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}