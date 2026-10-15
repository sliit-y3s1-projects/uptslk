import 'package:flutter/material.dart';

import '../../state/notification_store.dart';

class AlertsNavIcon extends StatelessWidget {
  const AlertsNavIcon({super.key, required this.store, required this.icon});

  final NotificationStore store;
  final IconData icon;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: store,
    builder: (context, _) => Badge(
      isLabelVisible: store.unreadCount > 0,
      label: Text('${store.unreadCount}'),
      child: Icon(icon),
    ),
  );
}
