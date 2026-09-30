import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../models/driver_assignment.dart';
import '../../services/auth_api_service.dart';
import '../../services/driver_api_service.dart';
import '../../state/auth_store.dart';
import 'duty_details_page.dart';

class DriverHomePage extends StatefulWidget {
  const DriverHomePage({super.key, required this.authStore});

  final AuthStore authStore;

  @override
  State<DriverHomePage> createState() => _DriverHomePageState();
}

class _DriverHomePageState extends State<DriverHomePage> {
  final _service = DriverApiService();
  late Future<_DriverDashboard> _dashboardFuture;

  @override
  void initState() {
    super.initState();
    _dashboardFuture = _load();
  }

  Future<_DriverDashboard> _load() async {
    final token = widget.authStore.token;
    if (token == null || token.isEmpty) {
      throw const AuthApiException('Your session has ended. Sign in again.');
    }
    final today = _serviceToday();
    final results = await Future.wait([
      _service.getProfile(token),
      _service.getDuties(
        token: token,
        fromDate: today,
        toDate: today.add(const Duration(days: 6)),
      ),
    ]);
    return _DriverDashboard(
      profile: results[0] as DriverOperationalProfile,
      duties: results[1] as List<DriverAssignment>,
      today: today,
    );
  }

  Future<void> _refresh() async {
    final future = _load();
    setState(() => _dashboardFuture = future);
    await future;
  }

  Future<void> _openDuty(DriverAssignment duty) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => DutyDetailsPage(
          duty: duty,
          authStore: widget.authStore,
          service: _service,
        ),
      ),
    );
    if (mounted) await _refresh();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.surface,
    appBar: AppBar(
      title: const Text('Driver duties'),
      backgroundColor: AppTheme.surface,
      centerTitle: true,
    ),
    body: FutureBuilder<_DriverDashboard>(
      future: _dashboardFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError || snapshot.data == null) {
          return _DriverError(
            message: snapshot.error is AuthApiException
                ? (snapshot.error! as AuthApiException).message
                : 'Could not load your assigned duties.',
            onRetry: () => setState(() => _dashboardFuture = _load()),
          );
        }

        final dashboard = snapshot.data!;
        final todayDuties = dashboard.duties
            .where((duty) => duty.serviceDate == dashboard.today)
            .toList();
        final upcomingByDay = <DateTime, List<DriverAssignment>>{};
        for (final duty in dashboard.duties) {
          if (duty.serviceDate.isAfter(dashboard.today)) {
            upcomingByDay.putIfAbsent(duty.serviceDate, () => []).add(duty);
          }
        }
        return RefreshIndicator(
          onRefresh: _refresh,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(22, 10, 22, 36),
            children: [
              _DriverSummary(profile: dashboard.profile),
              const SizedBox(height: 28),
              _DutyHeading(title: 'Today', count: todayDuties.length),
              const SizedBox(height: 13),
              if (todayDuties.isEmpty)
                const _EmptyDuties(message: 'No duties assigned today.')
              else
                ...todayDuties.map(
                  (duty) => Padding(
                    padding: const EdgeInsets.only(bottom: 14),
                    child: _DutyCard(duty: duty, onTap: () => _openDuty(duty)),
                  ),
                ),
              const SizedBox(height: 16),
              _DutyHeading(
                title: 'Upcoming',
                count: dashboard.duties.length - todayDuties.length,
              ),
              const SizedBox(height: 13),
              if (upcomingByDay.isEmpty)
                const _EmptyDuties(
                  message: 'No upcoming duties in the next six days.',
                )
              else
                for (final entry in upcomingByDay.entries) ...[
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: Text(
                      _formatDay(entry.key),
                      style: const TextStyle(
                        color: AppTheme.brandPrimary,
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  for (final duty in entry.value)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 14),
                      child: _DutyCard(
                        duty: duty,
                        onTap: () => _openDuty(duty),
                      ),
                    ),
                ],
            ],
          ),
        );
      },
    ),
  );
}

class _DriverDashboard {
  const _DriverDashboard({
    required this.profile,
    required this.duties,
    required this.today,
  });
  final DriverOperationalProfile profile;
  final List<DriverAssignment> duties;
  final DateTime today;
}

class _DriverSummary extends StatelessWidget {
  const _DriverSummary({required this.profile});
  final DriverOperationalProfile profile;

  @override
  Widget build(BuildContext context) => Container(
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
              width: 48,
              height: 48,
              decoration: const BoxDecoration(
                color: AppTheme.surface,
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.badge_outlined,
                color: AppTheme.brandPrimary,
              ),
            ),
            const SizedBox(width: 13),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'ON DUTY WITH UPTSLK',
                    style: TextStyle(
                      color: AppTheme.brandPrimary,
                      fontSize: 10,
                      fontWeight: FontWeight.w600,
                      letterSpacing: 0.7,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    profile.fullName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 18),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _SummaryChip(
              icon: Icons.apartment_outlined,
              label: profile.centreName,
            ),
            _SummaryChip(
              icon: Icons.credit_card_outlined,
              label: profile.licenseNumber,
            ),
          ],
        ),
      ],
    ),
  );
}

class _SummaryChip extends StatelessWidget {
  const _SummaryChip({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 7),
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

class _DutyHeading extends StatelessWidget {
  const _DutyHeading({required this.title, required this.count});
  final String title;
  final int count;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Expanded(
        child: Text(
          title,
          style: Theme.of(context).textTheme.titleMedium
              ?.copyWith(fontSize: 18),
        ),
      ),
      Text(
        '$count ${count == 1 ? 'trip' : 'trips'}',
        style: const TextStyle(
          color: AppTheme.muted,
          fontSize: 13,
          fontWeight: FontWeight.w500,
        ),
      ),
    ],
  );
}

class _DutyCard extends StatelessWidget {
  const _DutyCard({required this.duty, required this.onTap});
  final DriverAssignment duty;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: AppTheme.surface,
    borderRadius: BorderRadius.circular(20),
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          border: Border.all(color: AppTheme.borderStrong),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    _formatTime(duty.scheduledTime),
                    style: const TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.w700,
                      letterSpacing: -0.4,
                    ),
                  ),
                ),
                _StatusPill(status: duty.status),
              ],
            ),
            const SizedBox(height: 16),
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
                    duty.routeNumber,
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
                    duty.routeName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppTheme.muted, fontSize: 13),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 15),
            _RoutePoint(icon: Icons.trip_origin_rounded, label: duty.origin),
            const SizedBox(height: 9),
            _RoutePoint(
              icon: Icons.location_on_rounded,
              label: duty.destination,
            ),
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 16),
              child: Divider(height: 1),
            ),
            Row(
              children: [
                Expanded(
                  child: _DutyMeta(
                    icon: Icons.directions_bus_outlined,
                    label: duty.vehiclePlate,
                  ),
                ),
                Expanded(
                  child: _DutyMeta(
                    icon: Icons.signpost_outlined,
                    label: 'Bay ${duty.bayCode}',
                  ),
                ),
                Container(
                  width: 34,
                  height: 34,
                  decoration: const BoxDecoration(
                    color: AppTheme.surfaceMuted,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.arrow_forward_rounded,
                    size: 17,
                    color: AppTheme.brandPrimary,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

class _RoutePoint extends StatelessWidget {
  const _RoutePoint({required this.icon, required this.label});
  final IconData icon;
  final String label;

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

class _DutyMeta extends StatelessWidget {
  const _DutyMeta({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, size: 16, color: AppTheme.muted),
      const SizedBox(width: 6),
      Expanded(
        child: Text(
          label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(color: AppTheme.muted, fontSize: 12),
        ),
      ),
    ],
  );
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    final color = switch (status) {
      'Completed' => AppTheme.success,
      'Delayed' => AppTheme.warning,
      'Cancelled' => AppTheme.danger,
      'Ready' || 'Boarding' || 'Dispatched' => AppTheme.brandPrimary,
      _ => AppTheme.muted,
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(99),
      ),
      child: Text(
        _displayStatus(status),
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _EmptyDuties extends StatelessWidget {
  const _EmptyDuties({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 24),
    child: Column(
      children: [
        const Icon(
          Icons.event_available_outlined,
          size: 32,
          color: AppTheme.muted,
        ),
        const SizedBox(height: 10),
        Text(
          message,
          textAlign: TextAlign.center,
          style: const TextStyle(color: AppTheme.muted),
        ),
      ],
    ),
  );
}

class _DriverError extends StatelessWidget {
  const _DriverError({required this.message, required this.onRetry});
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

String _formatTime(DateTime value) {
  final time = value.toUtc().add(const Duration(hours: 5, minutes: 30));
  final hour = time.hour == 0
      ? 12
      : (time.hour > 12 ? time.hour - 12 : time.hour);
  return '${hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')} ${time.hour >= 12 ? 'PM' : 'AM'}';
}

DateTime _serviceToday() {
  final now = DateTime.now().toUtc().add(const Duration(hours: 5, minutes: 30));
  return DateTime(now.year, now.month, now.day);
}

String _formatDay(DateTime date) {
  const weekdays = [
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ];
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

String _displayStatus(String status) =>
    status == 'Dispatched' ? 'Departed' : status;
