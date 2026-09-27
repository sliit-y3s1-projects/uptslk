class MobileTicket {
  const MobileTicket({
    required this.id,
    required this.status,
    required this.passengerCount,
    required this.fare,
    required this.qrCode,
    required this.createdAt,
    required this.tripTime,
    required this.route,
    required this.routeName,
  });

  final String id;
  final String status;
  final int passengerCount;
  final double fare;
  final String qrCode;
  final DateTime createdAt;
  final DateTime tripTime;
  final String route;
  final String routeName;

  bool get isActive => status == 'Confirmed' || status == 'Pending';

  factory MobileTicket.fromJson(Map<String, dynamic> json) => MobileTicket(
    id: json['id']?.toString() ?? '',
    status: json['status']?.toString() ?? '',
    passengerCount: _intValue(json['passengerCount']),
    fare: _doubleValue(json['fare']),
    qrCode: json['qrCode']?.toString() ?? '',
    createdAt:
        DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
        DateTime.fromMillisecondsSinceEpoch(0),
    tripTime:
        DateTime.tryParse(json['tripTime']?.toString() ?? '') ??
        DateTime.fromMillisecondsSinceEpoch(0),
    route: json['route']?.toString() ?? '',
    routeName: json['routeName']?.toString() ?? '',
  );
}

int _intValue(dynamic value) =>
    value is num ? value.toInt() : int.tryParse(value?.toString() ?? '') ?? 0;
double _doubleValue(dynamic value) => value is num
    ? value.toDouble()
    : double.tryParse(value?.toString() ?? '') ?? 0;
