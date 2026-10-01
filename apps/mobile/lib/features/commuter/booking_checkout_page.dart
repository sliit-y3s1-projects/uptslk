import 'dart:async';

import 'package:flutter/material.dart';

import '../../core/time/service_time.dart';

import 'package:url_launcher/url_launcher.dart';

import '../../core/theme/app_theme.dart';
import '../../models/booking_checkout.dart';
import '../../models/trip_search_result.dart';
import '../../services/booking_api_service.dart';
import '../../state/auth_store.dart';

class BookingCheckoutPage extends StatefulWidget {
  const BookingCheckoutPage({
    super.key,
    required this.trip,
    required this.passengerCount,
    required this.authStore,
    this.onOpenTickets,
  });

  final TripSearchResult trip;
  final int passengerCount;
  final AuthStore authStore;
  final VoidCallback? onOpenTickets;

  @override
  State<BookingCheckoutPage> createState() => _BookingCheckoutPageState();
}

class _BookingCheckoutPageState extends State<BookingCheckoutPage>
    with WidgetsBindingObserver {
  final _service = BookingApiService();
  late Future<FareQuote> _quoteFuture;
  late int _passengerCount;
  bool _startingCheckout = false;
  bool _checkingPayment = false;
  CheckoutSession? _checkout;
  BookingPaymentStatus? _payment;
  String? _error;
  Timer? _departureTimer;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _passengerCount = widget.passengerCount;
    _quoteFuture = _loadQuote();
    final remaining = widget.trip.scheduledTime.difference(DateTime.now());
    _departureTimer = Timer(
      remaining.isNegative ? Duration.zero : remaining,
      () {
        if (mounted && _checkout == null) {
          setState(
            () => _error = 'Bookings have closed for this departure. Please choose a later trip.',
          );
        }
      },
    );
  }

  @override
  void dispose() {
    _departureTimer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed &&
        _checkout != null &&
        !_checkingPayment &&
        _payment?.succeeded != true) {
      _checkPayment();
    }
  }

  Future<FareQuote> _loadQuote() {
    final token = widget.authStore.token;
    if (token == null || token.isEmpty) {
      return Future.error(
        const BookingApiException('Your session has ended. Sign in again.'),
      );
    }
    return _service.getFareQuote(token: token, tripId: widget.trip.id);
  }

  Future<void> _startPayment() async {
    if (!widget.trip.isOpenForBooking) {
      setState(
        () => _error = 'Bookings have closed for this departure. Please choose a later trip.',
      );
      return;
    }
    final token = widget.authStore.token;
    if (token == null || token.isEmpty) {
      setState(() => _error = 'Your session has ended. Sign in again.');
      return;
    }

    setState(() {
      _startingCheckout = true;
      _error = null;
    });
    try {
      final checkout = await _service.startCheckout(
        token: token,
        tripId: widget.trip.id,
        passengerCount: _passengerCount,
      );
      if (!mounted) return;
      setState(() => _checkout = checkout);
      final opened = await launchUrl(
        Uri.parse(checkout.url),
        mode: LaunchMode.externalApplication,
      );
      if (!mounted) return;
      if (!opened) {
        setState(() => _error = 'Could not open the secure payment page.');
      }
    } on BookingApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } catch (_) {
      if (mounted) {
        setState(
          () => _error = 'Could not start payment. Check your connection.',
        );
      }
    } finally {
      if (mounted) setState(() => _startingCheckout = false);
    }
  }

  Future<void> _checkPayment() async {
    final checkout = _checkout;
    if (checkout == null) return;
    setState(() {
      _checkingPayment = true;
      _error = null;
    });
    try {
      final payment = await _service.getPaymentStatus(checkout.orderId);
      if (mounted) setState(() => _payment = payment);
    } on BookingApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } catch (_) {
      if (mounted) {
        setState(
          () => _error = 'Could not confirm payment. Check your connection.',
        );
      }
    } finally {
      if (mounted) setState(() => _checkingPayment = false);
    }
  }

  void _openTickets() {
    widget.onOpenTickets?.call();
    Navigator.of(context).popUntil((route) => route.isFirst);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.surface,
    appBar: AppBar(
      title: const Text('Confirm booking'),
      backgroundColor: AppTheme.surface,
      centerTitle: true,
    ),
    body: FutureBuilder<FareQuote>(
      future: _quoteFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError || snapshot.data == null) {
          return _QuoteError(
            message: snapshot.error is BookingApiException
                ? (snapshot.error! as BookingApiException).message
                : 'Could not calculate this fare.',
            onRetry: () {
              final quoteFuture = _loadQuote();
              setState(() {
                _quoteFuture = quoteFuture;
              });
            },
          );
        }
        return _buildCheckout(snapshot.data!);
      },
    ),
  );

  Widget _buildCheckout(FareQuote quote) {
    final total = quote.fare * _passengerCount;
    final maxPassengers = widget.trip.available.clamp(1, 10);
    final payment = _payment;
    return ListView(
      padding: const EdgeInsets.fromLTRB(22, 10, 22, 36),
      children: [
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: AppTheme.brandLight,
            borderRadius: BorderRadius.circular(24),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 6,
                    ),
                    decoration: BoxDecoration(
                      color: AppTheme.surface,
                      borderRadius: BorderRadius.circular(9),
                    ),
                    child: Text(
                      widget.trip.routeNumber,
                      style: const TextStyle(
                        color: AppTheme.brandPrimary,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      widget.trip.routeName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: AppTheme.muted,
                        fontSize: 13,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 18),
              _JourneyPoint(
                icon: Icons.trip_origin_rounded,
                label: widget.trip.origin,
              ),
              const SizedBox(height: 10),
              _JourneyPoint(
                icon: Icons.location_on_rounded,
                label: widget.trip.destination,
              ),
              const SizedBox(height: 18),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _JourneyMeta(
                    icon: Icons.calendar_month_outlined,
                    label: _formatDate(widget.trip.scheduledTime),
                  ),
                  _JourneyMeta(
                    icon: Icons.schedule_rounded,
                    label: _formatTime(widget.trip.scheduledTime),
                  ),
                  if (widget.trip.bay.isNotEmpty)
                    _JourneyMeta(
                      icon: Icons.directions_bus_outlined,
                      label: 'Bay ${widget.trip.bay}',
                    ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 28),
        Text(
          'Booking details',
          style: Theme.of(context).textTheme.titleMedium
              ?.copyWith(fontSize: 18),
        ),
        const SizedBox(height: 13),
        Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: AppTheme.surface,
            border: Border.all(color: AppTheme.borderStrong),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'Passengers',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 13),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      '$_passengerCount ${_passengerCount == 1 ? 'seat' : 'seats'}',
                      style: const TextStyle(
                        color: AppTheme.ink,
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  _CountButton(
                    icon: Icons.remove_rounded,
                    onPressed: _checkout == null && _passengerCount > 1
                        ? () => setState(() => _passengerCount--)
                        : null,
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 13),
                    child: Text(
                      '$_passengerCount',
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                  _CountButton(
                    icon: Icons.add_rounded,
                    onPressed:
                        _checkout == null && _passengerCount < maxPassengers
                        ? () => setState(() => _passengerCount++)
                        : null,
                  ),
                ],
              ),
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 18),
                child: Divider(height: 1),
              ),
              const Text(
                'Fare summary',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 14),
              _PriceRow(
                label: 'Standard fare × $_passengerCount',
                value: _currency(quote.fare * _passengerCount),
              ),
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 14),
                child: Divider(height: 1, color: AppTheme.border),
              ),
              _PriceRow(
                label: 'Total',
                value: _currency(total),
                emphasized: true,
              ),
            ],
          ),
        ),
        if (_error != null) ...[
          const SizedBox(height: 14),
          _MessagePanel(message: _error!, isError: true),
        ],
        if (_checkout == null) ...[
          const SizedBox(height: 20),
          FilledButton.icon(
            onPressed: _startingCheckout || !widget.trip.isOpenForBooking
                ? null
                : _startPayment,
            style: _primaryButtonStyle,
            icon: _startingCheckout
                ? const SizedBox.square(
                    dimension: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : const Icon(Icons.lock_outline_rounded),
            label: Text(
              _startingCheckout ? 'Preparing payment' : 'Continue to payment',
            ),
          ),
          const SizedBox(height: 10),
          const Text(
            'Payment opens in Stripe. Your ticket is confirmed only after a successful payment.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppTheme.muted, fontSize: 12, height: 1.4),
          ),
        ] else ...[
          const SizedBox(height: 18),
          _PaymentReturnPanel(
            payment: payment,
            checking: _checkingPayment,
            onCheck: _checkPayment,
            onOpenPayment: () => launchUrl(
              Uri.parse(_checkout!.url),
              mode: LaunchMode.externalApplication,
            ),
            onOpenTickets: _openTickets,
          ),
        ],
      ],
    );
  }
}

class _JourneyPoint extends StatelessWidget {
  const _JourneyPoint({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, size: 16, color: AppTheme.brandPrimary),
      const SizedBox(width: 10),
      Expanded(
        child: Text(
          label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
    ],
  );
}

class _JourneyMeta extends StatelessWidget {
  const _JourneyMeta({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
    decoration: BoxDecoration(
      color: AppTheme.surface,
      borderRadius: BorderRadius.circular(99),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 15, color: AppTheme.brandPrimary),
        const SizedBox(width: 6),
        Text(
          label,
          style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
        ),
      ],
    ),
  );
}

class _CountButton extends StatelessWidget {
  const _CountButton({required this.icon, this.onPressed});
  final IconData icon;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) => IconButton.outlined(
    onPressed: onPressed,
    icon: Icon(icon, size: 18),
    visualDensity: VisualDensity.compact,
  );
}

class _PriceRow extends StatelessWidget {
  const _PriceRow({
    required this.label,
    required this.value,
    this.emphasized = false,
  });
  final String label;
  final String value;
  final bool emphasized;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Expanded(
        child: Text(
          label,
          style: TextStyle(
            color: emphasized ? AppTheme.ink : AppTheme.muted,
            fontWeight: emphasized ? FontWeight.w700 : FontWeight.w400,
          ),
        ),
      ),
      Text(
        value,
        style: TextStyle(
          color: AppTheme.ink,
          fontSize: emphasized ? 19 : 15,
          fontWeight: emphasized ? FontWeight.w700 : FontWeight.w600,
        ),
      ),
    ],
  );
}

class _PaymentReturnPanel extends StatelessWidget {
  const _PaymentReturnPanel({
    required this.payment,
    required this.checking,
    required this.onCheck,
    required this.onOpenPayment,
    required this.onOpenTickets,
  });

  final BookingPaymentStatus? payment;
  final bool checking;
  final VoidCallback onCheck;
  final VoidCallback onOpenPayment;
  final VoidCallback onOpenTickets;

  @override
  Widget build(BuildContext context) {
    final succeeded = payment?.succeeded == true;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: succeeded ? const Color(0xFFECFDF3) : AppTheme.surface,
        border: Border.all(
          color: succeeded ? const Color(0xFFA7F3D0) : AppTheme.border,
        ),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Icon(
            succeeded ? Icons.check_circle_rounded : Icons.open_in_new_rounded,
            color: succeeded ? AppTheme.success : AppTheme.brandPrimary,
            size: 32,
          ),
          const SizedBox(height: 12),
          Text(
            succeeded ? 'Payment confirmed' : 'Complete payment in Stripe',
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: AppTheme.ink,
              fontSize: 18,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            succeeded
                ? 'Your booking is ready. Open My Tickets to view the boarding QR.'
                : payment == null || payment!.pending
                ? 'Return here after Stripe, then check the payment status.'
                : 'Payment status: ${payment!.status}. You can reopen Stripe or check again.',
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppTheme.muted, height: 1.4),
          ),
          const SizedBox(height: 18),
          if (succeeded)
            FilledButton.icon(
              onPressed: onOpenTickets,
              style: _primaryButtonStyle,
              icon: const Icon(Icons.confirmation_number_outlined),
              label: const Text('View my tickets'),
            )
          else ...[
            FilledButton.icon(
              onPressed: checking ? null : onCheck,
              style: _primaryButtonStyle,
              icon: checking
                  ? const SizedBox.square(
                      dimension: 18,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.refresh_rounded),
              label: Text(
                checking ? 'Checking payment' : 'Check payment status',
              ),
            ),
            const SizedBox(height: 8),
            TextButton.icon(
              onPressed: onOpenPayment,
              icon: const Icon(Icons.open_in_new_rounded, size: 18),
              label: const Text('Open payment again'),
            ),
          ],
        ],
      ),
    );
  }
}

class _MessagePanel extends StatelessWidget {
  const _MessagePanel({required this.message, required this.isError});
  final String message;
  final bool isError;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: isError ? const Color(0xFFFFF1F2) : const Color(0xFFECFDF3),
      borderRadius: BorderRadius.circular(14),
    ),
    child: Text(
      message,
      style: TextStyle(
        color: isError ? AppTheme.danger : AppTheme.success,
        fontWeight: FontWeight.w600,
      ),
    ),
  );
}

class _QuoteError extends StatelessWidget {
  const _QuoteError({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.receipt_long_outlined,
            color: AppTheme.muted,
            size: 38,
          ),
          const SizedBox(height: 14),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 14),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ),
    ),
  );
}

final ButtonStyle _primaryButtonStyle = FilledButton.styleFrom();

String _currency(double value) => 'LKR ${value.toStringAsFixed(2)}';

String _formatDate(DateTime date) {
  date = sriLankaTime(date);
  const months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];
  return '${date.day} ${months[date.month - 1]} ${date.year}';
}

String _formatTime(DateTime date) {
  date = sriLankaTime(date);
  final hour = date.hour == 0
      ? 12
      : (date.hour > 12 ? date.hour - 12 : date.hour);
  return '${hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')} ${date.hour >= 12 ? 'PM' : 'AM'}';
}
