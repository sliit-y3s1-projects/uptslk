import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_ui.dart';
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
  });
  final String firstName;
  final AuthStore authStore;
  final VoidCallback? onOpenTickets;

  @override
  State<CommuterHomePage> createState() => _CommuterHomePageState();
}

class _CommuterHomePageState extends State<CommuterHomePage> {
  final _service = TripSearchApiService();
  late Future<List<TransitCentre>> _centresFuture;
  late Future<List<JourneyOption>> _journeysFuture;
  TransitCentre? _origin;
  TransitCentre? _destination;
  DateTime _date = DateTime.now();
  int _passengerCount = 1;

  @override
  void initState() {
    super.initState();
    _centresFuture = _service.getOperatingCentres();
    _journeysFuture = _service.getJourneyOptions();
  }

  Future<void> _chooseDate() async {
    final selected = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 90)),
    );
    if (selected != null && mounted) setState(() => _date = selected);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.background,
    appBar: AppBar(
      backgroundColor: AppTheme.background,
      foregroundColor: AppTheme.ink,
      elevation: 0,
      scrolledUnderElevation: 0,
      title: const AppWordmark(compact: true),
    ),
    body: FutureBuilder<List<TransitCentre>>(
      future: _centresFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return _LoadError(
            onRetry: () =>
                setState(() => _centresFuture = _service.getOperatingCentres()),
          );
        }
        final centres = snapshot.data ?? const <TransitCentre>[];
        if (centres.length < 2) {
          return const _EmptyCentres();
        }
        return SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 18, 20, 32),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  AppPageTitle(
                    title: 'Plan your journey',
                    subtitle: 'Good to see you, ${widget.firstName}.',
                  ),
                  const SizedBox(height: 22),
                  AppSurface(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _CentrePicker(
                          label: 'From',
                          icon: Icons.trip_origin_rounded,
                          value: _origin,
                          centres: centres,
                          onChanged: (value) => setState(() => _origin = value),
                        ),
                        const SizedBox(height: 12),
                        _CentrePicker(
                          label: 'To',
                          icon: Icons.location_on_outlined,
                          value: _destination,
                          centres: centres,
                          onChanged: (value) =>
                              setState(() => _destination = value),
                        ),
                        if (_origin != null || _destination != null)
                          Align(
                            alignment: Alignment.centerRight,
                            child: TextButton(
                              onPressed: () => setState(() {
                                _origin = null;
                                _destination = null;
                              }),
                              child: const Text('Clear journey filters'),
                            ),
                          ),
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Expanded(
                              child: _DateField(
                                date: _date,
                                onTap: _chooseDate,
                              ),
                            ),
                            const SizedBox(width: 12),
                            _PassengerStepper(
                              value: _passengerCount,
                              onChanged: (value) =>
                                  setState(() => _passengerCount = value),
                            ),
                          ],
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
                            padding: EdgeInsets.all(24),
                            child: CircularProgressIndicator(),
                          ),
                        );
                      }
                      if (journeysSnapshot.hasError) {
                        return _JourneyError(
                          onRetry: () => setState(
                            () =>
                                _journeysFuture = _service.getJourneyOptions(),
                          ),
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
                        origin: _origin?.name,
                        destination: _destination?.name,
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
  );
}

class _CentrePicker extends StatelessWidget {
  const _CentrePicker({
    required this.label,
    required this.icon,
    required this.value,
    required this.centres,
    required this.onChanged,
  });
  final String label;
  final IconData icon;
  final TransitCentre? value;
  final List<TransitCentre> centres;
  final ValueChanged<TransitCentre?> onChanged;

  @override
  Widget build(BuildContext context) => DropdownButtonFormField<TransitCentre>(
    initialValue: value,
    isExpanded: true,
    decoration: _fieldDecoration(label, icon),
    hint: Text('Select $label'.toLowerCase()),
    items: centres
        .map(
          (centre) => DropdownMenuItem(
            value: centre,
            child: Text(centre.name, overflow: TextOverflow.ellipsis),
          ),
        )
        .toList(),
    onChanged: onChanged,
  );
}

class _DateField extends StatelessWidget {
  const _DateField({required this.date, required this.onTap});
  final DateTime date;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => OutlinedButton.icon(
    onPressed: onTap,
    style: OutlinedButton.styleFrom(
      alignment: Alignment.centerLeft,
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 15),
    ),
    icon: const Icon(Icons.calendar_today_outlined, size: 18),
    label: Text(_formatDate(date), overflow: TextOverflow.ellipsis),
  );
}

class _PassengerStepper extends StatelessWidget {
  const _PassengerStepper({required this.value, required this.onChanged});
  final int value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) => Container(
    height: 52,
    decoration: BoxDecoration(
      border: Border.all(color: AppTheme.border),
      borderRadius: BorderRadius.circular(14),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          onPressed: value > 1 ? () => onChanged(value - 1) : null,
          icon: const Icon(Icons.remove, size: 18),
        ),
        Text('$value', style: const TextStyle(fontWeight: FontWeight.w700)),
        IconButton(
          onPressed: value < 8 ? () => onChanged(value + 1) : null,
          icon: const Icon(Icons.add, size: 18),
        ),
      ],
    ),
  );
}

class _LoadError extends StatelessWidget {
  const _LoadError({required this.onRetry});
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off_outlined, size: 36, color: AppTheme.muted),
          const SizedBox(height: 14),
          const Text(
            'Could not load journey locations.',
            style: TextStyle(color: AppTheme.ink, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
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
      padding: EdgeInsets.all(32),
      child: Text(
        'There are not enough operating centres to search departures.',
        textAlign: TextAlign.center,
        style: TextStyle(color: AppTheme.muted),
      ),
    ),
  );
}

InputDecoration _fieldDecoration(String label, IconData icon) =>
    InputDecoration(labelText: label, prefixIcon: Icon(icon, size: 20));

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
  return '${date.day} ${months[date.month - 1]}';
}

bool _matches(String value, String? filter) =>
    filter == null ||
    filter.isEmpty ||
    value.toLowerCase().contains(filter.toLowerCase());

class _JourneyList extends StatelessWidget {
  const _JourneyList({
    required this.journeys,
    required this.origin,
    required this.destination,
    required this.date,
    required this.passengerCount,
    required this.service,
    required this.authStore,
    this.onOpenTickets,
  });

  final List<JourneyOption> journeys;
  final String? origin;
  final String? destination;
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
            child: Text(
              origin == null && destination == null
                  ? 'Available routes'
                  : '${origin ?? 'Any origin'} to ${destination ?? 'Any destination'}',
              style: const TextStyle(
                color: AppTheme.ink,
                fontSize: 17,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          Text(
            '${journeys.length} found',
            style: const TextStyle(color: AppTheme.muted, fontSize: 13),
          ),
        ],
      ),
      const SizedBox(height: 14),
      if (journeys.isEmpty)
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 28),
          child: Text(
            'No active routes found.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppTheme.muted),
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

  @override
  Widget build(BuildContext context) => AppSurface(
    padding: const EdgeInsets.all(16),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                journey.origin,
                style: const TextStyle(
                  color: AppTheme.ink,
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 10),
              child: Icon(
                Icons.arrow_forward_rounded,
                color: AppTheme.brandPrimary,
                size: 20,
              ),
            ),
            Expanded(
              child: Text(
                journey.destination,
                textAlign: TextAlign.right,
                style: const TextStyle(
                  color: AppTheme.ink,
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        Text(
          '${journey.routeNumber} · ${journey.name}',
          style: const TextStyle(color: AppTheme.muted, fontSize: 13),
        ),
        const SizedBox(height: 16),
        OutlinedButton(
          onPressed: () => Navigator.of(context).push(
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
          ),
          child: const Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text('View departures'),
              SizedBox(width: 7),
              Icon(Icons.arrow_forward_rounded, size: 18),
            ],
          ),
        ),
      ],
    ),
  );
}

class _JourneyError extends StatelessWidget {
  const _JourneyError({required this.onRetry});
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) => Column(
    children: [
      const Text(
        'Could not load routes.',
        style: TextStyle(color: AppTheme.muted),
      ),
      TextButton(onPressed: onRetry, child: const Text('Try again')),
    ],
  );
}
