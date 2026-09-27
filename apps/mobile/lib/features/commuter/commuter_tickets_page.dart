import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../core/theme/app_theme.dart';
import '../../models/mobile_ticket.dart';
import '../../services/booking_api_service.dart';
import '../../state/auth_store.dart';

class CommuterTicketsPage extends StatefulWidget {
  const CommuterTicketsPage({
    super.key,
    required this.authStore,
    this.refreshSignal = 0,
  });

  final AuthStore authStore;
  final int refreshSignal;

  @override
  State<CommuterTicketsPage> createState() => _CommuterTicketsPageState();
}

class _CommuterTicketsPageState extends State<CommuterTicketsPage> {
  final _service = BookingApiService();
  late Future<List<MobileTicket>> _ticketsFuture;

  @override
  void initState() {
    super.initState();
    _ticketsFuture = _load();
  }

  @override
  void didUpdateWidget(covariant CommuterTicketsPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.refreshSignal != widget.refreshSignal) {
      _ticketsFuture = _load();
    }
  }

  Future<List<MobileTicket>> _load() {
    final token = widget.authStore.token;
    if (token == null || token.isEmpty) {
      return Future.error(
        const BookingApiException('Your session has ended. Sign in again.'),
      );
    }
    return _service.getMyTickets(token);
  }

  Future<void> _refresh() async {
    final future = _load();
    setState(() => _ticketsFuture = future);
    await future;
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.background,
    appBar: AppBar(
      title: const Text(
        'My tickets',
        style: TextStyle(fontWeight: FontWeight.w800),
      ),
      backgroundColor: AppTheme.background,
      foregroundColor: AppTheme.ink,
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    body: FutureBuilder<List<MobileTicket>>(
      future: _ticketsFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return _TicketsError(
            message: snapshot.error is BookingApiException
                ? (snapshot.error! as BookingApiException).message
                : 'Could not load your tickets.',
            onRetry: () => setState(() => _ticketsFuture = _load()),
          );
        }
        final tickets = snapshot.data ?? const <MobileTicket>[];
        if (tickets.isEmpty) {
          return RefreshIndicator(
            onRefresh: _refresh,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              children: const [SizedBox(height: 180), _EmptyTickets()],
            ),
          );
        }

        final active = tickets.where((ticket) => ticket.isActive).toList();
        final past = tickets.where((ticket) => !ticket.isActive).toList();
        return RefreshIndicator(
          onRefresh: _refresh,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 6, 20, 32),
            children: [
              if (active.isNotEmpty) ...[
                _SectionTitle(label: 'Upcoming', count: active.length),
                const SizedBox(height: 12),
                ...active.map(
                  (ticket) => Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: _TicketCard(
                      ticket: ticket,
                      onTap: () => _showTicket(context, ticket),
                    ),
                  ),
                ),
              ],
              if (past.isNotEmpty) ...[
                if (active.isNotEmpty) const SizedBox(height: 18),
                _SectionTitle(label: 'Past bookings', count: past.length),
                const SizedBox(height: 12),
                ...past.map(
                  (ticket) => Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: _TicketCard(
                      ticket: ticket,
                      onTap: () => _showTicket(context, ticket),
                    ),
                  ),
                ),
              ],
            ],
          ),
        );
      },
    ),
  );

  void _showTicket(BuildContext context, MobileTicket ticket) {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: AppTheme.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (context) => SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(24, 12, 24, 28),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Center(
                child: Container(
                  width: 44,
                  height: 4,
                  decoration: BoxDecoration(
                    color: AppTheme.border,
                    borderRadius: BorderRadius.circular(99),
                  ),
                ),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  const Expanded(
                    child: Text(
                      'Boarding ticket',
                      style: TextStyle(
                        color: AppTheme.ink,
                        fontSize: 22,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  _StatusPill(status: ticket.status),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                '${ticket.route} · ${ticket.routeName}',
                style: const TextStyle(color: AppTheme.muted),
              ),
              const SizedBox(height: 22),
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: Colors.white,
                  border: Border.all(color: AppTheme.border),
                  borderRadius: BorderRadius.circular(18),
                ),
                child: Column(
                  children: [
                    QrImageView(
                      data: ticket.qrCode.isEmpty ? ticket.id : ticket.qrCode,
                      version: QrVersions.auto,
                      size: 210,
                      padding: const EdgeInsets.all(4),
                    ),
                    const SizedBox(height: 12),
                    Text(
                      ticket.status == 'Confirmed'
                          ? 'Show this QR when boarding'
                          : ticket.status == 'Pending'
                          ? 'This QR becomes valid after payment is confirmed'
                          : 'Booking reference',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: AppTheme.muted,
                        fontSize: 13,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      ticket.qrCode.isEmpty ? ticket.id : ticket.qrCode,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: AppTheme.ink,
                        fontWeight: FontWeight.w700,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 18),
              _TicketDetailRow(
                label: 'Departure',
                value:
                    '${_formatDate(ticket.tripTime)} · ${_formatTime(ticket.tripTime)}',
              ),
              _TicketDetailRow(
                label: 'Passengers',
                value: '${ticket.passengerCount}',
              ),
              _TicketDetailRow(
                label: 'Total paid',
                value: 'LKR ${ticket.fare.toStringAsFixed(2)}',
              ),
              const SizedBox(height: 10),
              FilledButton(
                onPressed: () => Navigator.pop(context),
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(50),
                  backgroundColor: AppTheme.brandPrimary,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(15),
                  ),
                ),
                child: const Text('Done'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle({required this.label, required this.count});
  final String label;
  final int count;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Expanded(
        child: Text(
          label,
          style: const TextStyle(
            color: AppTheme.ink,
            fontSize: 17,
            fontWeight: FontWeight.w800,
          ),
        ),
      ),
      Text('$count', style: const TextStyle(color: AppTheme.muted)),
    ],
  );
}

class _TicketCard extends StatelessWidget {
  const _TicketCard({required this.ticket, required this.onTap});
  final MobileTicket ticket;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: AppTheme.surface,
    borderRadius: BorderRadius.circular(18),
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(18),
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          border: Border.all(color: AppTheme.border),
          borderRadius: BorderRadius.circular(18),
        ),
        child: Row(
          children: [
            Container(
              width: 50,
              height: 50,
              decoration: BoxDecoration(
                color: AppTheme.brandLight,
                borderRadius: BorderRadius.circular(14),
              ),
              child: const Icon(
                Icons.qr_code_2_rounded,
                color: AppTheme.brandPrimary,
                size: 27,
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    ticket.route,
                    style: const TextStyle(
                      color: AppTheme.ink,
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    ticket.routeName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppTheme.muted),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${_formatDate(ticket.tripTime)} · ${_formatTime(ticket.tripTime)}',
                    style: const TextStyle(
                      color: AppTheme.ink,
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 10),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                _StatusPill(status: ticket.status),
                const SizedBox(height: 14),
                const Icon(Icons.chevron_right_rounded, color: AppTheme.muted),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    final confirmed = status == 'Confirmed';
    final pending = status == 'Pending';
    final color = confirmed
        ? AppTheme.success
        : pending
        ? AppTheme.warning
        : AppTheme.muted;
    final background = confirmed
        ? const Color(0xFFECFDF3)
        : pending
        ? const Color(0xFFFFF7ED)
        : const Color(0xFFF2F4F7);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(99),
      ),
      child: Text(
        status,
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

class _TicketDetailRow extends StatelessWidget {
  const _TicketDetailRow({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 9),
    child: Row(
      children: [
        Expanded(
          child: Text(label, style: const TextStyle(color: AppTheme.muted)),
        ),
        Text(
          value,
          style: const TextStyle(
            color: AppTheme.ink,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    ),
  );
}

class _EmptyTickets extends StatelessWidget {
  const _EmptyTickets();

  @override
  Widget build(BuildContext context) => const Padding(
    padding: EdgeInsets.all(32),
    child: Column(
      children: [
        Icon(
          Icons.confirmation_number_outlined,
          size: 42,
          color: AppTheme.muted,
        ),
        SizedBox(height: 14),
        Text(
          'No tickets yet',
          style: TextStyle(
            color: AppTheme.ink,
            fontSize: 18,
            fontWeight: FontWeight.w800,
          ),
        ),
        SizedBox(height: 6),
        Text(
          'Choose a departure from Home to make your first booking.',
          textAlign: TextAlign.center,
          style: TextStyle(color: AppTheme.muted, height: 1.4),
        ),
      ],
    ),
  );
}

class _TicketsError extends StatelessWidget {
  const _TicketsError({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off_outlined, size: 38, color: AppTheme.muted),
          const SizedBox(height: 14),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 14),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ),
    ),
  );
}

String _formatDate(DateTime date) {
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
  final hour = date.hour == 0
      ? 12
      : (date.hour > 12 ? date.hour - 12 : date.hour);
  return '${hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')} ${date.hour >= 12 ? 'PM' : 'AM'}';
}
