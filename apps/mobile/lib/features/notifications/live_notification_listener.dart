import 'dart:async';

import 'package:flutter/material.dart';

import '../../models/app_notification.dart';
import '../../state/notification_store.dart';

/// Reports live notifications and taps on notifications in the phone's panel.
class LiveNotificationListener extends StatefulWidget {
  const LiveNotificationListener({
    super.key,
    required this.store,
    required this.onNotification,
    required this.onOpen,
    required this.child,
  });

  final NotificationStore store;
  final ValueChanged<AppNotification> onNotification;
  final VoidCallback onOpen;
  final Widget child;

  @override
  State<LiveNotificationListener> createState() =>
      _LiveNotificationListenerState();
}

class _LiveNotificationListenerState extends State<LiveNotificationListener> {
  late final List<StreamSubscription<Object?>> _subscriptions;

  @override
  void initState() {
    super.initState();
    _subscriptions = [
      widget.store.incoming.listen((notification) {
        if (mounted) widget.onNotification(notification);
      }),
      widget.store.openRequests.listen((_) {
        if (mounted) widget.onOpen();
      }),
    ];
  }

  @override
  void dispose() {
    for (final subscription in _subscriptions) {
      subscription.cancel();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
