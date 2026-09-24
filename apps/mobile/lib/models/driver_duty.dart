import 'departure.dart';

class DriverDuty {
  final String id;
  final Departure departure;
  final String vehiclePlate;
  final int passengerCount;
  final String status; // 'Scheduled', 'Ready', 'Boarding', 'Departed', 'Completed'
  final List<String> stops;

  const DriverDuty({
    required this.id,
    required this.departure,
    required this.vehiclePlate,
    required this.passengerCount,
    required this.status,
    required this.stops,
  });
}
