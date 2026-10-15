using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class BookingCancellationService(
    IBookingRepository bookings,
    ITripRepository trips,
    IWalletRepository wallets,
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    IBookingPaymentService bookingPayments,
    IMobileNotificationService notifications,
    ILogger<BookingCancellationService> logger) : IBookingCancellationService
{
    private const string NotActive = "Only active bookings can be cancelled.";
    private const int MaxReasonLength = 1000;

    public async Task CancelBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason.Trim();
        var booking = await bookings.GetForCancellationAsync(bookingId, cancellationToken) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) throw new BadRequestException(NotActive);

        // Paid by card: the payment service cancels the booking and asks the provider for the refund.
        if (booking.Payments.Any(payment => payment.Status == PaymentStatus.Succeeded))
        {
            await bookingPayments.RequestRefundAsync(bookingId, reason, cancellationToken);
            return;
        }

        // A checkout was started but never paid, so there is no money to return.
        if (booking.Payments.Count > 0)
        {
            MarkCancelled(booking, reason);
            await notifications.BookingCancelledAsync(booking, RefundDestination.None, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var wallet = booking.Passenger.Wallet ?? throw new BadRequestException("The passenger wallet does not exist.");

        // Lock and re-read the booking and wallet so a repeated cancel cannot refund twice. Reuse a trip cancellation's transaction.
        await using var transaction = unitOfWork.HasActiveTransaction ? null : await unitOfWork.BeginTransactionAsync(cancellationToken);
        await bookings.LockAndReloadAsync(booking, cancellationToken);
        await wallets.LockAndReloadAsync(wallet, cancellationToken);
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) throw new BadRequestException(NotActive);

        MarkCancelled(booking, reason);
        booking.RefundAmount = booking.Fare;
        wallet.Balance += booking.RefundAmount;
        wallet.UpdatedAt = DateTime.UtcNow;
        wallets.AddTransaction(new Transaction { WalletId = wallet.Id, BookingId = booking.Id, Type = TransactionType.Refund, Amount = booking.RefundAmount });
        await notifications.BookingCancelledAsync(booking, RefundDestination.Wallet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    public async Task<TripCancellationResult> CancelTripAsync(Guid tripId, string reason, string? note = null, CancellationToken cancellationToken = default)
    {
        reason = reason.Trim();
        var bookingReason = $"Trip cancelled: {reason}";
        if (bookingReason.Length > MaxReasonLength) bookingReason = bookingReason[..MaxReasonLength];
        var cardBookings = new List<Guid>();
        var cancelledBookings = 0;

        // Step 1: cancel the trip and refund wallet bookings in one locked transaction, so no money is left owed on failure.
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            await trips.LockAsync(tripId, cancellationToken);
            // The caller may have loaded the trip already. Read it again now that the lock is held.
            unitOfWork.DiscardChanges();
            var trip = await trips.GetByIdAsync(tripId, cancellationToken) ?? throw new NotFoundException("Trip not found.");
            if (trip.Status is TripStatus.Completed or TripStatus.Cancelled) throw new BadRequestException("Only active trips can be cancelled.");

            trip.Status = TripStatus.Cancelled;
            trip.CancellationReason = reason;
            if (!string.IsNullOrWhiteSpace(note)) trip.Notes = note.Trim();
            trip.UpdatedAt = DateTime.UtcNow;
            await notifications.TripCancelledAsync(trip, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var bookingId in await bookings.GetActiveIdsForTripAsync(tripId, cancellationToken))
            {
                // Card refunds need their own transaction, so they run in step 2.
                if (await payments.HasSucceededForBookingAsync(bookingId, cancellationToken))
                {
                    cardBookings.Add(bookingId);
                    continue;
                }

                try
                {
                    await CancelBookingAsync(bookingId, bookingReason, cancellationToken);
                }
                catch (AppException exception)
                {
                    // A booking that vanished mid-way is a clash with another request, not a missing trip.
                    var statusCode = exception.StatusCode == StatusCodes.Status404NotFound ? StatusCodes.Status409Conflict : exception.StatusCode;
                    throw AppException.FromStatusCode(statusCode,
                        $"The trip was not cancelled because booking {bookingId} could not be refunded: {exception.Message}");
                }
                cancelledBookings++;
            }
            await transaction.CommitAsync(cancellationToken);
        }

        // Step 2: refund card bookings. A failed refund leaves that booking confirmed so staff can retry it.
        var unrefunded = new List<Guid>();
        foreach (var bookingId in cardBookings)
        {
            try
            {
                await CancelBookingAsync(bookingId, bookingReason, cancellationToken);
                cancelledBookings++;
            }
            catch (AppException exception)
            {
                unrefunded.Add(bookingId);
                logger.LogWarning("Trip {TripId} was cancelled but the card refund for booking {BookingId} failed: {Error}", tripId, bookingId, exception.Message);
            }
        }
        return new TripCancellationResult(cancelledBookings, unrefunded);
    }

    private static void MarkCancelled(Booking booking, string reason)
    {
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason;
        booking.CancelledAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
    }
}
