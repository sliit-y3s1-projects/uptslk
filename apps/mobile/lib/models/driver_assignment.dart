class DriverOperationalProfile {
  const DriverOperationalProfile({
    required this.id,
    required this.fullName,
    required this.email,
    required this.licenseNumber,
    required this.status,
    required this.centreId,
    required this.centreCode,
    required this.centreName,
    this.phoneNumber,
  });

  final String id;
  final String fullName;
  final String email;
  final String licenseNumber;
  final String status;
  final String centreId;
  final String centreCode;
  final String centreName;
  final String? phoneNumber;

  factory DriverOperationalProfile.fromJson(Map<String, dynamic> json) =>
      DriverOperationalProfile(
        id: json['id']?.toString() ?? '',
        fullName: json['fullName']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        licenseNumber: json['licenseNumber']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
        centreId: json['centreId']?.toString() ?? '',
        centreCode: json['centreCode']?.toString() ?? '',
        centreName: json['centreName']?.toString() ?? '',
        phoneNumber: json['phoneNumber']?.toString(),
      );
}

class DriverAssignment {
  const DriverAssignment({
    required this.id,
    required this.routeId,
    required this.routeNumber,
    required this.routeName,
    required this.origin,
    required this.destination,
    required this.scheduledTime,
    required this.status,
    required this.vehiclePlate,
    required this.vehicleModel,
    required this.capacity,
    required this.bayCode,
    required this.bayName,
    required this.passengerCount,
    this.directionId,
    this.notes,
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
  final String vehiclePlate;
  final String vehicleModel;
  final int capacity;
  final String bayCode;
  final String bayName;
  final int passengerCount;
  final String? notes;

  bool get isFinished => status == 'Completed' || status == 'Cancelled';

  DriverAssignment copyWith({String? status}) => DriverAssignment(
    id: id,
    routeId: routeId,
    directionId: directionId,
    routeNumber: routeNumber,
    routeName: routeName,
    origin: origin,
    destination: destination,
    scheduledTime: scheduledTime,
    status: status ?? this.status,
    vehiclePlate: vehiclePlate,
    vehicleModel: vehicleModel,
    capacity: capacity,
    bayCode: bayCode,
    bayName: bayName,
    passengerCount: passengerCount,
    notes: notes,
  );

  factory DriverAssignment.fromJson(Map<String, dynamic> json) =>
      DriverAssignment(
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
        vehiclePlate: json['vehiclePlate']?.toString() ?? '',
        vehicleModel: json['vehicleModel']?.toString() ?? '',
        capacity: _asInt(json['capacity']),
        bayCode: json['bayCode']?.toString() ?? '',
        bayName: json['bayName']?.toString() ?? '',
        passengerCount: _asInt(json['passengerCount']),
        notes: json['notes']?.toString(),
      );
}

int _asInt(dynamic value) => switch (value) {
  int number => number,
  num number => number.toInt(),
  String text => int.tryParse(text) ?? 0,
  _ => 0,
};
