using api.DTO.Bookings;
using api.DTO.Payments;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Stripe;
using Stripe.Checkout;

namespace api.Services;

public sealed class PaymentService(
    BookingPaymentService bookingPayments,
    IPaymentRepository payments,
    IPassengerRepository passengers,
    ICurrentUserService currentUser,
    IConfiguration configuration) : IPaymentService
{
    private const string DefaultMobileScheme = "uptslk";

    public async Task<CheckoutSessionResponse> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken)
    {
        var (session, error, statusCode) = await bookingPayments.StartCheckoutAsync(request, cancellationToken);
        if (session is null) throw AppException.FromStatusCode(statusCode, error!);
        return new CheckoutSessionResponse(session.Url, session.OrderId);
    }

    public async Task<CheckoutSessionResponse> StartCheckoutForCurrentUserAsync(CreateMyBookingRequest request, CancellationToken cancellationToken)
    {
        var passengerId = await passengers.GetIdByUserIdAsync(currentUser.RequireUserId(), cancellationToken)
            ?? throw new BadRequestException("No passenger profile is linked to this account.");

        return await StartCheckoutAsync(new StartCheckoutRequest
        {
            TripId = request.TripId,
            PassengerId = passengerId,
            PassengerCount = request.PassengerCount,
            Provider = PaymentProvider.Stripe,
            UseMobileReturnUrl = request.UseMobileReturnUrl
        }, cancellationToken);
    }

    public async Task HandleStripeWebhookAsync(string json, string? signature, CancellationToken cancellationToken)
    {
        var webhookSecret = configuration["Payments:Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret)) throw new ServiceUnavailableException("Stripe webhooks are not configured.");
        // Without the header Stripe's verifier throws a NullReferenceException instead of rejecting the call.
        if (string.IsNullOrWhiteSpace(signature)) throw new BadRequestException("The Stripe-Signature header is missing.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
        }
        catch (StripeException)
        {
            throw new BadRequestException("The Stripe webhook could not be verified.");
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted when stripeEvent.Data.Object is Session session:
                string? orderId = null;
                session.Metadata?.TryGetValue("upts_order_id", out orderId);
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
    }

    public async Task<PaymentStatusResponse> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken)
    {
        // Ask Stripe for the latest state first, in case the webhook has not arrived yet.
        await bookingPayments.ReconcileStripeCheckoutAsync(orderId, cancellationToken);
        var payment = await payments.GetByOrderIdAsync(orderId, cancellationToken) ?? throw new NotFoundException("Payment not found.");
        return PaymentStatusResponse.From(payment);
    }

    public async Task<string> GetMobileReturnUrlAsync(string orderId, CancellationToken cancellationToken)
    {
        EnsureOrderId(orderId);
        await bookingPayments.ReconcileStripeCheckoutAsync(orderId, cancellationToken);
        return MobileAppUrl("payment-return", orderId);
    }

    public string GetMobileCancelUrl(string orderId)
    {
        EnsureOrderId(orderId);
        return MobileAppUrl("payment-cancel", orderId);
    }

    public async Task RequestRefundAsync(Guid bookingId, RequestPaymentRefundRequest request, CancellationToken cancellationToken)
    {
        var (error, statusCode) = await bookingPayments.RequestRefundAsync(bookingId, request.Reason, cancellationToken);
        if (error is not null) throw AppException.FromStatusCode(statusCode, error);
    }

    private static void EnsureOrderId(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId)) throw new BadRequestException("An order id is required.");
    }

    private string MobileAppUrl(string host, string orderId)
    {
        var scheme = configuration["Payments:Stripe:MobileAppScheme"] ?? DefaultMobileScheme;
        if (!Uri.CheckSchemeName(scheme)) scheme = DefaultMobileScheme;
        return $"{scheme}://{host}?orderId={Uri.EscapeDataString(orderId)}";
    }
}
