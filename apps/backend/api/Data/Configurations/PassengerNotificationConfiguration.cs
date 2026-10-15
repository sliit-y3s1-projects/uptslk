using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class PassengerNotificationConfiguration : IEntityTypeConfiguration<PassengerNotification>
{
    public void Configure(EntityTypeBuilder<PassengerNotification> builder)
    {
        builder.HasIndex(notification => new { notification.WorkflowId, notification.BookingId, notification.Channel })
            .IsUnique();

        builder.Property(notification => notification.Subject).HasMaxLength(200);
        builder.Property(notification => notification.Message).HasMaxLength(1000);
        builder.Property(notification => notification.DeliveryError).HasMaxLength(1000);

        builder.HasOne(notification => notification.Workflow)
            .WithMany(workflow => workflow.PassengerNotifications)
            .HasForeignKey(notification => notification.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(notification => notification.Booking)
            .WithMany(booking => booking.PassengerNotifications)
            .HasForeignKey(notification => notification.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(notification => notification.Passenger)
            .WithMany(passenger => passenger.Notifications)
            .HasForeignKey(notification => notification.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
