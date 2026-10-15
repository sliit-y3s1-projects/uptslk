using api.DTO.Payments;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class BookingPaymentService(
    IPassengerRepository passengers,
    ITripRepository trips,
    IBookingRepository bookings,
    IFareRuleRepository fareRules,
    IPaymentRepository payments,
    IPaymentRefundRepository refunds,
    IPaymentWebhookEventRepository webhookEvents,
    IUnitOfWork unitOfWork,
    IEnumerable<IPaymentGateway> gateways,
    IMobileNotificationService notifications) : IBookingPaymentService
{
    private const string NotEnoughSpaces = "This departure does not have enough remaining spaces for every passenger.";

    public async Task<PaymentCheckoutSession> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken)
    {
        var passenger = await passengers.GetByIdAsync(request.PassengerId, cancellationToken);
        var trip = await trips.GetWithVehicleAndRouteAsync(request.TripId, track: true, cancellationToken);
        if (passenger is null || !passenger.IsActive) throw new BadRequestException("The selected passenger is not active.");
        if (trip?.Vehicle is null || trip.Vehicle.Capacity < 1) throw new BadRequestException("This trip has no valid vehicle capacity configured.");
        if (!BookingEligibility.IsOpenForBooking(trip, DateTime.UtcNow)) throw new BadRequestException("Bookings have closed for this departure. Please choose a later trip.");
        if (request.PassengerCount is < 1 or > 10) throw new BadRequestException("Passenger count must be between 1 and 10.");

        if (await bookings.GetOccupiedCapacityAsync(trip.Id, cancellationToken) + request.PassengerCount > trip.Vehicle.Capacity)
            throw new ConflictException(NotEnoughSpaces);
        var fare = await fareRules.GetActiveForRouteAsync(trip.RouteId, cancellationToken)
            ?? throw new BadRequestException("No active standard fare exists for this route.");
        var gateway = gateways.SingleOrDefault(item => item.Provider == request.Provider)
            ?? throw new BadRequestException("The requested payment provider is not available.");

        var booking = new Booking
        {
            TripId = trip.Id,
            PassengerId = passenger.Id,
            SeatNumber = BoardingReference(),
            PassengerCount = request.PassengerCount,
            Fare = fare.Amount * request.PassengerCount,
            PassengerCategory = passenger.Category,
            QrCode = $"BKG-{Guid.NewGuid():N}".ToUpperInvariant(),
            Status = BookingStatus.Pending
        };
        var payment = new Payment
        {
            Booking = booking,
            Provider = request.Provider,
            ProviderOrderId = $"UPTS-{Guid.NewGuid():N}".ToUpperInvariant(),
            Amount = booking.Fare,
            Currency = "LKR",
            Status = PaymentStatus.Initiated
        };

        // The pending booking holds the seats, so check capacity and insert under a trip lock to avoid overselling.
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            await trips.LockAsync(trip.Id, cancellationToken);
            if (await bookings.GetOccupiedCapacityAsync(trip.Id, cancellationToken) + request.PassengerCount > trip.Vehicle.Capacity)
                throw new ConflictException(NotEnoughSpaces);
            try
            {
                payments.Add(payment);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new ConflictException("This departure was just filled. Please choose another departure.");
            }
        }

        // The seats are held before the provider is called, so a slow provider cannot cause an oversell.
        try
        {
            var (firstName, lastName) = SplitName(passenger.FullName);
            var session = await gateway.CreateCheckoutAsync(new PaymentCheckoutRequest(
                payment.ProviderOrderId, payment.Amount, payment.Currency, firstName, lastName,
                passenger.Email ?? "passenger@upts.lk", passenger.PhoneNumber, $"UPTS trip {trip.Route.RouteNumber}",
                request.UseMobileReturnUrl), cancellationToken);
            payment.ProviderCheckoutId = session.ProviderCheckoutId;
            payment.ProviderPaymentId = session.ProviderPaymentId;
            payment.Status = PaymentStatus.Pending;
            payment.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return session;
        }
        catch (InvalidOperationException exception)
        {
            // The provider is not set up, so release the held seats.
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            CancelBooking(payment.Booking, "Checkout could not be started.");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ServiceUnavailableException(exception.Message);
        }
    }

    public async Task HandleStripeCheckoutCompletedAsync(string eventId, string checkoutId, string? paymentIntentId, string? orderId, long? amountTotal, string? currency, CancellationToken cancellationToken)
    {
        if (await webhookEvents.IsProcessedAsync(PaymentProvider.Stripe, eventId, cancellationToken)) return;
        var payment = await payments.GetStripeForCheckoutAsync(checkoutId, orderId, cancellationToken);
        if (payment is null || !MatchesPayment(payment, amountTotal, currency)) return;
        // Stripe can send the same success more than once, and a refund may already have started.
        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.RefundPending or PaymentStatus.Refunded) return;

        await MarkPaidAsync(payment, paymentIntentId, cancellationToken);
        RecordWebhook(eventId, "checkout.session.completed");
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReconcileStripeCheckoutAsync(string orderId, CancellationToken cancellationToken)
    {
        var payment = await payments.GetStripeByOrderIdAsync(orderId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Pending || string.IsNullOrWhiteSpace(payment.ProviderCheckoutId)) return;

        var gateway = gateways.SingleOrDefault(item => item.Provider == PaymentProvider.Stripe);
        var checkout = gateway is null ? null : await gateway.GetCheckoutStatusAsync(payment.ProviderCheckoutId, cancellationToken);
        if (checkout is null || !checkout.IsPaid || !MatchesPayment(payment, checkout.AmountMinor, checkout.Currency)) return;

        await MarkPaidAsync(payment, checkout.ProviderPaymentId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStripePaymentFailedAsync(string eventId, string paymentIntentId, CancellationToken cancellationToken)
    {
        if (await webhookEvents.IsProcessedAsync(PaymentProvider.Stripe, eventId, cancellationToken)) return;
        var payment = await payments.GetStripeByPaymentIntentAsync(paymentIntentId, cancellationToken);
        if (payment is null || payment.Status == PaymentStatus.Succeeded) return;

        payment.Status = PaymentStatus.Failed;
        payment.UpdatedAt = DateTime.UtcNow;
        CancelBooking(payment.Booking, "Payment was not completed.");
        RecordWebhook(eventId, "payment_intent.payment_failed");
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStripeCheckoutExpiredAsync(string eventId, string checkoutId, CancellationToken cancellationToken)
    {
        if (await webhookEvents.IsProcessedAsync(PaymentProvider.Stripe, eventId, cancellationToken)) return;
        var payment = await payments.GetStripeByCheckoutIdAsync(checkoutId, cancellationToken);
        if (payment is null || payment.Status == PaymentStatus.Succeeded) return;

        payment.Status = PaymentStatus.Cancelled;
        payment.UpdatedAt = DateTime.UtcNow;
        CancelBooking(payment.Booking, "Stripe Checkout expired before payment was completed.");
        RecordWebhook(eventId, "checkout.session.expired");
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleStripeRefundUpdatedAsync(string eventId, string refundId, string? status, CancellationToken cancellationToken)
    {
        if (await webhookEvents.IsProcessedAsync(PaymentProvider.Stripe, eventId, cancellationToken)) return;
        var refund = await refunds.GetStripeByProviderRefundIdAsync(refundId, cancellationToken);
        if (refund is null) return;

        // Other refund states are still in progress, so there is nothing to record yet.
        if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase)) refund.Status = PaymentRefundStatus.Succeeded;
        else if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)) refund.Status = PaymentRefundStatus.Failed;
        else return;

        refund.UpdatedAt = DateTime.UtcNow;
        // A failed refund puts the payment back to succeeded so staff can try again.
        refund.Payment.Status = refund.Status == PaymentRefundStatus.Succeeded ? PaymentStatus.Refunded : PaymentStatus.Succeeded;
        refund.Payment.UpdatedAt = DateTime.UtcNow;
        await notifications.RefundUpdatedAsync(refund, cancellationToken);
        RecordWebhook(eventId, "refund.updated");
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestRefundAsync(Guid bookingId, string reason, CancellationToken cancellationToken)
    {
        reason = reason.Trim();

        // Lock, then read, so a second refund request for the same booking sees it is already cancelled.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await bookings.LockAsync(bookingId, cancellationToken);
        // The caller may already have loaded this booking. Read it again now that the lock is held.
        unitOfWork.DiscardChanges();
        var booking = await bookings.GetWithPaymentsAndRefundsAsync(bookingId, cancellationToken) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) throw new BadRequestException("Only active bookings can be cancelled.");

        var payment = booking.Payments.OrderByDescending(item => item.CreatedAt).FirstOrDefault(item => item.Status == PaymentStatus.Succeeded)
            ?? throw new BadRequestException("This booking has no successful external payment to refund.");
        if (payment.Refunds.Any(item => item.Status == PaymentRefundStatus.Requested))
            throw new ConflictException("A refund is already being processed for this booking.");
        var gateway = gateways.SingleOrDefault(item => item.Provider == payment.Provider)
            ?? throw new ServiceUnavailableException("The original payment provider is unavailable.");

        var result = await gateway.RequestRefundAsync(new PaymentRefundRequest(payment.ProviderPaymentId!, payment.Amount, reason), cancellationToken);
        refunds.Add(new PaymentRefund
        {
            Payment = payment,
            Amount = payment.Amount,
            Reason = reason,
            ProviderRefundId = result.ProviderRefundId,
            Status = result.IsAccepted ? PaymentRefundStatus.Requested : PaymentRefundStatus.Failed,
            FailureReason = result.FailureReason
        });

        // A rejected refund is still saved, so staff can see the attempt and why it failed.
        if (!result.IsAccepted)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new BadGatewayException(result.FailureReason ?? "The payment provider rejected the refund.");
        }

        payment.Status = PaymentStatus.RefundPending;
        CancelBooking(booking, reason);
        booking.RefundAmount = payment.Amount;
        await notifications.BookingCancelledAsync(booking, RefundDestination.Card, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task MarkPaidAsync(Payment payment, string? providerPaymentId, CancellationToken cancellationToken)
    {
        payment.ProviderPaymentId = providerPaymentId ?? payment.ProviderPaymentId;
        payment.PaymentMethod = "Stripe Checkout";
        payment.Status = PaymentStatus.Succeeded;
        payment.UpdatedAt = DateTime.UtcNow;
        if (payment.Booking.Status == BookingStatus.Pending)
        {
            payment.Booking.Status = BookingStatus.Confirmed;
            await notifications.BookingConfirmedAsync(payment.Booking, cancellationToken);
        }
        payment.Booking.UpdatedAt = DateTime.UtcNow;
    }

    private static void CancelBooking(Booking booking, string reason)
    {
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason;
        booking.CancelledAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
    }

    // Saving the event id makes each webhook run only once, even when Stripe sends it again.
    private void RecordWebhook(string eventId, string eventType) =>
        webhookEvents.Add(new PaymentWebhookEvent { Provider = PaymentProvider.Stripe, ProviderEventId = eventId, EventType = eventType });

    // Ignore a checkout whose amount or currency does not match what we asked for.
    private static bool MatchesPayment(Payment payment, long? amountTotal, string? currency) =>
        amountTotal == decimal.ToInt64(payment.Amount * 100m) && string.Equals(currency, payment.Currency, StringComparison.OrdinalIgnoreCase);

    private static string BoardingReference() => $"B{Guid.NewGuid():N}"[..8].ToUpperInvariant();

    // Stripe wants a first and last name. Use a placeholder when the passenger only has one name.
    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var names = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return names.Length switch { 0 => ("Passenger", "UPTS"), 1 => (names[0], "UPTS"), _ => (names[0], names[1]) };
    }
}
