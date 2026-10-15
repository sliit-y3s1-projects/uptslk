import 'package:flutter/material.dart';

import '../../core/widgets/app_ui.dart';
import '../../state/auth_store.dart';
import '../../state/notification_store.dart';
import '../notifications/alerts_nav_icon.dart';
import '../notifications/live_notification_listener.dart';
import '../notifications/notifications_page.dart';
import '../profile/profile_page.dart';
import 'driver_home_page.dart';

class DriverShell extends StatefulWidget {
  const DriverShell({
    super.key,
    required this.authStore,
    required this.notificationStore,
  });

  final AuthStore authStore;
  final NotificationStore notificationStore;

  @override
  State<DriverShell> createState() => _DriverShellState();
}

class _DriverShellState extends State<DriverShell> {
  int _currentIndex = 0;
  int _dutyRefreshSignal = 0;

  @override
  Widget build(BuildContext context) {
    final user = widget.authStore.user;
    if (user == null) return const SizedBox.shrink();
    final pages = [
      DriverHomePage(
        authStore: widget.authStore,
        refreshSignal: _dutyRefreshSignal,
      ),
      NotificationsPage(store: widget.notificationStore),
      ProfilePage(
        authStore: widget.authStore,
        user: user,
        showJourneyAction: false,
      ),
    ];

    return LiveNotificationListener(
      store: widget.notificationStore,
      onNotification: (_) => setState(() => _dutyRefreshSignal++),
      onView: () => setState(() => _currentIndex = 1),
      child: Scaffold(
        body: IndexedStack(index: _currentIndex, children: pages),
        bottomNavigationBar: AppBottomNavigation(
          selectedIndex: _currentIndex,
          onSelected: (index) {
            setState(() {
              _currentIndex = index;
            });
          },
          destinations: [
            NavigationDestination(
              icon: const Icon(Icons.route_outlined),
              selectedIcon: const Icon(Icons.route),
              label: 'Duties',
            ),
            NavigationDestination(
              icon: AlertsNavIcon(
                store: widget.notificationStore,
                icon: Icons.notifications_outlined,
              ),
              selectedIcon: AlertsNavIcon(
                store: widget.notificationStore,
                icon: Icons.notifications,
              ),
              label: 'Alerts',
            ),
            NavigationDestination(
              icon: const Icon(Icons.person_outline),
              selectedIcon: const Icon(Icons.person),
              label: 'Profile',
            ),
          ],
        ),
      ),
    );
  }
}
