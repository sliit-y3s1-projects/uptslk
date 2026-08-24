using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using api.Models;
using RouteModel = api.Models.Route;

namespace api.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<RouteModel> Routes => Set<RouteModel>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<AgentWorkflow> AgentWorkflows => Set<AgentWorkflow>();
    public DbSet<AgentStep> AgentSteps => Set<AgentStep>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // IMPORTANT: sets up Identity's own tables first

        modelBuilder.Entity<Vehicle>().HasIndex(v => v.PlateNumber).IsUnique();
        modelBuilder.Entity<Driver>().HasIndex(d => d.LicenseNumber).IsUnique();

        // User <-> Driver (1:1)
        modelBuilder.Entity<Driver>()
            .HasOne(d => d.User)
            .WithOne(u => u.Driver)
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User <-> Wallet (1:1)
        modelBuilder.Entity<Wallet>()
            .HasOne(w => w.Commuter)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.CommuterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Route <-> RouteStop (1:N)
        modelBuilder.Entity<RouteStop>()
            .HasOne(rs => rs.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(rs => rs.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // User <-> Booking (1:N, as commuter)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Commuter)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.CommuterId)
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
            .OnDelete(DeleteBehavior.Restrict);

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

        // Booking <-> AgentWorkflow (1:1, nullable)
        modelBuilder.Entity<AgentWorkflow>()
            .HasOne(aw => aw.Booking)
            .WithOne(b => b.AgentWorkflow)
            .HasForeignKey<AgentWorkflow>(aw => aw.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

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