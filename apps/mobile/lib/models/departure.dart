import 'route_direction.dart';

class Departure {
  final String id;
  final RouteDirection direction;
  final DateTime dateTime;
  final String bayCode;
  final int capacity;
  final int bookedPassengers;
  final double fare;

  const Departure({
    required this.id,
    required this.direction,
    required this.dateTime,
    required this.bayCode,
    required this.capacity,
    required this.bookedPassengers,
    required this.fare,
  });
  
  int get availableSpaces => capacity - bookedPassengers;
}
