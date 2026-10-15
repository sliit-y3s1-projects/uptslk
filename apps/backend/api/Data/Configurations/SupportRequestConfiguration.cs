using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
{
    public void Configure(EntityTypeBuilder<SupportRequest> builder)
    {
        builder.Property(request => request.Subject).HasMaxLength(200);
        builder.Property(request => request.Description).HasMaxLength(2000);
        builder.Property(request => request.Resolution).HasMaxLength(2000);

        builder.HasOne(request => request.Passenger)
            .WithMany()
            .HasForeignKey(request => request.PassengerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(request => request.Trip)
            .WithMany()
            .HasForeignKey(request => request.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(request => request.Centre)
            .WithMany()
            .HasForeignKey(request => request.CentreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
