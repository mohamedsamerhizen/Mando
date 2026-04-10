using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mando.Api.Entities;

namespace Mando.Api.Configurations;

public class UserActionHistoryConfiguration : IEntityTypeConfiguration<UserActionHistory>
{
    public void Configure(EntityTypeBuilder<UserActionHistory> builder)
    {
        builder.Property(x => x.FullNameSnapshot)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EmailSnapshot)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.RolesSnapshot)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.PerformedByUserFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.TargetUserId, x.ActionAtUtc });

        builder.HasOne(x => x.TargetUser)
            .WithMany()
            .HasForeignKey(x => x.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}