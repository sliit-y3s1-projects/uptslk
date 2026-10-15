using api.Data;
using api.Interfaces.Services;
using api.Repositories;
using api.Services;
using api.Services.AgentRecovery;

namespace api.Tests.Shared;

// Builds services on top of a test database, for tests that call a service directly instead of over HTTP.
internal static class TestServices
{
    public static TripConflictService TripConflicts(AppDbContext db) =>
        new(new TripRepository(db), new MaintenanceRecordRepository(db));

    public static PassengerNotificationService PassengerNotices(AppDbContext db) =>
        new(new BayRepository(db), new BookingRepository(db), new PassengerNotificationRepository(db));

    public static BookingPaymentService BookingPayments(AppDbContext db, IEnumerable<IPaymentGateway> gateways, IMobileNotificationService notifications) =>
        new(
            new PassengerRepository(db),
            new TripRepository(db),
            new BookingRepository(db),
            new FareRuleRepository(db),
            new PaymentRepository(db),
            new PaymentRefundRepository(db),
            new PaymentWebhookEventRepository(db),
            new UnitOfWork(db),
            gateways,
            notifications);
}
