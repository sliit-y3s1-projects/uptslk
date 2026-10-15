using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class RouteScheduleConfiguration : IEntityTypeConfiguration<RouteSchedule>
{
    public void Configure(EntityTypeBuilder<RouteSchedule> builder)
    {
        builder.HasOne(s => s.Route)
            .WithMany(r => r.Schedules)
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Bay)
            .WithMany(b => b.RouteSchedules)
            .HasForeignKey(s => s.BayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(schedule => schedule.RouteDirection)
            .WithMany(direction => direction.Schedules)
            .HasForeignKey(schedule => schedule.RouteDirectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
