import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_ui.dart';
import '../../models/trip_search_result.dart';
import '../../services/trip_search_api_service.dart';
import '../../state/auth_store.dart';
import 'booking_checkout_page.dart';

class SearchResultsPage extends StatefulWidget {
  const SearchResultsPage({
    super.key,
    this.origin,
    this.destination,
    this.routeId,
    this.directionId,
    required this.date,
    required this.passengerCount,
    required this.service,
    required this.authStore,
    this.onOpenTickets,
  });

  final String? origin;
  final String? destination;
  final String? routeId;
  final String? directionId;
  final DateTime date;
  final int passengerCount;
  final TripSearchApiService service;
  final AuthStore authStore;
  final VoidCallback? onOpenTickets;

  @override
  State<SearchResultsPage> createState() => _SearchResultsPageState();
}

class _SearchResultsPageState extends State<SearchResultsPage> {
  late Future<List<TripSearchResult>> _results;

  @override
  void initState() {
    super.initState();
    _results = _load();
  }

  Future<List<TripSearchResult>> _load() => widget.service.searchTrips(
    date: widget.date,
    routeId: widget.routeId,
    directionId: widget.directionId,
    origin: widget.origin,
    destination: widget.destination,
  );

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.background,
    appBar: AppBar(
      title: const Text('Departures'),
      backgroundColor: AppTheme.background,
      foregroundColor: AppTheme.ink,
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    body: FutureBuilder<List<TripSearchResult>>(
      future: _results,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return _ResultError(
            onRetry: () => setState(() => _results = _load()),
          );
        }
        final trips = snapshot.data ?? const <TripSearchResult>[];
        return Column(
          children: [
            _SearchSummary(
              origin: widget.origin,
              destination: widget.destination,
              date: widget.date,
              passengerCount: widget.passengerCount,
            ),
            Expanded(
              child: trips.isEmpty
                  ? const _NoResults()
                  : ListView.separated(
                      padding: const EdgeInsets.all(20),
                      itemCount: trips.length,
                      separatorBuilder: (_, _) => const SizedBox(height: 12),
                      itemBuilder: (_, index) => _TripCard(
                        trip: trips[index],
                        passengerCount: widget.passengerCount,
                        authStore: widget.authStore,
                        onOpenTickets: widget.onOpenTickets,
                      ),
                    ),
            ),
          ],
        );
      },
    ),
  );
}

class _SearchSummary extends StatelessWidget {
  const _SearchSummary({
    required this.origin,
    required this.destination,
    required this.date,
    required this.passengerCount,
  });
  final String? origin;
  final String? destination;
  final DateTime date;
  final int passengerCount;

  @override
  Widget build(BuildContext context) => AppSurface(
    width: double.infinity,
    margin: const EdgeInsets.fromLTRB(20, 6, 20, 0),
    padding: const EdgeInsets.all(16),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          origin ?? 'All origins',
          style: const TextStyle(
            color: AppTheme.ink,
            fontSize: 16,
            fontWeight: FontWeight.w700,
          ),
        ),
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 5),
          child: Icon(
            Icons.south_rounded,
            color: AppTheme.brandPrimary,
            size: 18,
          ),
        ),
        Text(
          destination ?? 'All destinations',
          style: const TextStyle(
            color: AppTheme.ink,
            fontSize: 16,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 14),
        Text(
          '${_formatDate(date)} · $passengerCount ${passengerCount == 1 ? 'passenger' : 'passengers'}',
          style: const TextStyle(color: AppTheme.muted, fontSize: 13),
        ),
      ],
    ),
  );
}

class _TripCard extends StatelessWidget {
  const _TripCard({
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
  Widget build(BuildContext context) {
    final canSelect = trip.isBookable && trip.available >= passengerCount;
    return AppSurface(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Text(
                _formatTime(trip.scheduledTime),
                style: const TextStyle(
                  color: AppTheme.ink,
                  fontSize: 22,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const Spacer(),
              _StatusPill(label: trip.status, available: canSelect),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            trip.routeNumber,
            style: const TextStyle(
              color: AppTheme.brandPrimary,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            trip.routeName,
            style: const TextStyle(color: AppTheme.muted, fontSize: 14),
          ),
          const SizedBox(height: 8),
          Text(
            '${trip.origin}  →  ${trip.destination}',
            style: const TextStyle(
              color: AppTheme.ink,
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              const Icon(
                Icons.directions_bus_outlined,
                color: AppTheme.muted,
                size: 18,
              ),
              const SizedBox(width: 7),
              Text(
                trip.bay.isEmpty ? 'Bay to be confirmed' : 'Bay ${trip.bay}',
                style: const TextStyle(color: AppTheme.muted),
              ),
              const Spacer(),
              Text(
                '${trip.available} spaces left',
                style: TextStyle(
                  color: canSelect ? AppTheme.success : AppTheme.danger,
                  fontWeight: FontWeight.w700,
                  fontSize: 13,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: canSelect
                ? () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => BookingCheckoutPage(
                        trip: trip,
                        passengerCount: passengerCount,
                        authStore: authStore,
                        onOpenTickets: onOpenTickets,
                      ),
                    ),
                  )
                : null,
            child: Text(canSelect ? 'Select departure' : 'Not available'),
          ),
        ],
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.label, required this.available});
  final String label;
  final bool available;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
    decoration: BoxDecoration(
      color: (available ? const Color(0xFFDCFCE7) : const Color(0xFFFFE4E6)),
      borderRadius: BorderRadius.circular(99),
    ),
    child: Text(
      label,
      style: TextStyle(
        color: available ? AppTheme.success : AppTheme.danger,
        fontSize: 12,
        fontWeight: FontWeight.w700,
      ),
    ),
  );
}

class _ResultError extends StatelessWidget {
  const _ResultError({required this.onRetry});
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) => Center(
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        const Icon(Icons.cloud_off_outlined, size: 36, color: AppTheme.muted),
        const SizedBox(height: 14),
        const Text(
          'Could not load departures.',
          style: TextStyle(color: AppTheme.ink, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 12),
        OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
      ],
    ),
  );
}

class _NoResults extends StatelessWidget {
  const _NoResults();
  @override
  Widget build(BuildContext context) => const Center(
    child: Padding(
      padding: EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.event_busy_outlined, size: 38, color: AppTheme.muted),
          SizedBox(height: 14),
          Text(
            'No departures found',
            style: TextStyle(
              color: AppTheme.ink,
              fontSize: 17,
              fontWeight: FontWeight.w700,
            ),
          ),
          SizedBox(height: 6),
          Text(
            'Try another date or journey.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppTheme.muted),
          ),
        ],
      ),
    ),
  );
}

String _formatTime(DateTime time) {
  final hour = time.hour == 0
      ? 12
      : (time.hour > 12 ? time.hour - 12 : time.hour);
  final suffix = time.hour >= 12 ? 'PM' : 'AM';
  return '${hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')} $suffix';
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
