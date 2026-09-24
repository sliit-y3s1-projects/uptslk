import 'departure.dart';

class Booking {
  final String id;
  final Departure departure;
  final int passengerCount;
  final String status; // 'Upcoming', 'Completed', 'Cancelled'

  const Booking({
    required this.id,
    required this.departure,
    required this.passengerCount,
    required this.status,
  });
}
