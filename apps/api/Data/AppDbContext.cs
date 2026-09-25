using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using api.Models;
using RouteModel = api.Models.Route;
using RouteDirectionModel = api.Models.RouteDirection;

namespace api.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Centre> Centres => Set<Centre>();
    public DbSet<Bay> Bays => Set<Bay>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<RouteModel> Routes => Set<RouteModel>();
    public DbSet<RouteDirectionModel> RouteDirections => Set<RouteDirectionModel>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<RouteSchedule> RouteSchedules => Set<RouteSchedule>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<FareRule> FareRules => Set<FareRule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<AgentWorkflow> AgentWorkflows => Set<AgentWorkflow>();
    public DbSet<AgentStep> AgentSteps => Set<AgentStep>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // IMPORTANT: sets up Identity's own tables first

        modelBuilder.Entity<Vehicle>().HasIndex(v => v.PlateNumber).IsUnique();
        modelBuilder.Entity<Driver>().HasIndex(d => d.LicenseNumber).IsUnique();
        modelBuilder.Entity<Driver>().HasIndex(d => d.UserId).IsUnique();
        modelBuilder.Entity<Centre>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<Bay>().HasIndex(b => new { b.CentreId, b.Code }).IsUnique();
        modelBuilder.Entity<RouteModel>().HasIndex(r => new { r.CentreId, r.RouteNumber }).IsUnique();
        modelBuilder.Entity<RouteStop>().HasIndex(rs => new { rs.RouteDirectionId, rs.SequenceOrder }).IsUnique();
        modelBuilder.Entity<RouteDirectionModel>().HasIndex(direction => new { direction.RouteId, direction.StartCentreId, direction.EndCentreId }).IsUnique();
        modelBuilder.Entity<Passenger>().HasIndex(p => p.PhoneNumber).IsUnique();
        modelBuilder.Entity<Passenger>().HasOne(p => p.User).WithOne(u => u.Passenger).HasForeignKey<Passenger>(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FareRule>().HasIndex(rule => new { rule.RouteId, rule.PassengerCategory }).IsUnique();
        modelBuilder.Entity<Booking>().HasIndex(booking => new { booking.TripId, booking.SeatNumber })
            .IsUnique()
            .HasFilter("\"Status\" IN (0, 1)");
        modelBuilder.Entity<Payment>().HasIndex(payment => new { payment.Provider, payment.ProviderOrderId }).IsUnique();
        modelBuilder.Entity<PaymentWebhookEvent>().HasIndex(webhook => new { webhook.Provider, webhook.ProviderEventId }).IsUnique();

        modelBuilder.Entity<Centre>().Property(c => c.Code).HasMaxLength(32);
        modelBuilder.Entity<Centre>().Property(c => c.Name).HasMaxLength(160);
        modelBuilder.Entity<Bay>().Property(b => b.Code).HasMaxLength(24);
        modelBuilder.Entity<RouteModel>().Property(r => r.RouteNumber).HasMaxLength(32);
        modelBuilder.Entity<Vehicle>().Property(v => v.Model).HasMaxLength(120);
        modelBuilder.Entity<Driver>().Property(d => d.FullName).HasMaxLength(160);
        modelBuilder.Entity<MaintenanceRecord>().Property(m => m.Type).HasMaxLength(80);
        modelBuilder.Entity<Passenger>().Property(p => p.FullName).HasMaxLength(160);
        modelBuilder.Entity<Passenger>().Property(p => p.PhoneNumber).HasMaxLength(32);
        modelBuilder.Entity<FareRule>().Property(rule => rule.Amount).HasPrecision(12, 2);
        modelBuilder.Entity<Booking>().Property(booking => booking.Fare).HasPrecision(12, 2);
        modelBuilder.Entity<Booking>().Property(booking => booking.RefundAmount).HasPrecision(12, 2);
        modelBuilder.Entity<Wallet>().Property(wallet => wallet.Balance).HasPrecision(12, 2);
        modelBuilder.Entity<Transaction>().Property(transaction => transaction.Amount).HasPrecision(12, 2);
        modelBuilder.Entity<Payment>().Property(payment => payment.Amount).HasPrecision(12, 2);
        modelBuilder.Entity<Payment>().Property(payment => payment.Currency).HasMaxLength(3);
        modelBuilder.Entity<Payment>().Property(payment => payment.ProviderOrderId).HasMaxLength(96);
        modelBuilder.Entity<Payment>().Property(payment => payment.ProviderCheckoutId).HasMaxLength(128);
        modelBuilder.Entity<Payment>().Property(payment => payment.ProviderPaymentId).HasMaxLength(96);
        modelBuilder.Entity<PaymentWebhookEvent>().Property(webhook => webhook.ProviderEventId).HasMaxLength(128);
        modelBuilder.Entity<PaymentWebhookEvent>().Property(webhook => webhook.EventType).HasMaxLength(128);
        modelBuilder.Entity<PaymentRefund>().Property(refund => refund.Amount).HasPrecision(12, 2);
        modelBuilder.Entity<PaymentRefund>().Property(refund => refund.Reason).HasMaxLength(1000);
        modelBuilder.Entity<AgentWorkflow>().Property(workflow => workflow.Objective).HasMaxLength(1000);
        modelBuilder.Entity<AgentWorkflow>().Property(workflow => workflow.FailureReason).HasMaxLength(2000);
        modelBuilder.Entity<AgentWorkflow>().Property(workflow => workflow.PlanJson).HasColumnType("jsonb");
        modelBuilder.Entity<AgentWorkflow>().Property(workflow => workflow.ValidationJson).HasColumnType("jsonb");
        modelBuilder.Entity<AgentStep>().Property(step => step.AgentName).HasMaxLength(120);
        modelBuilder.Entity<AgentStep>().Property(step => step.Status).HasMaxLength(32);
        modelBuilder.Entity<AgentStep>().Property(step => step.Error).HasMaxLength(2000);
        modelBuilder.Entity<AgentStep>().Property(step => step.ToolCallsJson).HasColumnType("jsonb");
        modelBuilder.Entity<ApprovalRequest>().Property(request => request.Reason).HasMaxLength(2000);
        modelBuilder.Entity<ApprovalRequest>().Property(request => request.DecisionNote).HasMaxLength(2000);
        modelBuilder.Entity<SupportRequest>().Property(request => request.Subject).HasMaxLength(200);
        modelBuilder.Entity<SupportRequest>().Property(request => request.Description).HasMaxLength(2000);
        modelBuilder.Entity<SupportRequest>().Property(request => request.Resolution).HasMaxLength(2000);

        modelBuilder.Entity<Bay>()
            .HasOne(b => b.Centre)
            .WithMany(c => c.Bays)
            .HasForeignKey(b => b.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RouteModel>()
            .HasOne(r => r.Centre)
            .WithMany(c => c.Routes)
            .HasForeignKey(r => r.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Centre)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Driver>()
            .HasOne(d => d.Centre)
            .WithMany(c => c.Drivers)
            .HasForeignKey(d => d.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Centre)
            .WithMany(c => c.Trips)
            .HasForeignKey(t => t.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incident>()
            .HasOne(i => i.Centre)
            .WithMany(c => c.Incidents)
            .HasForeignKey(i => i.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(user => user.Centre)
            .WithMany()
            .HasForeignKey(user => user.CentreId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Bay)
            .WithMany(b => b.Trips)
            .HasForeignKey(t => t.BayId)
            .OnDelete(DeleteBehavior.Restrict);

        // User <-> Driver (1:1)
        modelBuilder.Entity<Driver>()
            .HasOne(d => d.User)
            .WithOne(u => u.Driver)
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MaintenanceRecord>()
            .HasOne(record => record.Vehicle)
            .WithMany(vehicle => vehicle.MaintenanceRecords)
            .HasForeignKey(record => record.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Wallet>()
            .HasOne(w => w.Passenger)
            .WithOne(p => p.Wallet)
            .HasForeignKey<Wallet>(w => w.PassengerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Route <-> RouteStop (1:N)
        modelBuilder.Entity<RouteStop>()
            .HasOne(rs => rs.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(rs => rs.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouteSchedule>()
            .HasOne(s => s.Route)
            .WithMany(r => r.Schedules)
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouteSchedule>()
            .HasOne(s => s.Bay)
            .WithMany(b => b.RouteSchedules)
            .HasForeignKey(s => s.BayId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RouteDirectionModel>()
            .HasOne(direction => direction.Route)
            .WithMany(route => route.Directions)
            .HasForeignKey(direction => direction.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RouteDirectionModel>()
            .HasOne(direction => direction.StartCentre)
            .WithMany()
            .HasForeignKey(direction => direction.StartCentreId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RouteDirectionModel>()
            .HasOne(direction => direction.EndCentre)
            .WithMany()
            .HasForeignKey(direction => direction.EndCentreId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RouteStop>()
            .HasOne(stop => stop.RouteDirection)
            .WithMany(direction => direction.Stops)
            .HasForeignKey(stop => stop.RouteDirectionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RouteSchedule>()
            .HasOne(schedule => schedule.RouteDirection)
            .WithMany(direction => direction.Schedules)
            .HasForeignKey(schedule => schedule.RouteDirectionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Trip>()
            .HasOne(trip => trip.RouteDirection)
            .WithMany(direction => direction.Trips)
            .HasForeignKey(trip => trip.RouteDirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Route <-> Trip (1:N)
        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(t => t.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Vehicle <-> Trip (1:N)
        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Vehicle)
            .WithMany(v => v.Trips)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Driver <-> Trip (1:N)
        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Driver)
            .WithMany(d => d.Trips)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Trip <-> Booking (1:N)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Trip)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Passenger)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FareRule>()
            .HasOne(rule => rule.Route)
            .WithMany(route => route.FareRules)
            .HasForeignKey(rule => rule.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Trip <-> Incident (1:N, nullable)
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.Trip)
            .WithMany(t => t.Incidents)
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        // User <-> Incident (1:N, as reporter)
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.ReportedBy)
            .WithMany(u => u.ReportedIncidents)
            .HasForeignKey(i => i.ReportedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Wallet <-> Transaction (1:N)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Booking <-> Transaction (1:N, nullable)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Booking)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.Booking)
            .WithMany(booking => booking.Payments)
            .HasForeignKey(payment => payment.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentRefund>()
            .HasOne(refund => refund.Payment)
            .WithMany(payment => payment.Refunds)
            .HasForeignKey(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupportRequest>()
            .HasOne(request => request.Passenger)
            .WithMany()
            .HasForeignKey(request => request.PassengerId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SupportRequest>()
            .HasOne(request => request.Trip)
            .WithMany()
            .HasForeignKey(request => request.TripId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SupportRequest>()
            .HasOne(request => request.Centre)
            .WithMany()
            .HasForeignKey(request => request.CentreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Booking <-> AgentWorkflow (1:1, nullable)
        modelBuilder.Entity<AgentWorkflow>()
            .HasOne(aw => aw.Booking)
            .WithOne(b => b.AgentWorkflow)
            .HasForeignKey<AgentWorkflow>(aw => aw.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AgentWorkflow>()
            .HasOne(workflow => workflow.Centre)
            .WithMany()
            .HasForeignKey(workflow => workflow.CentreId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AgentWorkflow>()
            .HasOne(workflow => workflow.Incident)
            .WithMany()
            .HasForeignKey(workflow => workflow.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AgentWorkflow>()
            .HasOne(workflow => workflow.Trip)
            .WithMany()
            .HasForeignKey(workflow => workflow.TripId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AgentWorkflow>().HasIndex(workflow => new { workflow.CentreId, workflow.Status });
        modelBuilder.Entity<AgentWorkflow>().HasIndex(workflow => workflow.IncidentId);

        // AgentWorkflow <-> AgentStep (1:N)
        modelBuilder.Entity<AgentStep>()
            .HasOne(s => s.Workflow)
            .WithMany(w => w.Steps)
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // AgentWorkflow <-> ApprovalRequest (1:N)
        modelBuilder.Entity<ApprovalRequest>()
            .HasOne(ar => ar.Workflow)
            .WithMany(w => w.ApprovalRequests)
            .HasForeignKey(ar => ar.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // User <-> ApprovalRequest (1:N, as reviewer, nullable)
        modelBuilder.Entity<ApprovalRequest>()
            .HasOne(ar => ar.ReviewedBy)
            .WithMany(u => u.ReviewedApprovals)
            .HasForeignKey(ar => ar.ReviewedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
