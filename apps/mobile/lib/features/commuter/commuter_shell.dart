import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import 'commuter_home_page.dart';
import 'commuter_tickets_page.dart';
import 'commuter_profile_page.dart';

class CommuterShell extends StatefulWidget {
  const CommuterShell({super.key});

  @override
  State<CommuterShell> createState() => _CommuterShellState();
}

class _CommuterShellState extends State<CommuterShell> {
  int _currentIndex = 0;

  final List<Widget> _pages = [
    const CommuterHomePage(),
    const CommuterTicketsPage(),
    const CommuterProfilePage(),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: _pages[_currentIndex],
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (index) {
          setState(() {
            _currentIndex = index;
          });
        },
        backgroundColor: AppTheme.surface,
        indicatorColor: AppTheme.brandLight,
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.search_outlined),
            selectedIcon: Icon(Icons.search, color: AppTheme.brandPrimary),
            label: 'Home',
          ),
          NavigationDestination(
            icon: Icon(Icons.confirmation_number_outlined),
            selectedIcon: Icon(Icons.confirmation_number, color: AppTheme.brandPrimary),
            label: 'Tickets',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person, color: AppTheme.brandPrimary),
            label: 'Profile',
          ),
        ],
      ),
    );
  }
}
