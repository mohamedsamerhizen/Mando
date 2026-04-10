using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ContactPersonName)
            .HasMaxLength(150);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.City)
            .HasMaxLength(100);

        builder.Property(x => x.Region)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.Latitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.Longitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(x => x.CreditLimit)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.OpeningBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Customers_Latitude_Range",
                "[Latitude] >= -90 AND [Latitude] <= 90");

            tableBuilder.HasCheckConstraint(
                "CK_Customers_Longitude_Range",
                "[Longitude] >= -180 AND [Longitude] <= 180");
        });

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasOne(x => x.AssignedSalesRep)
            .WithMany()
            .HasForeignKey(x => x.AssignedSalesRepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}