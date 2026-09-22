using System.Globalization;
using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Payments;

public sealed class BookingPaymentService(AppDbContext db, IEnumerable<IPaymentGateway> gateways)
{
    public async Task<(PaymentCheckoutSession? Session, string? Error, int StatusCode)> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken)
    {
        var passenger = await db.Passengers.SingleOrDefaultAsync(item => item.Id == request.PassengerId, cancellationToken);
        var trip = await db.Trips.Include(item => item.Vehicle).Include(item => item.Route).SingleOrDefaultAsync(item => item.Id == request.TripId, cancellationToken);
        if (passenger is null || !passenger.IsActive) return (null, "The selected passenger is not active.", StatusCodes.Status400BadRequest);
        if (trip?.Vehicle is null || trip.Vehicle.Capacity < 1) return (null, "This trip has no valid vehicle capacity configured.", StatusCodes.Status400BadRequest);
        if (trip.Status is not (TripStatus.Scheduled or TripStatus.Ready or TripStatus.Boarding)) return (null, "Bookings are not available for this trip.", StatusCodes.Status400BadRequest);

        if (await ActiveBookingCount(trip.Id, cancellationToken) >= trip.Vehicle.Capacity) return (null, "This departure is full. Please choose another departure.", StatusCodes.Status409Conflict);
        var fare = await db.FareRules.SingleOrDefaultAsync(rule => rule.RouteId == trip.RouteId && rule.PassengerCategory == passenger.Category && rule.IsActive, cancellationToken);
        if (fare is null) return (null, "No active fare rule exists for this passenger category and route.", StatusCodes.Status400BadRequest);
        var gateway = gateways.SingleOrDefault(item => item.Provider == request.Provider);
        if (gateway is null) return (null, "The requested payment provider is not available.", StatusCodes.Status400BadRequest);

        var booking = new Booking
        {
            TripId = trip.Id, PassengerId = passenger.Id, SeatNumber = BoardingReference(), Fare = fare.Amount,
            PassengerCategory = passenger.Category, QrCode = $"BKG-{Guid.NewGuid():N}".ToUpperInvariant(), Status = BookingStatus.Pending
        };
        var payment = new Payment
        {
            Booking = booking, Provider = request.Provider, ProviderOrderId = $"UPTS-{Guid.NewGuid():N}".ToUpperInvariant(),
            Amount = fare.Amount, Currency = "LKR", Status = PaymentStatus.Initiated
        };

        try
        {
            db.Payments.Add(payment);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (null, "This departure was just filled. Please choose another departure.", StatusCodes.Status409Conflict);
        }

        try
        {
            var (firstName, lastName) = SplitName(passenger.FullName);
            var session = await gateway.CreateCheckoutAsync(new PaymentCheckoutRequest(
                payment.ProviderOrderId, payment.Amount, payment.Currency, firstName, lastName,
                passenger.Email ?? "passenger@upts.lk", passenger.PhoneNumber, $"UPTS trip {trip.Route.RouteNumber}"), cancellationToken);
            payment.ProviderCheckoutId = session.ProviderCheckoutId;
            payment.ProviderPaymentId = session.ProviderPaymentId;
            payment.Status = PaymentStatus.Pending;
            payment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (session, null, StatusCodes.Status200OK);
        }
        catch (InvalidOperationException exception)
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            payment.Booking.Status = BookingStatus.Cancelled;
            payment.Booking.CancellationReason = "Checkout could not be started.";
            payment.Booking.CancelledAt = DateTime.UtcNow;
            payment.Booking.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (null, exception.Message, StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task HandleStripeCheckoutCompletedAsync(string eventId, string checkoutId, string? paymentIntentId, string? orderId, long? amountTotal, string? currency, CancellationToken cancellationToken)
    {
        if (await IsWebhookAlreadyProcessedAsync(eventId, cancellationToken)) return;
        var payment = await db.Payments.Include(item => item.Booking)
            .SingleOrDefaultAsync(item => item.Provider == PaymentProvider.Stripe && (item.ProviderCheckoutId == checkoutId || item.ProviderOrderId == orderId), cancellationToken);
        if (payment is null || !MatchesPayment(payment, amountTotal, currency)) return;

        payment.ProviderPaymentId = paymentIntentId ?? payment.ProviderPaymentId;
        payment.PaymentMethod = "Stripe Checkout";
        payment.Status = PaymentStatus.Succeeded;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Booking.Status = BookingStatus.Confirmed;
        payment.Booking.UpdatedAt = DateTime.UtcNow;
        db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { Provider = PaymentProvider.Stripe, ProviderEventId = eventId, EventType = "checkout.session.completed" });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReconcileStripeCheckoutAsync(string orderId, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.Include(item => item.Booking)
            .SingleOrDefaultAsync(item => item.Provider == PaymentProvider.Stripe && item.ProviderOrderId == orderId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Pending || string.IsNullOrWhiteSpace(payment.ProviderCheckoutId)) return;

        var gateway = gateways.SingleOrDefault(item => item.Provider == PaymentProvider.Stripe);
        var checkout = gateway is null ? null : await gateway.GetCheckoutStatusAsync(payment.ProviderCheckoutId, cancellationToken);
        if (checkout is null || !checkout.IsPaid || !MatchesPayment(payment, checkout.AmountMinor, checkout.Currency)) return;

        payment.ProviderPaymentId = checkout.ProviderPaymentId ?? payment.ProviderPaymentId;
        payment.PaymentMethod = "Stripe Checkout";
        payment.Status = PaymentStatus.Succeeded;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Booking.Status = BookingStatus.Confirmed;
        payment.Booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStripePaymentFailedAsync(string eventId, string paymentIntentId, CancellationToken cancellationToken)
    {
        if (await IsWebhookAlreadyProcessedAsync(eventId, cancellationToken)) return;
        var payment = await db.Payments.Include(item => item.Booking)
            .SingleOrDefaultAsync(item => item.Provider == PaymentProvider.Stripe && item.ProviderPaymentId == paymentIntentId, cancellationToken);
        if (payment is null || payment.Status == PaymentStatus.Succeeded) return;
        payment.Status = PaymentStatus.Failed;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Booking.Status = BookingStatus.Cancelled;
        payment.Booking.CancellationReason = "Payment was not completed.";
        payment.Booking.CancelledAt = DateTime.UtcNow;
        payment.Booking.UpdatedAt = DateTime.UtcNow;
        db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { Provider = PaymentProvider.Stripe, ProviderEventId = eventId, EventType = "payment_intent.payment_failed" });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStripeCheckoutExpiredAsync(string eventId, string checkoutId, CancellationToken cancellationToken)
    {
        if (await IsWebhookAlreadyProcessedAsync(eventId, cancellationToken)) return;
        var payment = await db.Payments.Include(item => item.Booking)
            .SingleOrDefaultAsync(item => item.Provider == PaymentProvider.Stripe && item.ProviderCheckoutId == checkoutId, cancellationToken);
        if (payment is null || payment.Status == PaymentStatus.Succeeded) return;
        payment.Status = PaymentStatus.Cancelled;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Booking.Status = BookingStatus.Cancelled;
        payment.Booking.CancellationReason = "Stripe Checkout expired before payment was completed.";
        payment.Booking.CancelledAt = DateTime.UtcNow;
        payment.Booking.UpdatedAt = DateTime.UtcNow;
        db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { Provider = PaymentProvider.Stripe, ProviderEventId = eventId, EventType = "checkout.session.expired" });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStripeRefundUpdatedAsync(string eventId, string refundId, string? status, CancellationToken cancellationToken)
    {
        if (await IsWebhookAlreadyProcessedAsync(eventId, cancellationToken)) return;
        var refund = await db.PaymentRefunds.Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.ProviderRefundId == refundId && item.Payment.Provider == PaymentProvider.Stripe, cancellationToken);
        if (refund is null) return;
        if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase)) refund.Status = PaymentRefundStatus.Succeeded;
        else if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)) refund.Status = PaymentRefundStatus.Failed;
        else return;
        refund.UpdatedAt = DateTime.UtcNow;
        refund.Payment.Status = refund.Status == PaymentRefundStatus.Succeeded ? PaymentStatus.Refunded : PaymentStatus.Succeeded;
        refund.Payment.UpdatedAt = DateTime.UtcNow;
        db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { Provider = PaymentProvider.Stripe, ProviderEventId = eventId, EventType = "refund.updated" });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(string? Error, int StatusCode)> RequestRefundAsync(Guid bookingId, string reason, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.Include(item => item.Payments).SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null) return ("Booking not found.", StatusCodes.Status404NotFound);
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) return ("Only active bookings can be cancelled.", StatusCodes.Status400BadRequest);
        var payment = booking.Payments.OrderByDescending(item => item.CreatedAt).FirstOrDefault(item => item.Status == PaymentStatus.Succeeded);
        if (payment is null) return ("This booking has no successful external payment to refund.", StatusCodes.Status400BadRequest);
        if (payment.Refunds.Any(item => item.Status == PaymentRefundStatus.Requested)) return ("A refund is already being processed for this booking.", StatusCodes.Status409Conflict);
        var gateway = gateways.SingleOrDefault(item => item.Provider == payment.Provider);
        if (gateway is null) return ("The original payment provider is unavailable.", StatusCodes.Status503ServiceUnavailable);

        var result = await gateway.RequestRefundAsync(new PaymentRefundRequest(payment.ProviderPaymentId!, payment.Amount, reason.Trim()), cancellationToken);
        var refund = new PaymentRefund { Payment = payment, Amount = payment.Amount, Reason = reason.Trim(), ProviderRefundId = result.ProviderRefundId, Status = result.IsAccepted ? PaymentRefundStatus.Requested : PaymentRefundStatus.Failed, FailureReason = result.FailureReason };
        db.PaymentRefunds.Add(refund);
        if (!result.IsAccepted)
        {
            await db.SaveChangesAsync(cancellationToken);
            return (result.FailureReason ?? "The payment provider rejected the refund.", StatusCodes.Status502BadGateway);
        }
        payment.Status = PaymentStatus.RefundPending;
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason.Trim();
        booking.CancelledAt = DateTime.UtcNow;
        booking.RefundAmount = payment.Amount;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (null, StatusCodes.Status204NoContent);
    }

    private Task<int> ActiveBookingCount(Guid tripId, CancellationToken cancellationToken) =>
        db.Bookings.CountAsync(booking => booking.TripId == tripId && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed), cancellationToken);

    private Task<bool> IsWebhookAlreadyProcessedAsync(string eventId, CancellationToken cancellationToken) =>
        db.PaymentWebhookEvents.AnyAsync(item => item.Provider == PaymentProvider.Stripe && item.ProviderEventId == eventId, cancellationToken);

    private static bool MatchesPayment(Payment payment, long? amountTotal, string? currency) =>
        amountTotal == decimal.ToInt64(payment.Amount * 100m) && string.Equals(currency, payment.Currency, StringComparison.OrdinalIgnoreCase);

    private static string BoardingReference() => $"B{Guid.NewGuid():N}"[..8].ToUpperInvariant();
    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var names = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return names.Length switch { 0 => ("Passenger", "UPTS"), 1 => (names[0], "UPTS"), _ => (names[0], names[1]) };
    }
}
