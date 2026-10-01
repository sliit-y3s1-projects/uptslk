import 'dart:async';

import 'package:flutter/material.dart';

import '../../core/time/service_time.dart';

import '../../core/theme/app_theme.dart';
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
  Timer? _clock;

  @override
  void initState() {
    super.initState();
    _results = _load();
    _clock = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (mounted) {
        setState(() {
          if (timer.tick % 30 == 0) _results = _load();
        });
      }
    });
  }

  @override
  void dispose() {
    _clock?.cancel();
    super.dispose();
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
      backgroundColor: AppTheme.surface,
      centerTitle: false,
    ),
    body: SafeArea(
      top: false,
      child: FutureBuilder<List<TripSearchResult>>(
        future: _results,
        builder: (context, snapshot) {
          if (!snapshot.hasData &&
              snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return _ResultError(
              onRetry: () => setState(() {
                _results = _load();
              }),
            );
          }
          final trips =
              (snapshot.data ?? const <TripSearchResult>[])
                  .where((trip) => trip.isOpenForBooking)
                  .toList()
                ..sort((a, b) => a.scheduledTime.compareTo(b.scheduledTime));
          return RefreshIndicator(
            onRefresh: () async {
              final request = _load();
              setState(() {
                _results = request;
              });
              // FutureBuilder renders a retry state if the refresh fails.
              try {
                await request;
              } catch (_) {}
            },
            child: CustomScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(22, 18, 22, 0),
                  sliver: SliverToBoxAdapter(
                    child: _SearchSummary(
                      origin: widget.origin,
                      destination: widget.destination,
                      date: widget.date,
                      passengerCount: widget.passengerCount,
                      onEdit: () => Navigator.of(context).maybePop(),
                    ),
                  ),
                ),
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(22, 30, 22, 14),
                  sliver: SliverToBoxAdapter(
                    child: _ResultsHeading(count: trips.length),
                  ),
                ),
                if (trips.isEmpty)
                  const SliverFillRemaining(
                    hasScrollBody: false,
                    child: _NoResults(),
                  )
                else
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(22, 0, 22, 36),
                    sliver: SliverList.separated(
                      itemCount: trips.length,
                      separatorBuilder: (_, _) => const SizedBox(height: 14),
                      itemBuilder: (_, index) => _TripCard(
                        trip: trips[index],
                        passengerCount: widget.passengerCount,
                        authStore: widget.authStore,
                        onOpenTickets: widget.onOpenTickets,
                      ),
                    ),
                  ),
              ],
            ),
          );
        },
      ),
    ),
  );
}

class _SearchSummary extends StatelessWidget {
  const _SearchSummary({
    required this.origin,
    required this.destination,
    required this.date,
    required this.passengerCount,
    required this.onEdit,
  });
  final String? origin;
  final String? destination;
  final DateTime date;
  final int passengerCount;
  final VoidCallback onEdit;

  @override
  Widget build(BuildContext context) => Container(
    width: double.infinity,
    padding: const EdgeInsets.all(20),
    decoration: BoxDecoration(
      color: AppTheme.surface,
      border: Border.all(color: AppTheme.borderStrong, width: 1.25),
      borderRadius: BorderRadius.circular(14),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Expanded(
              child: Text(
                'Your journey',
                style: TextStyle(
                  color: AppTheme.ink,
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            TextButton(onPressed: onEdit, child: const Text('Edit')),
          ],
        ),
        const SizedBox(height: 8),
        _SummaryPoint(
          icon: Icons.trip_origin_rounded,
          label: origin ?? 'All origins',
        ),
        Padding(
          padding: const EdgeInsets.only(left: 8),
          child: Container(
            width: 1,
            height: 15,
            color: AppTheme.brandPrimary.withValues(alpha: 0.3),
          ),
        ),
        _SummaryPoint(
          icon: Icons.location_on_rounded,
          label: destination ?? 'All destinations',
        ),
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 16),
          child: Divider(height: 1, color: AppTheme.borderStrong),
        ),
        Wrap(
          spacing: 9,
          runSpacing: 9,
          children: [
            _SummaryMeta(
              icon: Icons.calendar_month_outlined,
              label: _formatDate(date),
            ),
            _SummaryMeta(
              icon: Icons.people_outline_rounded,
              label:
                  '$passengerCount ${passengerCount == 1 ? 'seat' : 'seats'}',
            ),
          ],
        ),
        const SizedBox(height: 10),
        const Text(
          'Times shown in Sri Lanka time',
          style: TextStyle(color: AppTheme.muted, fontSize: 12),
        ),
      ],
    ),
  );
}

class _SummaryPoint extends StatelessWidget {
  const _SummaryPoint({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, size: 17, color: AppTheme.brandPrimary),
      const SizedBox(width: 11),
      Expanded(
        child: Text(
          label,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
    ],
  );
}

class _SummaryMeta extends StatelessWidget {
  const _SummaryMeta({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 7),
    decoration: BoxDecoration(
      color: AppTheme.surfaceMuted,
      borderRadius: BorderRadius.circular(6),
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

class _ResultsHeading extends StatelessWidget {
  const _ResultsHeading({required this.count});
  final int count;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Expanded(
        child: Text(
          'Select a departure',
          style: Theme.of(context).textTheme.titleMedium
              ?.copyWith(fontSize: 18),
        ),
      ),
      Text(
        '$count ${count == 1 ? 'trip' : 'trips'}',
        style: const TextStyle(
          color: AppTheme.muted,
          fontSize: 13,
          fontWeight: FontWeight.w600,
        ),
      ),
    ],
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
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        border: Border.all(color: AppTheme.borderStrong, width: 1.25),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Departs at',
                      style: TextStyle(
                        color: AppTheme.muted,
                        fontSize: 12,
                        fontWeight: FontWeight.w400,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      _formatTime(trip.scheduledTime),
                      style: const TextStyle(
                        color: AppTheme.ink,
                        fontSize: 25,
                        fontWeight: FontWeight.w700,
                        letterSpacing: -0.5,
                      ),
                    ),
                  ],
                ),
              ),
              _StatusPill(label: trip.status),
            ],
          ),
          const SizedBox(height: 18),
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceMuted,
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  trip.routeNumber,
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
                  trip.routeName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: AppTheme.muted, fontSize: 13),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            '${trip.origin} → ${trip.destination}',
            style: const TextStyle(
              color: AppTheme.muted,
              fontSize: 13,
              height: 1.5,
            ),
          ),
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 17),
            child: Divider(height: 1, color: AppTheme.borderStrong),
          ),
          Row(
            children: [
              Expanded(
                child: _TripMeta(
                  icon: Icons.directions_bus_outlined,
                  label: trip.bay.isEmpty ? 'Bay pending' : 'Bay ${trip.bay}',
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _TripMeta(
                  icon: Icons.event_seat_outlined,
                  label: '${trip.available} seats left',
                  color: canSelect ? AppTheme.ink : AppTheme.danger,
                ),
              ),
            ],
          ),
          const SizedBox(height: 17),
          Align(
            alignment: Alignment.centerRight,
            child: FilledButton(
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
              style: FilledButton.styleFrom(
                minimumSize: const Size(168, 46),
                padding: const EdgeInsets.symmetric(horizontal: 18),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
              ),
              child: Text(
                canSelect
                    ? 'Select departure'
                    : trip.available == 0
                    ? 'Sold out'
                    : 'Not enough seats',
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.label});
  final String label;

  @override
  Widget build(BuildContext context) {
    final normalized = label.toLowerCase();
    final color = normalized == 'delayed'
        ? AppTheme.warning
        : {'ready', 'boarding'}.contains(normalized)
        ? AppTheme.success
        : AppTheme.muted;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _TripMeta extends StatelessWidget {
  const _TripMeta({required this.icon, required this.label, this.color});
  final IconData icon;
  final String label;
  final Color? color;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, size: 17, color: color ?? AppTheme.muted),
      const SizedBox(width: 7),
      Expanded(
        child: Text(
          label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: color ?? AppTheme.muted,
            fontSize: 12,
            fontWeight: FontWeight.w500,
          ),
        ),
      ),
    ],
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
          style: TextStyle(color: AppTheme.ink, fontWeight: FontWeight.w600),
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
              fontWeight: FontWeight.w600,
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
  time = sriLankaTime(time);
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
