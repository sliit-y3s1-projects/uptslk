class AppNotification {
  const AppNotification({
    required this.id,
    required this.type,
    required this.title,
    required this.message,
    required this.createdAt,
    this.tripId,
    this.bookingId,
    this.readAt,
  });

  final String id;
  final String type;
  final String title;
  final String message;
  final DateTime createdAt;
  final String? tripId;
  final String? bookingId;
  final DateTime? readAt;

  bool get isRead => readAt != null;

  factory AppNotification.fromJson(Map<String, dynamic> json) =>
      AppNotification(
        id: json['id']?.toString() ?? '',
        type: json['type']?.toString() ?? '',
        title: json['title']?.toString() ?? '',
        message: json['message']?.toString() ?? '',
        createdAt:
            DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
            DateTime.now(),
        tripId: json['tripId']?.toString(),
        bookingId: json['bookingId']?.toString(),
        readAt: DateTime.tryParse(json['readAt']?.toString() ?? ''),
      );

  AppNotification markedRead(DateTime at) => AppNotification(
    id: id,
    type: type,
    title: title,
    message: message,
    createdAt: createdAt,
    tripId: tripId,
    bookingId: bookingId,
    readAt: readAt ?? at,
  );
}
