using System.Collections.Concurrent;
using api.Enums;
using api.Services.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace api.Tests.Shared;

/// <summary>Stripe replacement for tests: no network, records every call, and can be told to fail.</summary>
internal sealed class FakePaymentGateway : IPaymentGateway
{
    private int _checkoutCalls;
    private int _refundCalls;

    public PaymentProvider Provider => PaymentProvider.Stripe;
    public int CheckoutCalls => _checkoutCalls;
    public int RefundCalls => _refundCalls;
    public ConcurrentBag<PaymentCheckoutRequest> CheckoutRequests { get; } = [];
    public Exception? CheckoutFailure { get; set; }
    public PaymentRefundResult RefundResult { get; set; } = new(true, "re_test", null);
    public PaymentCheckoutStatus? CheckoutStatus { get; set; }
    public TimeSpan RefundDelay { get; set; } = TimeSpan.Zero;

    public Task<PaymentCheckoutSession> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _checkoutCalls);
        CheckoutRequests.Add(request);
        if (CheckoutFailure is not null) throw CheckoutFailure;
        return Task.FromResult(new PaymentCheckoutSession($"https://checkout.test/{request.OrderId}", $"cs_{request.OrderId}", $"pi_{request.OrderId}", request.OrderId));
    }

    public Task<PaymentCheckoutStatus?> GetCheckoutStatusAsync(string providerCheckoutId, CancellationToken cancellationToken) =>
        Task.FromResult(CheckoutStatus);

    public async Task<PaymentRefundResult> RequestRefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _refundCalls);
        if (RefundDelay > TimeSpan.Zero) await Task.Delay(RefundDelay, cancellationToken);
        return RefundResult;
    }

    /// <summary>Registers this fake instead of the real Stripe gateway.</summary>
    public Action<IServiceCollection> Register => services =>
    {
        services.RemoveAll<IPaymentGateway>();
        services.AddSingleton<IPaymentGateway>(this);
    };
}
