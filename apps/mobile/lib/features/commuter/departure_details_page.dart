import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../models/departure.dart';
import '../../models/booking.dart';
import '../../main.dart';
import 'booking_success_page.dart';

String formatTime(DateTime d) {
  final hr = d.hour == 0 ? 12 : (d.hour > 12 ? d.hour - 12 : d.hour);
  final hrStr = hr.toString().padLeft(2, '0');
  final minStr = d.minute.toString().padLeft(2, '0');
  final amPm = d.hour >= 12 ? 'PM' : 'AM';
  return '$hrStr:$minStr $amPm';
}

String formatDate(DateTime d) {
  const m = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  return '${d.day.toString().padLeft(2, '0')} ${m[d.month - 1]} ${d.year}';
}

class DepartureDetailsPage extends StatefulWidget {
  final Departure departure;
  final int passengerCount;

  const DepartureDetailsPage({
    super.key,
    required this.departure,
    required this.passengerCount,
  });

  @override
  State<DepartureDetailsPage> createState() => _DepartureDetailsPageState();
}

class _DepartureDetailsPageState extends State<DepartureDetailsPage> {
  late int _passengers;

  @override
  void initState() {
    super.initState();
    _passengers = widget.passengerCount;
  }

  void _confirmBooking() {
    final newBooking = Booking(
      id: 'bkg-$_passengers-${DateTime.now().millisecondsSinceEpoch}',
      departure: widget.departure,
      passengerCount: _passengers,
      status: 'Upcoming',
    );
    
    demoStore.myBookings.add(newBooking);
    demoStore.notifyListeners();

    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (context) => BookingSuccessPage(booking: newBooking)),
    );
  }

  @override
  Widget build(BuildContext context) {
    final dep = widget.departure;
    final totalFare = dep.fare * _passengers;
    final timeStr = formatTime(dep.dateTime);
    final dateStr = formatDate(dep.dateTime);

    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        title: const Text('Review Booking', style: TextStyle(fontSize: 16)),
        backgroundColor: AppTheme.surface,
        foregroundColor: AppTheme.ink,
        elevation: 0,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: AppTheme.border, height: 1),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppTheme.border),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${dep.direction.origin} ? ${dep.direction.destination}',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.brandPrimary),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${dep.direction.routeNumber} · ${dep.direction.routeName}',
                    style: const TextStyle(color: AppTheme.muted, fontSize: 13),
                  ),
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 16),
                    child: Divider(color: AppTheme.border),
                  ),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      _buildMiniInfo('Date', dateStr),
                      _buildMiniInfo('Time', timeStr),
                      _buildMiniInfo('Bay', dep.bayCode),
                    ],
                  ),
                ],
              ),
            ),
            
            const SizedBox(height: 24),
            const Text('Passengers', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppTheme.border),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Number of seats', style: TextStyle(color: AppTheme.ink, fontSize: 16)),
                  Row(
                    children: [
                      IconButton(
                        onPressed: () {
                          if (_passengers > 1) setState(() => _passengers--);
                        },
                        icon: const Icon(Icons.remove_circle_outline, color: AppTheme.brandPrimary),
                      ),
                      const SizedBox(width: 8),
                      Text('$_passengers', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                      const SizedBox(width: 8),
                      IconButton(
                        onPressed: () {
                          if (_passengers < 4) setState(() => _passengers++);
                        },
                        icon: const Icon(Icons.add_circle_outline, color: AppTheme.brandPrimary),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            
            const SizedBox(height: 24),
            const Text('Payment', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppTheme.border),
              ),
              child: const Row(
                children: [
                  Icon(Icons.account_balance_wallet, color: AppTheme.brandPrimary),
                  SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('UPTSLK Wallet', style: TextStyle(fontWeight: FontWeight.bold)),
                        Text('LKR 2,500 available', style: TextStyle(color: AppTheme.muted, fontSize: 12)),
                      ],
                    ),
                  ),
                  Icon(Icons.check_circle, color: AppTheme.success, size: 20),
                ],
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        child: Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: AppTheme.surface,
            boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10, offset: const Offset(0, -5))],
          ),
          child: Row(
            children: [
              Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Total Fare', style: TextStyle(color: AppTheme.muted, fontSize: 12)),
                  Text('LKR $totalFare', style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTheme.ink)),
                ],
              ),
              const SizedBox(width: 24),
              Expanded(
                child: ElevatedButton(
                  onPressed: _confirmBooking,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.brandPrimary,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  child: const Text('Confirm booking', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildMiniInfo(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(color: AppTheme.muted, fontSize: 12)),
        const SizedBox(height: 2),
        Text(value, style: const TextStyle(fontWeight: FontWeight.bold, color: AppTheme.ink)),
      ],
    );
  }
}
