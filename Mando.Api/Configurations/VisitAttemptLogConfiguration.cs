using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class VisitAttemptLogConfiguration : IEntityTypeConfiguration<VisitAttemptLog>
{
    public void Configure(EntityTypeBuilder<VisitAttemptLog> builder)
    {
        builder.Property(x => x.Latitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.Longitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.AccuracyInMeters)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne(x => x.SalesRep)
            .WithMany()
            .HasForeignKey(x => x.SalesRepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SalesRepId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.IsSuccessful);
        builder.HasIndex(x => x.ComplianceStatus);
    }
}