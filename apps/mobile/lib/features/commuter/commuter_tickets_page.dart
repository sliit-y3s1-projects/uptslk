import 'dart:async';

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
  Timer? _timer;
  String _filter = 'Upcoming';

  @override
  void initState() {
    super.initState();
    _ticketsFuture = _load();
    _timer = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted) setState(() => _ticketsFuture = _load());
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
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
    try {
      await future;
    } catch (_) {
      /* FutureBuilder displays the error. */
    }
  }

  void _showTicket(MobileTicket ticket) {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      showDragHandle: false,
      backgroundColor: AppTheme.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => _TicketSheet(id: ticket.id, load: _load),
    );
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.surface,
    appBar: AppBar(title: const Text('My tickets'), centerTitle: true),
    body: FutureBuilder<List<MobileTicket>>(
      future: _ticketsFuture,
      builder: (context, snapshot) {
        if (!snapshot.hasData &&
            snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(
            child: TextButton(
              onPressed: _refresh,
              child: const Text('Could not load tickets. Tap to retry.'),
            ),
          );
        }
        final all = snapshot.data ?? <MobileTicket>[];
        final tickets = all.where((ticket) => ticket.group == _filter).toList()
          ..sort(
            (a, b) => _filter == 'Upcoming'
                ? a.tripTime.compareTo(b.tripTime)
                : b.tripTime.compareTo(a.tripTime),
          );
        return RefreshIndicator(
          onRefresh: _refresh,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(22, 10, 22, 36),
            children: [
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  for (final group in ['Upcoming', 'Past', 'Cancelled'])
                    ChoiceChip(
                      label: Text(
                        '$group (${all.where((ticket) => ticket.group == group).length})',
                      ),
                      selected: _filter == group,
                      showCheckmark: false,
                      selectedColor: AppTheme.brandLight,
                      onSelected: (_) => setState(() => _filter = group),
                    ),
                ],
              ),
              const SizedBox(height: 20),
              if (tickets.isEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 70),
                  child: Text(
                    'No ${_filter.toLowerCase()} bookings.',
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: AppTheme.muted),
                  ),
                ),
              for (final ticket in tickets)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: _TicketCard(
                    ticket: ticket,
                    onTap: () => _showTicket(ticket),
                  ),
                ),
            ],
          ),
        );
      },
    ),
  );
}

class _TicketCard extends StatelessWidget {
  const _TicketCard({required this.ticket, required this.onTap});
  final MobileTicket ticket;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Material(
    color: AppTheme.surface,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(16),
      side: const BorderSide(color: AppTheme.borderStrong),
    ),
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    ticket.route,
                    style: const TextStyle(
                      color: AppTheme.brandPrimary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                _StatusPill(ticket: ticket),
              ],
            ),
            const SizedBox(height: 10),
            Text(
              ticket.origin.isEmpty
                  ? ticket.routeName
                  : '${ticket.origin} to ${ticket.destination}',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            Text(
              _departure(ticket.tripTime),
              style: const TextStyle(color: AppTheme.muted, fontSize: 13),
            ),
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 14),
              child: Divider(height: 1),
            ),
            Row(
              children: [
                Expanded(
                  child: Text(
                    '${ticket.passengerCount} passenger${ticket.passengerCount == 1 ? '' : 's'} · LKR ${ticket.fare.toStringAsFixed(2)}',
                    style: const TextStyle(fontSize: 12, color: AppTheme.muted),
                  ),
                ),
                Text(
                  ticket.canBoard ? 'View ticket' : 'Details',
                  style: const TextStyle(
                    color: AppTheme.brandPrimary,
                    fontWeight: FontWeight.w600,
                    fontSize: 13,
                  ),
                ),
                const SizedBox(width: 4),
                const Icon(
                  Icons.chevron_right,
                  size: 18,
                  color: AppTheme.brandPrimary,
                ),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

class _TicketSheet extends StatefulWidget {
  const _TicketSheet({required this.id, required this.load});
  final String id;
  final Future<List<MobileTicket>> Function() load;
  @override
  State<_TicketSheet> createState() => _TicketSheetState();
}

class _TicketSheetState extends State<_TicketSheet> {
  late Future<List<MobileTicket>> _future;
  Timer? _timer;
  @override
  void initState() {
    super.initState();
    _future = widget.load();
    _timer = Timer.periodic(const Duration(seconds: 15), (_) {
      if (mounted) setState(() => _future = widget.load());
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    child: SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(24, 12, 24, 24),
      child: FutureBuilder<List<MobileTicket>>(
        future: _future,
        builder: (context, snapshot) {
          if (!snapshot.hasData &&
              snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return TextButton(
              onPressed: () => setState(() {
                _future = widget.load();
              }),
              child: const Text('Could not verify this booking. Tap to retry.'),
            );
          }
          final matches = snapshot.data?.where(
            (ticket) => ticket.id == widget.id,
          );
          if (matches == null || matches.isEmpty) {
            return const Text('Booking not found.');
          }
          final ticket = matches.first;
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      ticket.canBoard ? 'Boarding ticket' : 'Booking details',
                      style: const TextStyle(
                        fontSize: 21,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  IconButton(
                    onPressed: () => Navigator.pop(context),
                    icon: const Icon(Icons.close),
                    tooltip: 'Close',
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                '${ticket.route} · ${ticket.origin.isEmpty ? ticket.routeName : '${ticket.origin} to ${ticket.destination}'}',
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 10),
              Align(
                alignment: Alignment.centerLeft,
                child: _StatusPill(ticket: ticket),
              ),
              const SizedBox(height: 20),
              if (ticket.canBoard)
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    border: Border.all(color: AppTheme.borderStrong),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Column(
                    children: [
                      QrImageView(
                        data: ticket.qrCode,
                        version: QrVersions.auto,
                        errorCorrectionLevel: QrErrorCorrectLevel.M,
                        size: 220,
                        padding: const EdgeInsets.all(16),
                      ),
                      const SizedBox(height: 10),
                      const Text(
                        'Show this QR when boarding',
                        style: TextStyle(color: AppTheme.muted, fontSize: 13),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        ticket.qrCode,
                        textAlign: TextAlign.center,
                        style: const TextStyle(fontSize: 11),
                      ),
                    ],
                  ),
                )
              else
                const Text(
                  'This booking is not valid for boarding.',
                  style: TextStyle(color: AppTheme.muted),
                ),
              const SizedBox(height: 18),
              _Detail(label: 'Departure', value: _departure(ticket.tripTime)),
              _Detail(label: 'Passenger', value: ticket.passengerName),
              _Detail(label: 'Departure bay', value: ticket.bay),
              _Detail(label: 'Passengers', value: '${ticket.passengerCount}'),
              _Detail(
                label: 'Fare',
                value: 'LKR ${ticket.fare.toStringAsFixed(2)}',
              ),
              const SizedBox(height: 14),
              Text(
                'Booking reference: ${ticket.id}',
                style: const TextStyle(fontSize: 11, color: AppTheme.muted),
              ),
              const SizedBox(height: 10),
              const Text(
                'All departure times are in Sri Lanka time.',
                style: TextStyle(fontSize: 11, color: AppTheme.muted),
              ),
            ],
          );
        },
      ),
    ),
  );
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.ticket});
  final MobileTicket ticket;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
    decoration: BoxDecoration(
      color: ticket.canBoard ? const Color(0xFFECFDF3) : AppTheme.surfaceMuted,
      borderRadius: BorderRadius.circular(7),
    ),
    child: Text(
      ticket.displayStatus,
      style: TextStyle(
        color: ticket.canBoard ? AppTheme.success : AppTheme.muted,
        fontSize: 11,
        fontWeight: FontWeight.w600,
      ),
    ),
  );
}

class _Detail extends StatelessWidget {
  const _Detail({required this.label, required this.value});
  final String label;
  final String value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 8),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Text(
            label,
            style: const TextStyle(color: AppTheme.muted, fontSize: 13),
          ),
        ),
        Expanded(
          child: Text(
            value,
            textAlign: TextAlign.right,
            style: const TextStyle(fontWeight: FontWeight.w500, fontSize: 13),
          ),
        ),
      ],
    ),
  );
}

String _departure(DateTime value) {
  final date = value.toUtc().add(const Duration(hours: 5, minutes: 30));
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
  final hour = date.hour % 12 == 0 ? 12 : date.hour % 12;
  return '${date.day} ${months[date.month - 1]} ${date.year} · $hour:${date.minute.toString().padLeft(2, '0')} ${date.hour >= 12 ? 'PM' : 'AM'}';
}
