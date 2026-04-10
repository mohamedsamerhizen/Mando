using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class PaymentActionHistoryConfiguration : IEntityTypeConfiguration<PaymentActionHistory>
{
    public void Configure(EntityTypeBuilder<PaymentActionHistory> builder)
    {
        builder.Property(x => x.PerformedByUserFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.BalanceBeforeAction)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.BalanceAfterAction)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.PaymentId, x.ActionAtUtc });

        builder.HasOne(x => x.Payment)
            .WithMany(x => x.ActionHistories)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}