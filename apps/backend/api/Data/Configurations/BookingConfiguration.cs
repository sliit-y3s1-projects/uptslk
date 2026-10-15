using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasIndex(booking => new { booking.TripId, booking.SeatNumber })
            .IsUnique()
            .HasFilter("\"Status\" IN (0, 1)");

        builder.Property(booking => booking.Fare).HasPrecision(12, 2);
        builder.Property(booking => booking.RefundAmount).HasPrecision(12, 2);

        // Trip <-> Booking (1:N)
        builder.HasOne(b => b.Trip)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Passenger)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
