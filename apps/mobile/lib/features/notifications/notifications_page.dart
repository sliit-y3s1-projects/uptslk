import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../core/time/service_time.dart';
import '../../models/app_notification.dart';
import '../../state/notification_store.dart';

class NotificationsPage extends StatelessWidget {
  const NotificationsPage({super.key, required this.store});

  final NotificationStore store;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: store,
    builder: (context, _) => Scaffold(
      backgroundColor: AppTheme.surface,
      appBar: AppBar(
        title: const Text('Notifications'),
        centerTitle: true,
        actions: [
          if (store.unreadCount > 0)
            TextButton(
              onPressed: store.markAllRead,
              child: const Text('Mark all read'),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: store.refresh,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(22, 10, 22, 36),
          children: [
            if (store.items.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 70),
                child: Text(
                  'No notifications yet.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: AppTheme.muted),
                ),
              ),
            for (final notification in store.items)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: _NotificationCard(
                  notification: notification,
                  onTap: () => store.markRead(notification),
                ),
              ),
          ],
        ),
      ),
    ),
  );
}

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = notificationColor(notification.type);
    return Material(
      color: notification.isRead ? AppTheme.surface : AppTheme.brandLight,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: AppTheme.border),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: color.withValues(alpha: 0.12),
                child: Icon(
                  notificationIcon(notification.type),
                  color: color,
                  size: 20,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            notification.title,
                            style: TextStyle(
                              fontWeight: notification.isRead
                                  ? FontWeight.w500
                                  : FontWeight.w700,
                            ),
                          ),
                        ),
                        Text(
                          _formatTime(notification.createdAt),
                          style: const TextStyle(
                            color: AppTheme.subtle,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      notification.message,
                      style: const TextStyle(
                        color: AppTheme.muted,
                        height: 1.35,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static String _formatTime(DateTime instant) {
    const months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec',
    ];
    final local = sriLankaTime(instant);
    final time =
        '${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
    final today = serviceToday();
    final isToday =
        local.year == today.year &&
        local.month == today.month &&
        local.day == today.day;
    return isToday ? time : '${local.day} ${months[local.month - 1]}, $time';
  }
}

IconData notificationIcon(String type) => switch (type) {
  'TripAssigned' => Icons.assignment_turned_in_outlined,
  'TripUnassigned' => Icons.assignment_return_outlined,
  'TripUpdated' => Icons.swap_horiz,
  'TripStatusChanged' => Icons.directions_bus_outlined,
  'TripCancelled' || 'BookingCancelled' => Icons.cancel_outlined,
  'BookingConfirmed' => Icons.check_circle_outline,
  'RefundCompleted' => Icons.account_balance_wallet_outlined,
  'RefundFailed' => Icons.error_outline,
  'IncidentUpdated' => Icons.report_outlined,
  _ => Icons.notifications_outlined,
};

Color notificationColor(String type) => switch (type) {
  'TripCancelled' || 'BookingCancelled' || 'RefundFailed' => AppTheme.danger,
  'BookingConfirmed' || 'RefundCompleted' => AppTheme.success,
  'TripStatusChanged' || 'TripUpdated' => AppTheme.warning,
  _ => AppTheme.brandPrimary,
};
