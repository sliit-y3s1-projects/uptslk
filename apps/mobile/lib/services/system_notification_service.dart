import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../models/app_notification.dart';

/// Shows notifications in the phone's notification panel, like an SMS.
class SystemNotificationService {
  SystemNotificationService({required this.onTap});

  final void Function() onTap;
  final _plugin = FlutterLocalNotificationsPlugin();
  var _permissionRequested = false;
  var _nextId = 0;

  Future<void> initialize() => _plugin.initialize(
    settings: const InitializationSettings(
      android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      iOS: DarwinInitializationSettings(
        requestAlertPermission: false,
        requestBadgePermission: false,
        requestSoundPermission: false,
      ),
    ),
    onDidReceiveNotificationResponse: (_) => onTap(),
  );

  Future<void> requestPermission() async {
    if (_permissionRequested) return;
    _permissionRequested = true;
    await _plugin
        .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin
        >()
        ?.requestNotificationsPermission();
    await _plugin
        .resolvePlatformSpecificImplementation<
          IOSFlutterLocalNotificationsPlugin
        >()
        ?.requestPermissions(alert: true, badge: true, sound: true);
  }

  Future<void> show(AppNotification notification) => _plugin.show(
    id: _nextId++,
    title: notification.title,
    body: notification.message,
    notificationDetails: NotificationDetails(
      android: AndroidNotificationDetails(
        'uptslk_trip_updates',
        'Trip updates',
        channelDescription: 'Changes to your trips, bookings and duties',
        importance: Importance.high,
        priority: Priority.high,
        styleInformation: BigTextStyleInformation(notification.message),
      ),
      iOS: const DarwinNotificationDetails(
        presentAlert: true,
        presentSound: true,
      ),
    ),
  );
}
