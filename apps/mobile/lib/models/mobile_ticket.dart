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
    this.tripStatus = '',
    this.boardingAllowed = false,
    this.passengerName = '',
    this.origin = '',
    this.destination = '',
    this.bay = '',
    this.activeUntil,
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
  final String tripStatus;
  final bool boardingAllowed;
  final String passengerName;
  final String origin;
  final String destination;
  final String bay;
  final DateTime? activeUntil;

  String get group {
    if (status == 'Cancelled' || tripStatus == 'Cancelled') return 'Cancelled';
    if (status == 'Completed' || tripStatus == 'Completed') return 'Past';
    return tripTime.isAfter(DateTime.now()) ||
            ({'Boarding', 'Delayed', 'Dispatched'}.contains(tripStatus) &&
                DateTime.now().isBefore(
                  activeUntil ?? tripTime.add(const Duration(hours: 2)),
                ))
        ? 'Upcoming'
        : 'Past';
  }

  bool get isActive => group == 'Upcoming';
  bool get canBoard =>
      boardingAllowed &&
      status == 'Confirmed' &&
      qrCode.isNotEmpty &&
      isActive &&
      tripStatus != 'Dispatched';

  String get displayStatus {
    if (group == 'Cancelled') return 'Cancelled';
    if (group == 'Past') {
      if (tripStatus == 'Dispatched' && status != 'Completed') {
        return 'Past trip';
      }
      return status == 'Completed' || tripStatus == 'Completed'
          ? 'Completed'
          : 'Expired';
    }
    if (status == 'Pending') return 'Awaiting payment';
    return tripStatus == 'Dispatched' ? 'In progress' : 'Confirmed';
  }

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
    tripStatus: json['tripStatus']?.toString() ?? '',
    activeUntil: DateTime.tryParse(json['activeUntil']?.toString() ?? ''),
    boardingAllowed: json['canBoard'] == true,
    passengerName: json['passengerName']?.toString() ?? '',
    origin: json['origin']?.toString() ?? '',
    destination: json['destination']?.toString() ?? '',
    bay: json['bay']?.toString() ?? '',
  );
}

int _intValue(dynamic value) =>
    value is num ? value.toInt() : int.tryParse(value?.toString() ?? '') ?? 0;
double _doubleValue(dynamic value) => value is num
    ? value.toDouble()
    : double.tryParse(value?.toString() ?? '') ?? 0;
