using api.Data.Converters;
using api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Route = api.Entities.Route;
using RouteDirection = api.Entities.RouteDirection;

namespace api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Centre> Centres => Set<Centre>();
    public DbSet<Bay> Bays => Set<Bay>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteDirection> RouteDirections => Set<RouteDirection>();
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
    public DbSet<RecoveryMemory> RecoveryMemories => Set<RecoveryMemory>();
    public DbSet<PassengerNotification> PassengerNotifications => Set<PassengerNotification>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    // PostgreSQL "timestamp with time zone" only accepts UTC, so every DateTime is converted on the way in.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity must set up its own tables first.
        base.OnModelCreating(modelBuilder);

        // Loads every IEntityTypeConfiguration class in Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
