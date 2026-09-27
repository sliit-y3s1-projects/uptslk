class TripSearchResult {
  const TripSearchResult({
    required this.id,
    required this.routeId,
    required this.directionId,
    required this.routeNumber,
    required this.routeName,
    required this.origin,
    required this.destination,
    required this.scheduledTime,
    required this.status,
    required this.bay,
    required this.available,
    required this.capacity,
  });

  final String id;
  final String routeId;
  final String? directionId;
  final String routeNumber;
  final String routeName;
  final String origin;
  final String destination;
  final DateTime scheduledTime;
  final String status;
  final String bay;
  final int available;
  final int capacity;

  factory TripSearchResult.fromJson(Map<String, dynamic> json) =>
      TripSearchResult(
        id: json['id']?.toString() ?? '',
        routeId: json['routeId']?.toString() ?? '',
        directionId: json['routeDirectionId']?.toString(),
        routeNumber: json['routeNumber']?.toString() ?? '',
        routeName: json['routeName']?.toString() ?? '',
        origin: json['origin']?.toString() ?? '',
        destination: json['destination']?.toString() ?? '',
        scheduledTime:
            DateTime.tryParse(json['scheduledTime']?.toString() ?? '') ??
            DateTime.fromMillisecondsSinceEpoch(0),
        status: json['status']?.toString() ?? '',
        bay: json['bay']?.toString() ?? '',
        available: _asInt(json['available']),
        capacity: _asInt(json['capacity']),
      );

  bool get isBookable =>
      available > 0 &&
      {'Scheduled', 'Ready', 'Boarding', 'Delayed'}.contains(status);
}

int _asInt(dynamic value) => switch (value) {
  int number => number,
  num number => number.toInt(),
  String text => int.tryParse(text) ?? 0,
  _ => 0,
};
