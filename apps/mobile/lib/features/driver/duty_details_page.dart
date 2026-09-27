import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../models/driver_assignment.dart';
import '../../services/auth_api_service.dart';
import '../../services/driver_api_service.dart';
import '../../state/auth_store.dart';

class DutyDetailsPage extends StatefulWidget {
  const DutyDetailsPage({
    super.key,
    required this.duty,
    required this.authStore,
    required this.service,
  });

  final DriverAssignment duty;
  final AuthStore authStore;
  final DriverApiService service;

  @override
  State<DutyDetailsPage> createState() => _DutyDetailsPageState();
}

class _DutyDetailsPageState extends State<DutyDetailsPage> {
  late DriverAssignment _duty;
  bool _updating = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _duty = widget.duty;
  }

  String? get _nextStatus => switch (_duty.status) {
    'Scheduled' => 'Ready',
    'Ready' => 'Boarding',
    'Boarding' => 'Dispatched',
    'Delayed' => 'Dispatched',
    'Dispatched' => 'Completed',
    _ => null,
  };

  Future<void> _advanceStatus() async {
    final nextStatus = _nextStatus;
    final token = widget.authStore.token;
    if (nextStatus == null || token == null || token.isEmpty) return;

    setState(() {
      _updating = true;
      _error = null;
    });
    try {
      await widget.service.updateDutyStatus(
        token: token,
        tripId: _duty.id,
        status: nextStatus,
      );
      if (!mounted) return;
      setState(() => _duty = _duty.copyWith(status: nextStatus));
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text('Duty updated to ${_displayStatus(nextStatus)}.'),
          ),
        );
    } on AuthApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _updating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final nextStatus = _nextStatus;
    return Scaffold(
      backgroundColor: AppTheme.surface,
      appBar: AppBar(
        title: const Text('Duty details'),
        backgroundColor: AppTheme.surface,
        centerTitle: true,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(22, 10, 22, 120),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _StatusSummary(duty: _duty),
            const SizedBox(height: 28),
            Text(
              'Assignment',
              style: Theme.of(context).textTheme.titleMedium
                  ?.copyWith(fontSize: 18),
            ),
            const SizedBox(height: 13),
            _AssignmentDetails(duty: _duty),
            if (_duty.notes?.isNotEmpty ?? false) ...[
              const SizedBox(height: 18),
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceMuted,
                  borderRadius: BorderRadius.circular(16),
                ),
                child: Text(
                  _duty.notes!,
                  style: const TextStyle(color: AppTheme.muted, height: 1.4),
                ),
              ),
            ],
            if (_error != null) ...[
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFFFF1F2),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Text(
                  _error!,
                  style: const TextStyle(
                    color: AppTheme.danger,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        top: false,
        child: Container(
          padding: const EdgeInsets.fromLTRB(22, 12, 22, 18),
          decoration: const BoxDecoration(
            color: AppTheme.surface,
            border: Border(top: BorderSide(color: AppTheme.border)),
          ),
          child: FilledButton(
            onPressed: nextStatus == null || _updating ? null : _advanceStatus,
            child: _updating
                ? const SizedBox.square(
                    dimension: 18,
                    child: CircularProgressIndicator(
                      color: Colors.white,
                      strokeWidth: 2,
                    ),
                  )
                : Text(
                    nextStatus == null
                        ? _duty.status == 'Cancelled'
                              ? 'Duty cancelled'
                              : 'Duty completed'
                        : 'Mark as ${_displayStatus(nextStatus)}',
                  ),
          ),
        ),
      ),
    );
  }
}

class _StatusSummary extends StatelessWidget {
  const _StatusSummary({required this.duty});
  final DriverAssignment duty;

  @override
  Widget build(BuildContext context) {
    final color = switch (duty.status) {
      'Completed' => AppTheme.success,
      'Delayed' => AppTheme.warning,
      'Cancelled' => AppTheme.danger,
      _ => AppTheme.brandPrimary,
    };
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(24),
      ),
      child: Row(
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: const BoxDecoration(
              color: AppTheme.surface,
              shape: BoxShape.circle,
            ),
            child: Icon(
              duty.status == 'Completed'
                  ? Icons.check_rounded
                  : Icons.directions_bus_rounded,
              color: color,
            ),
          ),
          const SizedBox(width: 13),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'CURRENT STATUS',
                  style: TextStyle(
                    color: AppTheme.muted,
                    fontSize: 10,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 0.7,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  _displayStatus(duty.status),
                  style: TextStyle(
                    color: color,
                    fontSize: 21,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
          Text(
            _formatTime(duty.scheduledTime),
            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
          ),
        ],
      ),
    );
  }
}

class _AssignmentDetails extends StatelessWidget {
  const _AssignmentDetails({required this.duty});
  final DriverAssignment duty;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(18),
    decoration: BoxDecoration(
      color: AppTheme.surface,
      border: Border.all(color: AppTheme.borderStrong),
      borderRadius: BorderRadius.circular(20),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
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
        const SizedBox(height: 18),
        _DetailRow(
          icon: Icons.trip_origin_rounded,
          label: 'From',
          value: duty.origin,
        ),
        const Divider(height: 25),
        _DetailRow(
          icon: Icons.location_on_rounded,
          label: 'To',
          value: duty.destination,
        ),
        const Divider(height: 25),
        _DetailRow(
          icon: Icons.directions_bus_outlined,
          label: 'Vehicle',
          value: '${duty.vehiclePlate} · ${duty.vehicleModel}',
        ),
        const Divider(height: 25),
        _DetailRow(
          icon: Icons.signpost_outlined,
          label: 'Departure bay',
          value: duty.bayName.isEmpty
              ? duty.bayCode
              : '${duty.bayCode} · ${duty.bayName}',
        ),
        const Divider(height: 25),
        _DetailRow(
          icon: Icons.people_outline_rounded,
          label: 'Booked passengers',
          value: '${duty.passengerCount} of ${duty.capacity}',
        ),
      ],
    ),
  );
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
  });
  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      SizedBox(
        width: 30,
        child: Icon(icon, size: 20, color: AppTheme.brandPrimary),
      ),
      const SizedBox(width: 10),
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: const TextStyle(color: AppTheme.muted, fontSize: 12),
            ),
            const SizedBox(height: 3),
            Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
          ],
        ),
      ),
    ],
  );
}

String _formatTime(DateTime value) {
  final time = value.toLocal();
  final hour = time.hour == 0
      ? 12
      : (time.hour > 12 ? time.hour - 12 : time.hour);
  return '${hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')} ${time.hour >= 12 ? 'PM' : 'AM'}';
}

String _displayStatus(String status) =>
    status == 'Dispatched' ? 'Departed' : status;
