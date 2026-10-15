import 'dart:async';

import 'package:flutter/material.dart';

import '../../models/app_notification.dart';
import '../../state/notification_store.dart';

/// Shows a snack bar for each live notification and reports it to [onNotification].
class LiveNotificationListener extends StatefulWidget {
  const LiveNotificationListener({
    super.key,
    required this.store,
    required this.onNotification,
    required this.onView,
    required this.child,
  });

  final NotificationStore store;
  final ValueChanged<AppNotification> onNotification;
  final VoidCallback onView;
  final Widget child;

  @override
  State<LiveNotificationListener> createState() =>
      _LiveNotificationListenerState();
}

class _LiveNotificationListenerState extends State<LiveNotificationListener> {
  late StreamSubscription<AppNotification> _subscription;

  @override
  void initState() {
    super.initState();
    _subscription = widget.store.incoming.listen(_show);
  }

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }

  void _show(AppNotification notification) {
    if (!mounted) return;
    widget.onNotification(notification);
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          behavior: SnackBarBehavior.floating,
          content: Text('${notification.title}\n${notification.message}'),
          action: SnackBarAction(label: 'View', onPressed: widget.onView),
        ),
      );
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
