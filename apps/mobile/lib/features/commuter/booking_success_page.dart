import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../models/booking.dart';

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

class BookingSuccessPage extends StatelessWidget {
  final Booking booking;

  const BookingSuccessPage({super.key, required this.booking});

  @override
  Widget build(BuildContext context) {
    final dep = booking.departure;
    final timeStr = formatTime(dep.dateTime);
    final dateStr = formatDate(dep.dateTime);

    return Scaffold(
      backgroundColor: AppTheme.brandPrimary,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(20),
                decoration: const BoxDecoration(
                  color: AppTheme.success,
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.check, size: 48, color: Colors.white),
              ),
              const SizedBox(height: 32),
              const Text(
                'Booking confirmed',
                style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              const SizedBox(height: 48),
              
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: AppTheme.surface,
                  borderRadius: BorderRadius.circular(16),
                ),
                child: Column(
                  children: [
                    Text(
                      '${dep.direction.routeNumber} · $timeStr',
                      style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTheme.ink),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      '$dateStr · ${dep.bayCode}',
                      style: const TextStyle(fontSize: 16, color: AppTheme.muted),
                    ),
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 24),
                      child: Divider(color: AppTheme.border),
                    ),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('Passengers', style: TextStyle(color: AppTheme.muted)),
                        Text('${booking.passengerCount}', style: const TextStyle(fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ],
                ),
              ),
              
              const SizedBox(height: 48),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () {
                    Navigator.of(context).popUntil((route) => route.isFirst);
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.white,
                    foregroundColor: AppTheme.brandPrimary,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  child: const Text('Back to home', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
