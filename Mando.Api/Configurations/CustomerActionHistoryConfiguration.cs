using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class CustomerActionHistoryConfiguration : IEntityTypeConfiguration<CustomerActionHistory>
{
    public void Configure(EntityTypeBuilder<CustomerActionHistory> builder)
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

        builder.Property(x => x.NewAssignedSalesRepName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PreviousAssignedSalesRepName)
            .HasMaxLength(200);

        builder.Property(x => x.PerformedByUserFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.Property(x => x.PreviousCreditLimit)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.NewCreditLimit)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PreviousOpeningBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.NewOpeningBalance)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.CustomerId, x.ActionAtUtc });

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.ActionHistories)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}