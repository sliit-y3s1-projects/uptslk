using api.Data;
using api.DTOs;
using api.Enums;
using api.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Stripe;
using Stripe.Checkout;

namespace api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController(
    AppDbContext db,
    BookingPaymentService bookingPayments,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<IActionResult> StartCheckout(StartCheckoutRequest request, CancellationToken cancellationToken)
    {
        var (session, error, statusCode) = await bookingPayments.StartCheckoutAsync(request, cancellationToken);
        if (session is null) return StatusCode(statusCode, new { error });
        return Ok(new { url = session.Url });
    }

    [Authorize(Roles = "Commuter")]
    [HttpPost("checkout/me")]
    public async Task<IActionResult> StartCheckoutForCurrentUser(CreateMyBookingRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId)) return Unauthorized();
        var passengerId = await db.Passengers.AsNoTracking()
            .Where(item => item.UserId == parsedUserId)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!passengerId.HasValue) return BadRequest(new { error = "No passenger profile is linked to this account." });

        var (session, error, statusCode) = await bookingPayments.StartCheckoutAsync(new StartCheckoutRequest
        {
            TripId = request.TripId,
            PassengerId = passengerId.Value,
            PassengerCount = request.PassengerCount,
            Provider = PaymentProvider.Stripe
        }, cancellationToken);
        if (session is null) return StatusCode(statusCode, new { error });
        return Ok(new { url = session.Url });
    }

    [AllowAnonymous]
    [HttpPost("stripe/webhook")]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        var webhookSecret = configuration["Payments:Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret)) return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret);
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted when stripeEvent.Data.Object is Session session:
                    string? orderId = null;
                    if (session.Metadata is not null) session.Metadata.TryGetValue("upts_order_id", out orderId);
                    await bookingPayments.HandleStripeCheckoutCompletedAsync(stripeEvent.Id, session.Id, session.PaymentIntentId, orderId, session.AmountTotal, session.Currency, cancellationToken);
                    break;
                case EventTypes.PaymentIntentPaymentFailed when stripeEvent.Data.Object is PaymentIntent intent:
                    await bookingPayments.HandleStripePaymentFailedAsync(stripeEvent.Id, intent.Id, cancellationToken);
                    break;
                case EventTypes.CheckoutSessionExpired when stripeEvent.Data.Object is Session expiredSession:
                    await bookingPayments.HandleStripeCheckoutExpiredAsync(stripeEvent.Id, expiredSession.Id, cancellationToken);
                    break;
                case EventTypes.RefundUpdated when stripeEvent.Data.Object is Refund refund:
                    await bookingPayments.HandleStripeRefundUpdatedAsync(stripeEvent.Id, refund.Id, refund.Status, cancellationToken);
                    break;
            }
            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest();
        }
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetByOrderId(string orderId, CancellationToken cancellationToken)
    {
        await bookingPayments.ReconcileStripeCheckoutAsync(orderId, cancellationToken);
        var payment = await db.Payments.AsNoTracking().Include(item => item.Booking)
            .SingleOrDefaultAsync(item => item.ProviderOrderId == orderId, cancellationToken);
        if (payment is null) return NotFound();
        return Ok(new { payment.BookingId, payment.Status, payment.Amount, payment.Currency, payment.Provider, payment.UpdatedAt });
    }

    [HttpPost("bookings/{bookingId:guid}/refund")]
    public async Task<IActionResult> Refund(Guid bookingId, RequestPaymentRefundRequest request, CancellationToken cancellationToken)
    {
        var (error, statusCode) = await bookingPayments.RequestRefundAsync(bookingId, request.Reason, cancellationToken);
        return error is null ? NoContent() : StatusCode(statusCode, new { error });
    }
}
