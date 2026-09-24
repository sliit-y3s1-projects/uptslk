import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../main.dart';
import 'search_results_page.dart';

class CommuterHomePage extends StatefulWidget {
  const CommuterHomePage({super.key});

  @override
  State<CommuterHomePage> createState() => _CommuterHomePageState();
}

class _CommuterHomePageState extends State<CommuterHomePage> {
  int _passengerCount = 1;
  String _from = 'Kadawatha Centre';
  String _to = 'Makumbura Centre';

  void _swapLocations() {
    setState(() {
      final temp = _from;
      _from = _to;
      _to = temp;
    });
  }

  @override
  Widget build(BuildContext context) {
    final userName = demoStore.currentUser?.name.split(' ').first ?? 'Traveler';

    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: AppTheme.brandPrimary,
        elevation: 0,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Good morning,',
              style: TextStyle(fontSize: 14, color: AppTheme.brandLight),
            ),
            Text(
              userName,
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.white),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_outlined, color: Colors.white),
            onPressed: () {},
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Find your journey card
            Container(
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppTheme.border),
                boxShadow: [
                  BoxShadow(
                    color: AppTheme.ink.withOpacity(0.04),
                    blurRadius: 12,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  const Align(
                    alignment: Alignment.centerLeft,
                    child: Text(
                      'Find your journey',
                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTheme.brandPrimary),
                    ),
                  ),
                  const SizedBox(height: 20),
                  
                  // From/To with swap
                  Stack(
                    alignment: Alignment.centerRight,
                    children: [
                      Column(
                        children: [
                          _buildLocationField('From', _from, Icons.my_location),
                          const SizedBox(height: 12),
                          _buildLocationField('To', _to, Icons.location_on),
                        ],
                      ),
                      Positioned(
                        right: 16,
                        child: Container(
                          decoration: BoxDecoration(
                            color: AppTheme.surface,
                            shape: BoxShape.circle,
                            border: Border.all(color: AppTheme.border),
                          ),
                          child: IconButton(
                            icon: const Icon(Icons.swap_vert, color: AppTheme.brandPrimary),
                            onPressed: _swapLocations,
                          ),
                        ),
                      ),
                    ],
                  ),
                  
                  const SizedBox(height: 16),
                  
                  // Date and Passengers
                  Row(
                    children: [
                      Expanded(
                        flex: 5,
                        child: _buildInfoField('Travel date', '24 Sep 2026', Icons.calendar_today),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        flex: 4,
                        child: _buildPassengerField(),
                      ),
                    ],
                  ),
                  
                  const SizedBox(height: 24),
                  
                  // Search Button
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(builder: (context) => const SearchResultsPage()),
                        );
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppTheme.brandPrimary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                      child: const Text('Search buses', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                    ),
                  ),
                ],
              ),
            ),
            
            const SizedBox(height: 32),
            const Text(
              'Popular routes',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTheme.ink),
            ),
            const SizedBox(height: 16),
            
            _buildPopularRouteCard('Kadawatha', 'Makumbura', 'Express', 'LKR 180'),
            const SizedBox(height: 12),
            _buildPopularRouteCard('Makumbura', 'Galle', 'Highway', 'LKR 800'),
          ],
        ),
      ),
    );
  }

  Widget _buildLocationField(String label, String value, IconData icon) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: AppTheme.background,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppTheme.border),
      ),
      child: Row(
        children: [
          Icon(icon, size: 20, color: AppTheme.muted),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: const TextStyle(fontSize: 12, color: AppTheme.muted)),
              const SizedBox(height: 2),
              Text(value, style: const TextStyle(fontWeight: FontWeight.w600, color: AppTheme.ink)),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildInfoField(String label, String value, IconData icon) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: AppTheme.background,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppTheme.border),
      ),
      child: Row(
        children: [
          Icon(icon, size: 18, color: AppTheme.brandPrimary),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: const TextStyle(fontSize: 12, color: AppTheme.muted)),
              Text(value, style: const TextStyle(fontWeight: FontWeight.w600, color: AppTheme.ink)),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildPassengerField() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      decoration: BoxDecoration(
        color: AppTheme.background,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppTheme.border),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          InkWell(
            onTap: () {
              if (_passengerCount > 1) setState(() => _passengerCount--);
            },
            child: const Icon(Icons.remove_circle_outline, color: AppTheme.brandPrimary),
          ),
          Column(
            children: [
              const Text('Passengers', style: TextStyle(fontSize: 11, color: AppTheme.muted)),
              Text('$_passengerCount', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            ],
          ),
          InkWell(
            onTap: () {
              if (_passengerCount < 4) setState(() => _passengerCount++);
            },
            child: const Icon(Icons.add_circle_outline, color: AppTheme.brandPrimary),
          ),
        ],
      ),
    );
  }

  Widget _buildPopularRouteCard(String from, String to, String type, String price) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppTheme.border),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(color: AppTheme.brandLight, borderRadius: BorderRadius.circular(8)),
            child: const Icon(Icons.directions_bus, color: AppTheme.brandPrimary),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('$from - $to', style: const TextStyle(fontWeight: FontWeight.bold)),
                Text(type, style: const TextStyle(color: AppTheme.muted, fontSize: 13)),
              ],
            ),
          ),
          Text(price, style: const TextStyle(fontWeight: FontWeight.bold, color: AppTheme.brandPrimary)),
        ],
      ),
    );
  }
}

