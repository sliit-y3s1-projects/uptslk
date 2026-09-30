import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/models/mobile_ticket.dart';
import 'package:mobile/models/trip_search_result.dart';
import 'package:mobile/models/driver_assignment.dart';
import 'package:mobile/core/time/service_time.dart';

void main() {
  final now = DateTime.now().toUtc();
  test('stale ongoing tickets move to Past without changing trip status', () {
    for (final status in ['Dispatched', 'Boarding', 'Delayed']) {
      MobileTicket ongoing(DateTime cutoff) => MobileTicket.fromJson({
        'status': 'Confirmed',
        'tripStatus': status,
        'tripTime': now.subtract(const Duration(days: 6)).toIso8601String(),
        'activeUntil': cutoff.toIso8601String(),
        'canBoard': true,
        'qrCode': 'BKG-TEST',
      });
      final stale = ongoing(now.subtract(const Duration(minutes: 1)));
      expect(stale.group, 'Past');
      expect(stale.canBoard, isFalse);
      expect(stale.tripStatus, status);
      expect(ongoing(now.add(const Duration(minutes: 30))).group, 'Upcoming');
    }
  });
  test('departure display uses Sri Lanka time across midnight', () {
    final value = sriLankaTime(DateTime.parse('2026-09-30T20:00:00Z'));
    expect(value.day, 1);
    expect(value.month, 10);
    expect(value.hour, 1);
    expect(value.minute, 30);
  });
  test('driver next duty skips stale and cancelled trips and prioritizes current work', () {
    DriverAssignment duty(String id, String status, int hours) =>
        DriverAssignment.fromJson({
          'id': id,
          'status': status,
          'scheduledTime': now.add(Duration(hours: hours)).toIso8601String(),
        });
    final duties = [
      duty('old', 'Scheduled', -8),
      duty('cancelled', 'Cancelled', 1),
      duty('later', 'Scheduled', 3),
      duty('next', 'Scheduled', 2),
    ];
    expect(nextDriverDuty(duties, now)?.id, 'next');
    duties.add(duty('current', 'Dispatched', -1));
    expect(nextDriverDuty(duties, now)?.id, 'current');
    expect(nextDriverDuty([duty('old', 'Scheduled', -8)], now), isNull);
  });
  TripSearchResult trip(String status, DateTime departure) =>
      TripSearchResult.fromJson({
        'status': status,
        'scheduledTime': departure.toIso8601String(),
        'available': 10,
      });
  MobileTicket ticket(String status, String tripStatus, DateTime departure) =>
      MobileTicket.fromJson({
        'status': status,
        'tripStatus': tripStatus,
        'tripTime': departure.toIso8601String(),
        'canBoard': true,
        'qrCode': 'BKG-SAME-SERVER-TOKEN',
      });

  test('past and terminal departures cannot be booked', () {
    expect(
      trip('Scheduled', now.subtract(const Duration(hours: 8))).isBookable,
      isFalse,
    );
    expect(trip('Scheduled', now).isBookable, isFalse);
    for (final status in ['Dispatched', 'Completed', 'Cancelled', 'Delayed']) {
      expect(
        trip(status, now.add(const Duration(hours: 1))).isBookable,
        isFalse,
      );
    }
    expect(
      trip('Scheduled', now.add(const Duration(hours: 1))).isBookable,
      isTrue,
    );
  });

  test('cancelled, unpaid and expired bookings never show a boarding code', () {
    final future = now.add(const Duration(hours: 1));
    expect(ticket('Cancelled', 'Scheduled', future).group, 'Cancelled');
    expect(ticket('Cancelled', 'Scheduled', future).canBoard, isFalse);
    expect(ticket('Confirmed', 'Cancelled', future).canBoard, isFalse);
    expect(ticket('Pending', 'Scheduled', future).canBoard, isFalse);
    final expired = ticket(
      'Confirmed',
      'Scheduled',
      now.subtract(const Duration(hours: 1)),
    );
    expect(expired.group, 'Past');
    expect(expired.canBoard, isFalse);
    expect(ticket('Confirmed', 'Scheduled', future).canBoard, isTrue);
    expect(
      ticket('Confirmed', 'Scheduled', future).qrCode,
      'BKG-SAME-SERVER-TOKEN',
    );
  });

  test(
    'existing paid ticket remains usable during explicit delayed boarding',
    () {
      final departure = now.subtract(const Duration(minutes: 10));
      expect(ticket('Confirmed', 'Boarding', departure).canBoard, isTrue);
      expect(ticket('Confirmed', 'Delayed', departure).canBoard, isTrue);
      expect(ticket('Confirmed', 'Dispatched', departure).canBoard, isFalse);
    },
  );
}
