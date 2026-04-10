using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.Property(x => x.CheckInLatitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.CheckInLongitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.CheckInAccuracyInMeters)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.CheckOutLatitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.CheckOutLongitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.CheckOutAccuracyInMeters)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalesRep)
            .WithMany()
            .HasForeignKey(x => x.SalesRepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.SalesRepId);
        builder.HasIndex(x => x.CheckInAtUtc);
        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.SalesRepId)
            .HasDatabaseName("IX_Visits_SalesRepId_OneActiveVisit")
            .IsUnique()
            .HasFilter("[Status] = 1");
    }
}