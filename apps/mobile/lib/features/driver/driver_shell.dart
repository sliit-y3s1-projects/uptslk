import 'package:flutter/material.dart';

import '../../core/widgets/app_ui.dart';
import '../../state/auth_store.dart';
import '../profile/profile_page.dart';
import 'driver_home_page.dart';

class DriverShell extends StatefulWidget {
  const DriverShell({super.key, required this.authStore});

  final AuthStore authStore;

  @override
  State<DriverShell> createState() => _DriverShellState();
}

class _DriverShellState extends State<DriverShell> {
  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    final user = widget.authStore.user;
    if (user == null) return const SizedBox.shrink();
    final pages = [
      DriverHomePage(authStore: widget.authStore),
      ProfilePage(
        authStore: widget.authStore,
        user: user,
        showJourneyAction: false,
      ),
    ];

    return Scaffold(
      body: IndexedStack(index: _currentIndex, children: pages),
      bottomNavigationBar: AppBottomNavigation(
        selectedIndex: _currentIndex,
        onSelected: (index) {
          setState(() {
            _currentIndex = index;
          });
        },
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.route_outlined),
            selectedIcon: Icon(Icons.route),
            label: 'Duties',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person),
            label: 'Profile',
          ),
        ],
      ),
    );
  }
}
