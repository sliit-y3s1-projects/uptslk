import '../models/route_direction.dart';
import '../models/departure.dart';
import '../models/driver_duty.dart';

class MockData {
  static final RouteDirection kadawathaMakumbura = const RouteDirection(
    id: 'rt-ex-km-01',
    routeNumber: 'EX-KM-01',
    routeName: 'Kadawatha - Makumbura Express',
    origin: 'Kadawatha Centre',
    destination: 'Makumbura Centre',
  );

  static final List<Departure> upcomingDepartures = [
    Departure(
      id: 'dep-0600',
      direction: kadawathaMakumbura,
      dateTime: DateTime.now().add(const Duration(days: 1)).copyWith(hour: 6, minute: 0),
      bayCode: 'KAD-B01',
      capacity: 45,
      bookedPassengers: 1,
      fare: 180.0,
    ),
    Departure(
      id: 'dep-0700',
      direction: kadawathaMakumbura,
      dateTime: DateTime.now().add(const Duration(days: 1)).copyWith(hour: 7, minute: 0),
      bayCode: 'KAD-B01',
      capacity: 45,
      bookedPassengers: 14,
      fare: 180.0,
    ),
    Departure(
      id: 'dep-0800',
      direction: kadawathaMakumbura,
      dateTime: DateTime.now().add(const Duration(days: 1)).copyWith(hour: 8, minute: 0),
      bayCode: 'KAD-B02',
      capacity: 45,
      bookedPassengers: 27,
      fare: 180.0,
    ),
  ];

  static final List<DriverDuty> driverDuties = [
    DriverDuty(
      id: 'duty-01',
      departure: upcomingDepartures[0], // 6:00 AM
      vehiclePlate: 'ND-8899',
      passengerCount: 44,
      status: 'Scheduled',
      stops: ['Kadawatha Centre', 'Makumbura Centre'],
    ),
    DriverDuty(
      id: 'duty-02',
      departure: upcomingDepartures[1], // 7:00 AM
      vehiclePlate: 'ND-8899',
      passengerCount: 31,
      status: 'Scheduled',
      stops: ['Makumbura Centre', 'Kadawatha Centre'],
    ),
  ];
}
