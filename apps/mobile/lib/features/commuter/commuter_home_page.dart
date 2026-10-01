import 'package:flutter/material.dart';

import '../../core/time/service_time.dart';

import '../../core/theme/app_theme.dart';
import '../../models/journey_option.dart';
import '../../models/transit_centre.dart';
import '../../services/trip_search_api_service.dart';
import '../../state/auth_store.dart';
import 'search_results_page.dart';

class CommuterHomePage extends StatefulWidget {
  const CommuterHomePage({
    super.key,
    required this.firstName,
    required this.authStore,
    this.onOpenTickets,
    this.service,
  });

  final String firstName;
  final AuthStore authStore;
  final VoidCallback? onOpenTickets;
  final TripSearchApiService? service;

  @override
  State<CommuterHomePage> createState() => _CommuterHomePageState();
}

class _CommuterHomePageState extends State<CommuterHomePage> {
  late final TripSearchApiService _service;
  late Future<List<TransitCentre>> _centresFuture;
  late Future<List<JourneyOption>> _journeysFuture;
  TransitCentre? _origin;
  TransitCentre? _destination;
  DateTime _date = serviceToday();
  int _passengerCount = 1;

  @override
  void initState() {
    super.initState();
    _service = widget.service ?? TripSearchApiService();
    _centresFuture = _service.getOperatingCentres();
    _journeysFuture = _service.getJourneyOptions();
  }

  Future<void> _chooseDate() async {
    final now = serviceToday();
    final selected = await showDatePicker(
      context: context,
      initialDate: _date.isBefore(DateUtils.dateOnly(now)) ? now : _date,
      firstDate: now,
      lastDate: now.add(const Duration(days: 90)),
    );
    if (selected != null && mounted) setState(() => _date = selected);
  }

  Future<void> _chooseCentre({
    required List<TransitCentre> centres,
    required bool isOrigin,
  }) async {
    final selected = await showModalBottomSheet<TransitCentre>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: AppTheme.surface,
      showDragHandle: false,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(18)),
      ),
      builder: (_) => _CentreSheet(
        title: isOrigin ? 'Choose departure centre' : 'Choose arrival centre',
        centres: centres,
        selectedId: (isOrigin ? _origin : _destination)?.id,
      ),
    );
    if (selected == null || !mounted) return;
    setState(() {
      if (isOrigin) {
        _origin = selected;
      } else {
        _destination = selected;
      }
    });
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.background,
    body: SafeArea(
      child: FutureBuilder<List<TransitCentre>>(
        future: _centresFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return _LoadError(
              message: 'Could not load journey locations.',
              onRetry: () => setState(() {
                _centresFuture = _service.getOperatingCentres();
              }),
            );
          }
          final centres = snapshot.data ?? const <TransitCentre>[];
          if (centres.length < 2) {
            return const _EmptyCentres();
          }
          return SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(22, 28, 22, 36),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 560),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'Plan a journey',
                      style: TextStyle(
                        color: AppTheme.ink,
                        fontSize: 28,
                        fontWeight: FontWeight.w600,
                        height: 1.16,
                        letterSpacing: -0.9,
                      ),
                    ),
                    const SizedBox(height: 6),
                    const Text(
                      'Find a route and choose your departure.',
                      style: TextStyle(color: AppTheme.muted, fontSize: 14),
                    ),
                    const SizedBox(height: 24),
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: AppTheme.surface,
                        border: Border.all(
                          color: AppTheme.borderStrong,
                          width: 1.25,
                        ),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Row(
                            children: [
                              const Expanded(
                                child: _SectionTitle('Journey details'),
                              ),
                              if (_origin != null || _destination != null)
                                TextButton(
                                  onPressed: () => setState(() {
                                    _origin = null;
                                    _destination = null;
                                  }),
                                  child: const Text('Clear'),
                                ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          _CentreField(
                            label: 'From',
                            placeholder: 'Select departure centre',
                            centre: _origin,
                            onTap: () =>
                                _chooseCentre(centres: centres, isOrigin: true),
                          ),
                          const Divider(height: 1),
                          _CentreField(
                            label: 'To',
                            placeholder: 'Select arrival centre',
                            centre: _destination,
                            onTap: () => _chooseCentre(
                              centres: centres,
                              isOrigin: false,
                            ),
                          ),
                          const SizedBox(height: 16),
                          LayoutBuilder(
                            builder: (context, constraints) {
                              final date = _DateField(
                                date: _date,
                                onTap: _chooseDate,
                              );
                              final seats = _PassengerStepper(
                                value: _passengerCount,
                                onChanged: (value) =>
                                    setState(() => _passengerCount = value),
                              );
                              if (constraints.maxWidth < 290 ||
                                  MediaQuery.textScalerOf(context).scale(14) >
                                      19) {
                                return Column(
                                  children: [
                                    date,
                                    const SizedBox(height: 12),
                                    seats,
                                  ],
                                );
                              }
                              return Row(
                                children: [
                                  Expanded(child: date),
                                  const SizedBox(width: 12),
                                  SizedBox(width: 136, child: seats),
                                ],
                              );
                            },
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 28),
                    FutureBuilder<List<JourneyOption>>(
                      future: _journeysFuture,
                      builder: (context, journeysSnapshot) {
                        if (journeysSnapshot.connectionState ==
                            ConnectionState.waiting) {
                          return const Center(
                            child: Padding(
                              padding: EdgeInsets.all(28),
                              child: CircularProgressIndicator(),
                            ),
                          );
                        }
                        if (journeysSnapshot.hasError) {
                          return _LoadError(
                            message: 'Could not load routes.',
                            onRetry: () => setState(() {
                              _journeysFuture = _service.getJourneyOptions();
                            }),
                          );
                        }
                        final journeys =
                            (journeysSnapshot.data ?? const <JourneyOption>[])
                                .where(
                                  (journey) =>
                                      _matches(journey.origin, _origin?.name) &&
                                      _matches(
                                        journey.destination,
                                        _destination?.name,
                                      ),
                                )
                                .toList();
                        return _JourneyList(
                          journeys: journeys,
                          hasFilters: _origin != null || _destination != null,
                          date: _date,
                          passengerCount: _passengerCount,
                          service: _service,
                          authStore: widget.authStore,
                          onOpenTickets: widget.onOpenTickets,
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    ),
  );
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.title);
  final String title;

  @override
  Widget build(BuildContext context) => Text(
    title,
    style: const TextStyle(
      color: AppTheme.ink,
      fontSize: 17,
      fontWeight: FontWeight.w700,
      letterSpacing: -0.3,
    ),
  );
}

class _CentreField extends StatelessWidget {
  const _CentreField({
    required this.label,
    required this.placeholder,
    required this.centre,
    required this.onTap,
  });

  final String label;
  final String placeholder;
  final TransitCentre? centre;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppTheme.surface,
      borderRadius: BorderRadius.circular(13),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(13),
        child: Container(
          width: double.infinity,
          constraints: const BoxConstraints(minHeight: 62),
          padding: const EdgeInsets.symmetric(horizontal: 2, vertical: 12),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      label,
                      style: const TextStyle(
                        color: AppTheme.muted,
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      centre?.name ?? placeholder,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: centre == null ? AppTheme.muted : AppTheme.ink,
                        fontSize: 15,
                        fontWeight: centre == null
                            ? FontWeight.w400
                            : FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              const Icon(
                Icons.expand_more_rounded,
                color: AppTheme.muted,
                size: 20,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _CentreSheet extends StatefulWidget {
  const _CentreSheet({
    required this.title,
    required this.centres,
    required this.selectedId,
  });

  final String title;
  final List<TransitCentre> centres;
  final String? selectedId;

  @override
  State<_CentreSheet> createState() => _CentreSheetState();
}

class _CentreSheetState extends State<_CentreSheet> {
  final _searchController = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final query = _query.trim().toLowerCase();
    final matches =
        widget.centres
            .where(
              (centre) =>
                  centre.name.toLowerCase().contains(query) ||
                  centre.city.toLowerCase().contains(query),
            )
            .toList()
          ..sort((a, b) => a.name.compareTo(b.name));

    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: FractionallySizedBox(
        heightFactor: 0.78,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(22, 24, 22, 18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.title,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: _searchController,
                    autofocus: true,
                    onChanged: (value) => setState(() => _query = value),
                    decoration: InputDecoration(
                      hintText: 'Search centre or city',
                      prefixIcon: const Icon(Icons.search_rounded, size: 20),
                      fillColor: AppTheme.background,
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(
                          color: AppTheme.borderStrong,
                        ),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(
                          color: AppTheme.brandPrimary,
                          width: 1.5,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            Expanded(
              child: matches.isEmpty
                  ? const Center(
                      child: Text(
                        'No centres found. Try another name.',
                        style: TextStyle(color: AppTheme.muted),
                      ),
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.symmetric(vertical: 8),
                      itemCount: matches.length,
                      separatorBuilder: (_, _) =>
                          const Divider(height: 1, indent: 22, endIndent: 22),
                      itemBuilder: (context, index) {
                        final centre = matches[index];
                        final selected = centre.id == widget.selectedId;
                        return ListTile(
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 22,
                            vertical: 3,
                          ),
                          title: Text(
                            centre.name,
                            style: TextStyle(
                              fontWeight: selected
                                  ? FontWeight.w700
                                  : FontWeight.w500,
                            ),
                          ),
                          subtitle: centre.city.isEmpty
                              ? null
                              : Text(centre.city),
                          trailing: selected
                              ? const Icon(
                                  Icons.check_rounded,
                                  color: AppTheme.brandPrimary,
                                )
                              : null,
                          onTap: () => Navigator.of(context).pop(centre),
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DateField extends StatelessWidget {
  const _DateField({required this.date, required this.onTap});
  final DateTime date;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: AppTheme.surface,
    borderRadius: BorderRadius.circular(13),
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(13),
      child: Container(
        constraints: const BoxConstraints(minHeight: 64),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        decoration: BoxDecoration(
          border: Border.all(color: AppTheme.borderStrong),
          borderRadius: BorderRadius.circular(13),
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Travel date',
                    style: TextStyle(
                      color: AppTheme.muted,
                      fontSize: 12,
                      fontWeight: FontWeight.w400,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    _formatDate(date),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: AppTheme.ink,
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
            const Icon(
              Icons.calendar_today_outlined,
              color: AppTheme.brandPrimary,
              size: 19,
            ),
          ],
        ),
      ),
    ),
  );
}

class _PassengerStepper extends StatelessWidget {
  const _PassengerStepper({required this.value, required this.onChanged});
  final int value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => Container(
    constraints: const BoxConstraints(minHeight: 64),
    padding: const EdgeInsets.symmetric(vertical: 6),
    decoration: BoxDecoration(
      color: AppTheme.surface,
      border: Border.all(color: AppTheme.borderStrong),
      borderRadius: BorderRadius.circular(13),
    ),
    child: Row(
      children: [
        IconButton(
          tooltip: 'Remove seat',
          onPressed: value > 1 ? () => onChanged(value - 1) : null,
          icon: const Icon(Icons.remove_rounded, size: 18),
        ),
        Expanded(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                '$value',
                style: const TextStyle(
                  color: AppTheme.ink,
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                value == 1 ? 'seat' : 'seats',
                style: const TextStyle(color: AppTheme.muted, fontSize: 11),
              ),
            ],
          ),
        ),
        IconButton(
          tooltip: 'Add seat',
          onPressed: value < 8 ? () => onChanged(value + 1) : null,
          icon: const Icon(Icons.add_rounded, size: 18),
        ),
      ],
    ),
  );
}

class _JourneyList extends StatelessWidget {
  const _JourneyList({
    required this.journeys,
    required this.hasFilters,
    required this.date,
    required this.passengerCount,
    required this.service,
    required this.authStore,
    this.onOpenTickets,
  });

  final List<JourneyOption> journeys;
  final bool hasFilters;
  final DateTime date;
  final int passengerCount;
  final TripSearchApiService service;
  final AuthStore authStore;
  final VoidCallback? onOpenTickets;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Row(
        children: [
          Expanded(
            child: _SectionTitle(
              hasFilters ? 'Matching routes' : 'Browse routes',
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
            decoration: BoxDecoration(
              color: AppTheme.surfaceMuted,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              '${journeys.length} ${journeys.length == 1 ? 'route' : 'routes'}',
              style: const TextStyle(
                color: AppTheme.muted,
                fontSize: 12,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
      const SizedBox(height: 5),
      Text(
        hasFilters
            ? 'Routes matching your selected centres'
            : 'Choose a route to see its departures',
        style: const TextStyle(color: AppTheme.muted, fontSize: 13),
      ),
      const SizedBox(height: 15),
      if (journeys.isEmpty)
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            border: Border.all(color: AppTheme.borderStrong),
            borderRadius: BorderRadius.circular(13),
          ),
          child: Text(
            hasFilters
                ? 'No routes match these centres. Try another stop or clear your selection.'
                : 'No active routes are available right now.',
            style: const TextStyle(color: AppTheme.muted, height: 1.4),
          ),
        )
      else
        ...journeys.map(
          (journey) => Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: _JourneyCard(
              journey: journey,
              date: date,
              passengerCount: passengerCount,
              service: service,
              authStore: authStore,
              onOpenTickets: onOpenTickets,
            ),
          ),
        ),
    ],
  );
}

class _JourneyCard extends StatelessWidget {
  const _JourneyCard({
    required this.journey,
    required this.date,
    required this.passengerCount,
    required this.service,
    required this.authStore,
    this.onOpenTickets,
  });

  final JourneyOption journey;
  final DateTime date;
  final int passengerCount;
  final TripSearchApiService service;
  final AuthStore authStore;
  final VoidCallback? onOpenTickets;

  void _openDepartures(BuildContext context) => Navigator.of(context).push(
    MaterialPageRoute(
      builder: (_) => SearchResultsPage(
        origin: journey.origin,
        destination: journey.destination,
        routeId: journey.routeId,
        directionId: journey.id.isEmpty ? null : journey.id,
        date: date,
        passengerCount: passengerCount,
        service: service,
        authStore: authStore,
        onOpenTickets: onOpenTickets,
      ),
    ),
  );

  @override
  Widget build(BuildContext context) => Material(
    color: AppTheme.surface,
    borderRadius: BorderRadius.circular(13),
    child: InkWell(
      onTap: () => _openDepartures(context),
      borderRadius: BorderRadius.circular(13),
      child: Container(
        padding: const EdgeInsets.fromLTRB(16, 15, 16, 13),
        decoration: BoxDecoration(
          border: Border.all(color: AppTheme.borderStrong),
          borderRadius: BorderRadius.circular(13),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 9,
                    vertical: 5,
                  ),
                  decoration: BoxDecoration(
                    color: AppTheme.surfaceMuted,
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    journey.routeNumber,
                    style: const TextStyle(
                      color: AppTheme.ink,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                const SizedBox(width: 9),
                Expanded(
                  child: Text(
                    journey.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppTheme.ink, fontSize: 13),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 17),
            Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _RouteEnd(label: 'From', name: journey.origin),
                const SizedBox(height: 10),
                _RouteEnd(label: 'To', name: journey.destination),
              ],
            ),
            const SizedBox(height: 16),
            const Divider(height: 1, color: AppTheme.border),
            const SizedBox(height: 11),
            const Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                Flexible(
                  child: Text(
                    'View departures',
                    textAlign: TextAlign.end,
                    style: TextStyle(
                      color: AppTheme.brandPrimary,
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                SizedBox(width: 5),
                Icon(
                  Icons.arrow_forward_rounded,
                  color: AppTheme.brandPrimary,
                  size: 17,
                ),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

class _RouteEnd extends StatelessWidget {
  const _RouteEnd({required this.label, required this.name});
  final String label;
  final String name;

  @override
  Widget build(BuildContext context) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      SizedBox(
        width: 46,
        child: Text(
          label,
          style: const TextStyle(
            color: AppTheme.muted,
            fontSize: 13,
            height: 1.45,
          ),
        ),
      ),
      Expanded(
        child: Text(
          name,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(
            color: AppTheme.ink,
            fontSize: 15,
            fontWeight: FontWeight.w600,
            height: 1.25,
          ),
        ),
      ),
    ],
  );
}

class _LoadError extends StatelessWidget {
  const _LoadError({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 10),
          TextButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ),
    ),
  );
}

class _EmptyCentres extends StatelessWidget {
  const _EmptyCentres();

  @override
  Widget build(BuildContext context) => const Center(
    child: Padding(
      padding: EdgeInsets.all(28),
      child: Text(
        'Journey search is unavailable until at least two centres are operating.',
        textAlign: TextAlign.center,
        style: TextStyle(color: AppTheme.muted),
      ),
    ),
  );
}

String _formatDate(DateTime date) {
  const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
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
  return '${weekdays[date.weekday - 1]}, ${date.day} ${months[date.month - 1]}';
}

bool _matches(String value, String? filter) =>
    filter == null ||
    filter.isEmpty ||
    value.toLowerCase().contains(filter.toLowerCase());
