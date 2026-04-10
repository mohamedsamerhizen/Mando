using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class OrderActionHistoryConfiguration : IEntityTypeConfiguration<OrderActionHistory>
{
    public void Configure(EntityTypeBuilder<OrderActionHistory> builder)
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

        builder.HasIndex(x => new { x.OrderId, x.ActionAtUtc });

        builder.HasOne(x => x.Order)
            .WithMany(x => x.ActionHistories)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}