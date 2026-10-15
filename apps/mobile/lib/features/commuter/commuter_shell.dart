import 'package:flutter/material.dart';

import '../../core/widgets/app_ui.dart';
import '../../state/auth_store.dart';
import '../../state/notification_store.dart';
import '../notifications/alerts_nav_icon.dart';
import '../notifications/live_notification_listener.dart';
import '../notifications/notifications_page.dart';
import '../profile/profile_page.dart';
import 'commuter_home_page.dart';
import 'commuter_tickets_page.dart';

class CommuterShell extends StatefulWidget {
  const CommuterShell({
    super.key,
    required this.authStore,
    required this.notificationStore,
  });

  final AuthStore authStore;
  final NotificationStore notificationStore;

  @override
  State<CommuterShell> createState() => _CommuterShellState();
}

class _CommuterShellState extends State<CommuterShell> {
  int _currentIndex = 0;
  int _ticketRefreshSignal = 0;

  void _openTickets() {
    if (!mounted) return;
    setState(() {
      _currentIndex = 1;
      _ticketRefreshSignal++;
    });
  }

  @override
  Widget build(BuildContext context) {
    final user = widget.authStore.user;
    if (user == null) return const SizedBox.shrink();

    final firstName = user.name.trim().split(RegExp(r'\s+')).first;
    return LiveNotificationListener(
      store: widget.notificationStore,
      onNotification: (_) => setState(() => _ticketRefreshSignal++),
      onOpen: () => setState(() => _currentIndex = 2),
      child: Scaffold(
        body: IndexedStack(
          index: _currentIndex,
          children: [
            CommuterHomePage(
              firstName: firstName.isEmpty ? 'Commuter' : firstName,
              authStore: widget.authStore,
              onOpenTickets: _openTickets,
            ),
            CommuterTicketsPage(
              authStore: widget.authStore,
              refreshSignal: _ticketRefreshSignal,
            ),
            NotificationsPage(store: widget.notificationStore),
            ProfilePage(
              authStore: widget.authStore,
              user: user,
              showJourneyAction: false,
            ),
          ],
        ),
        bottomNavigationBar: AppBottomNavigation(
          selectedIndex: _currentIndex,
          onSelected: (index) {
            setState(() {
              _currentIndex = index;
              if (index == 1) _ticketRefreshSignal++;
            });
          },
          destinations: [
            NavigationDestination(
              icon: const Icon(Icons.search_outlined),
              selectedIcon: const Icon(Icons.search),
              label: 'Home',
            ),
            NavigationDestination(
              icon: const Icon(Icons.confirmation_number_outlined),
              selectedIcon: const Icon(Icons.confirmation_number),
              label: 'Tickets',
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
