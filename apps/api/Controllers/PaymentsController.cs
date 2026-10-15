using api.Common.Constants;
using api.DTO.Bookings;
using api.DTO.Payments;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController(IPaymentService payments) : ControllerBase
{
    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionResponse>> StartCheckout(StartCheckoutRequest request, CancellationToken cancellationToken) =>
        Ok(await payments.StartCheckoutAsync(request, cancellationToken));

    [Authorize(Roles = AppRoles.Commuter)]
    [HttpPost("checkout/me")]
    public async Task<ActionResult<CheckoutSessionResponse>> StartCheckoutForCurrentUser(CreateMyBookingRequest request, CancellationToken cancellationToken) =>
        Ok(await payments.StartCheckoutForCurrentUserAsync(request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("stripe/webhook")]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        await payments.HandleStripeWebhookAsync(json, Request.Headers["Stripe-Signature"], cancellationToken);
        return Ok();
    }

    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<PaymentStatusResponse>> GetByOrderId(string orderId, CancellationToken cancellationToken) =>
        Ok(await payments.GetByOrderIdAsync(orderId, cancellationToken));

    [AllowAnonymous]
    [HttpGet("mobile-return")]
    public async Task<IActionResult> MobileReturn([FromQuery] string orderId, CancellationToken cancellationToken) =>
        Redirect(await payments.GetMobileReturnUrlAsync(orderId, cancellationToken));

    [AllowAnonymous]
    [HttpGet("mobile-cancel")]
    public IActionResult MobileCancel([FromQuery] string orderId) =>
        Redirect(payments.GetMobileCancelUrl(orderId));

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPost("bookings/{bookingId:guid}/refund")]
    public async Task<IActionResult> Refund(Guid bookingId, RequestPaymentRefundRequest request, CancellationToken cancellationToken)
    {
        await payments.RequestRefundAsync(bookingId, request, cancellationToken);
        return NoContent();
    }
}
