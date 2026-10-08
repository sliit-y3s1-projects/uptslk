using api.Data;
using api.Enums;
using api.Models;
using api.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

/// <summary>
/// Cancels bookings and gives the passenger's money back: a wallet refund with a refund transaction for wallet bookings,
/// and the existing Stripe refund for bookings paid by card. Used by staff cancelling one booking and by a trip
/// cancellation that cancels every active booking on the trip.
/// </summary>
public sealed class BookingCancellationService(
    AppDbContext db,
    BookingPaymentService bookingPayments,
    ILogger<BookingCancellationService> logger)
{
    public sealed record TripCancellationResult(bool Cancelled, string? Error, int StatusCode, int CancelledBookings, IReadOnlyList<Guid> UnrefundedCardBookings);

    public async Task<(string? Error, int StatusCode)> CancelBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason.Trim();
        var booking = await db.Bookings.Include(item => item.Payments).Include(item => item.Passenger).ThenInclude(passenger => passenger.Wallet)
            .SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null) return ("Booking not found.", StatusCodes.Status404NotFound);
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) return ("Only active bookings can be cancelled.", StatusCodes.Status400BadRequest);
        if (booking.Payments.Any(payment => payment.Status == PaymentStatus.Succeeded))
            return await bookingPayments.RequestRefundAsync(bookingId, reason, cancellationToken);
        if (booking.Payments.Count > 0)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (null, StatusCodes.Status204NoContent);
        }
        if (booking.Passenger.Wallet is null) return ("The passenger wallet does not exist.", StatusCodes.Status400BadRequest);

        // Lock the booking and wallet, then re-read them, so a duplicate cancel request cannot refund the booking twice.
        // When a trip cancellation already holds a transaction, this joins it instead of opening a second one.
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        await db.LockRowAsync("Bookings", booking.Id, cancellationToken);
        await db.LockRowAsync("Wallets", booking.Passenger.Wallet.Id, cancellationToken);
        await db.Entry(booking).ReloadAsync(cancellationToken);
        await db.Entry(booking.Passenger.Wallet).ReloadAsync(cancellationToken);
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) return ("Only active bookings can be cancelled.", StatusCodes.Status400BadRequest);
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason;
        booking.CancelledAt = DateTime.UtcNow;
        booking.RefundAmount = booking.Fare;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.Passenger.Wallet.Balance += booking.RefundAmount;
        booking.Passenger.Wallet.UpdatedAt = DateTime.UtcNow;
        db.Transactions.Add(new Transaction { WalletId = booking.Passenger.Wallet.Id, BookingId = booking.Id, Type = TransactionType.Refund, Amount = booking.RefundAmount });
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return (null, StatusCodes.Status204NoContent);
    }

    /// <summary>
    /// Cancels a trip and every active booking on it. The trip status change and all wallet refunds happen in one
    /// transaction under a row lock on the trip. Bookings paid by card are refunded through the existing Stripe path
    /// right after that transaction commits (that path needs its own transaction); any that fail stay Confirmed with a
    /// successful payment so staff can retry the refund, and they are returned in <see cref="TripCancellationResult.UnrefundedCardBookings"/>.
    /// </summary>
    public async Task<TripCancellationResult> CancelTripAsync(Guid tripId, string reason, string? note = null, CancellationToken cancellationToken = default)
    {
        reason = reason.Trim();
        var bookingReason = $"Trip cancelled: {reason}";
        if (bookingReason.Length > 1000) bookingReason = bookingReason[..1000];
        var cardBookings = new List<Guid>();
        var cancelledBookings = 0;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.LockRowAsync("Trips", tripId, cancellationToken);
            db.ChangeTracker.Clear(); // the caller may have loaded the trip; read it again now that the lock is held
            var trip = await db.Trips.SingleOrDefaultAsync(item => item.Id == tripId, cancellationToken);
            if (trip is null) return new(false, "Trip not found.", StatusCodes.Status404NotFound, 0, []);
            if (trip.Status is TripStatus.Completed or TripStatus.Cancelled)
                return new(false, "Only active trips can be cancelled.", StatusCodes.Status400BadRequest, 0, []);

            trip.Status = TripStatus.Cancelled;
            trip.CancellationReason = reason;
            if (!string.IsNullOrWhiteSpace(note)) trip.Notes = note.Trim();
            trip.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var bookingIds = await db.Bookings
                .Where(booking => booking.TripId == tripId && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
                .Select(booking => booking.Id)
                .ToListAsync(cancellationToken);
            foreach (var bookingId in bookingIds)
            {
                if (await db.Payments.AnyAsync(payment => payment.BookingId == bookingId && payment.Status == PaymentStatus.Succeeded, cancellationToken))
                {
                    cardBookings.Add(bookingId);
                    continue;
                }
                var (error, statusCode) = await CancelBookingAsync(bookingId, bookingReason, cancellationToken);
                // Leaving without committing rolls everything back, so the trip is not cancelled with money still owed.
                if (error is not null)
                    return new(false, $"The trip was not cancelled because booking {bookingId} could not be refunded: {error}", statusCode == StatusCodes.Status404NotFound ? StatusCodes.Status409Conflict : statusCode, 0, []);
                cancelledBookings++;
            }
            await transaction.CommitAsync(cancellationToken);
        }

        var unrefunded = new List<Guid>();
        foreach (var bookingId in cardBookings)
        {
            var (error, _) = await CancelBookingAsync(bookingId, bookingReason, cancellationToken);
            if (error is null) cancelledBookings++;
            else
            {
                unrefunded.Add(bookingId);
                logger.LogWarning("Trip {TripId} was cancelled but the card refund for booking {BookingId} failed: {Error}", tripId, bookingId, error);
            }
        }
        return new(true, null, StatusCodes.Status204NoContent, cancelledBookings, unrefunded);
    }
}
