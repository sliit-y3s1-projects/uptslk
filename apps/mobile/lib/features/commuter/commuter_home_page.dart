import 'package:flutter/material.dart';

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

  void _swapLocations() {
    setState(() {
      final origin = _origin;
      _origin = _destination;
      _destination = origin;
    });
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.surface,
    appBar: AppBar(
      backgroundColor: AppTheme.surface,
      centerTitle: true,
      title: const Text('Plan a journey'),
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
          padding: const EdgeInsets.fromLTRB(22, 10, 22, 36),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const _TravelBanner(),
                  const SizedBox(height: 30),
                  const _SectionLabel('Details'),
                  const SizedBox(height: 13),
                  Stack(
                    alignment: Alignment.centerRight,
                    children: [
                      Column(
                        children: [
                          _CentrePicker(
                            label: 'From',
                            icon: Icons.trip_origin_rounded,
                            value: _origin,
                            centres: centres,
                            onChanged: (value) =>
                                setState(() => _origin = value),
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
                        ],
                      ),
                      Positioned(
                        right: 16,
                        child: _SwapButton(onTap: _swapLocations),
                      ),
                    ],
                  ),
                  if (_origin != null || _destination != null)
                    Align(
                      alignment: Alignment.centerRight,
                      child: TextButton(
                        onPressed: () => setState(() {
                          _origin = null;
                          _destination = null;
                        }),
                        child: const Text('Clear'),
                      ),
                    ),
                  const SizedBox(height: 20),
                  const _SectionLabel('Trip options'),
                  const SizedBox(height: 13),
                  Row(
                    children: [
                      Expanded(
                        child: _DateField(date: _date, onTap: _chooseDate),
                      ),
                      const SizedBox(width: 12),
                      _PassengerStepper(
                        value: _passengerCount,
                        onChanged: (value) =>
                            setState(() => _passengerCount = value),
                      ),
                    ],
                  ),
                  const SizedBox(height: 32),
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
    key: ValueKey('$label-${value?.id ?? 'empty'}'),
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
  Widget build(BuildContext context) => Material(
    color: AppTheme.surface,
    borderRadius: BorderRadius.circular(17),
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(17),
      child: Container(
        height: 58,
        padding: const EdgeInsets.symmetric(horizontal: 16),
        decoration: BoxDecoration(
          border: Border.all(color: AppTheme.borderStrong),
          borderRadius: BorderRadius.circular(17),
        ),
        child: Row(
          children: [
            const Icon(
              Icons.calendar_month_outlined,
              size: 20,
              color: AppTheme.brandPrimary,
            ),
            const SizedBox(width: 11),
            Flexible(
              child: Text(
                _formatDate(date),
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
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
    height: 58,
    decoration: BoxDecoration(
      color: AppTheme.surface,
      border: Border.all(color: AppTheme.borderStrong),
      borderRadius: BorderRadius.circular(17),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          onPressed: value > 1 ? () => onChanged(value - 1) : null,
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.remove_rounded, size: 18),
        ),
        Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text('$value', style: const TextStyle(fontWeight: FontWeight.w600)),
            const Text(
              'seats',
              style: TextStyle(color: AppTheme.muted, fontSize: 10),
            ),
          ],
        ),
        IconButton(
          onPressed: value < 8 ? () => onChanged(value + 1) : null,
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.add_rounded, size: 18),
        ),
      ],
    ),
  );
}

class _TravelBanner extends StatelessWidget {
  const _TravelBanner();

  @override
  Widget build(BuildContext context) => Container(
    height: 112,
    padding: const EdgeInsets.fromLTRB(20, 18, 16, 18),
    decoration: BoxDecoration(
      color: const Color(0xFFDFF1F6),
      borderRadius: BorderRadius.circular(24),
    ),
    child: Row(
      children: [
        Expanded(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'SCHEDULED TRAVEL',
                style: TextStyle(
                  color: AppTheme.muted,
                  fontSize: 11,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 0.8,
                ),
              ),
              const SizedBox(height: 5),
              Text(
                'Reserve your seat',
                style: Theme.of(context).textTheme.titleLarge
                    ?.copyWith(fontSize: 21, letterSpacing: -0.4),
              ),
            ],
          ),
        ),
        Container(
          width: 72,
          height: 72,
          decoration: const BoxDecoration(
            color: Color(0xFFC6E8EF),
            shape: BoxShape.circle,
          ),
          child: const Icon(
            Icons.directions_bus_rounded,
            size: 35,
            color: AppTheme.brandPrimary,
          ),
        ),
      ],
    ),
  );
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.label);
  final String label;

  @override
  Widget build(BuildContext context) => Text(
    label,
    style: Theme.of(context).textTheme.titleMedium
        ?.copyWith(fontSize: 18, letterSpacing: -0.2),
  );
}

class _SwapButton extends StatelessWidget {
  const _SwapButton({required this.onTap});
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: AppTheme.surface,
    shape: const CircleBorder(side: BorderSide(color: AppTheme.borderStrong)),
    child: InkWell(
      onTap: onTap,
      customBorder: const CircleBorder(),
      child: const SizedBox(
        width: 38,
        height: 38,
        child: Icon(
          Icons.swap_vert_rounded,
          size: 21,
          color: AppTheme.brandPrimary,
        ),
      ),
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
            style: TextStyle(color: AppTheme.ink, fontWeight: FontWeight.w600),
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
    InputDecoration(
      labelText: label,
      prefixIcon: Icon(icon, size: 20),
      filled: true,
      fillColor: AppTheme.surface,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(17),
        borderSide: const BorderSide(color: AppTheme.borderStrong),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(17),
        borderSide: const BorderSide(color: AppTheme.borderStrong),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(17),
        borderSide: const BorderSide(color: AppTheme.brandPrimary, width: 1.4),
      ),
    );

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
              'Available routes',
              style: const TextStyle(
                color: AppTheme.ink,
                fontSize: 18,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
            decoration: BoxDecoration(
              color: AppTheme.surfaceMuted,
              borderRadius: BorderRadius.circular(99),
            ),
            child: Text(
              '${journeys.length}',
              style: const TextStyle(
                color: AppTheme.muted,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
      if (origin != null || destination != null) ...[
        const SizedBox(height: 4),
        Text(
          '${origin ?? 'Any origin'} to ${destination ?? 'Any destination'}',
          style: const TextStyle(color: AppTheme.muted, fontSize: 13),
        ),
      ],
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
    borderRadius: BorderRadius.circular(19),
    child: InkWell(
      onTap: () => _openDepartures(context),
      borderRadius: BorderRadius.circular(19),
      child: Container(
        padding: const EdgeInsets.all(17),
        decoration: BoxDecoration(
          border: Border.all(color: AppTheme.borderStrong),
          borderRadius: BorderRadius.circular(19),
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 9,
                          vertical: 5,
                        ),
                        decoration: BoxDecoration(
                          color: AppTheme.brandLight,
                          borderRadius: BorderRadius.circular(9),
                        ),
                        child: Text(
                          journey.routeNumber,
                          style: const TextStyle(
                            color: AppTheme.brandPrimary,
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
                          style: const TextStyle(
                            color: AppTheme.muted,
                            fontSize: 13,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  _RoutePoint(
                    label: journey.origin,
                    icon: Icons.trip_origin_rounded,
                  ),
                  const SizedBox(height: 9),
                  _RoutePoint(
                    label: journey.destination,
                    icon: Icons.location_on_rounded,
                  ),
                ],
              ),
            ),
            const SizedBox(width: 14),
            Container(
              width: 42,
              height: 42,
              decoration: const BoxDecoration(
                color: AppTheme.ink,
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.arrow_forward_rounded,
                color: Colors.white,
                size: 20,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

class _RoutePoint extends StatelessWidget {
  const _RoutePoint({required this.label, required this.icon});
  final String label;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, size: 15, color: AppTheme.brandPrimary),
      const SizedBox(width: 10),
      Expanded(
        child: Text(
          label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
        ),
      ),
    ],
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
