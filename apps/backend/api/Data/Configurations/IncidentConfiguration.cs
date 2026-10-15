using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.HasOne(i => i.Centre)
            .WithMany(c => c.Incidents)
            .HasForeignKey(i => i.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Trip <-> Incident (1:N, nullable)
        builder.HasOne(i => i.Trip)
            .WithMany(t => t.Incidents)
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        // User <-> Incident (1:N, as reporter)
        builder.HasOne(i => i.ReportedBy)
            .WithMany(u => u.ReportedIncidents)
            .HasForeignKey(i => i.ReportedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
