import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../main.dart';

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

class CommuterTicketsPage extends StatelessWidget {
  const CommuterTicketsPage({super.key});

  @override
  Widget build(BuildContext context) {
    // ListenableBuilder updates the UI when demoStore notifies changes
    return ListenableBuilder(
      listenable: demoStore,
      builder: (context, child) {
        final bookings = demoStore.myBookings.reversed.toList(); // Newest first

        return Scaffold(
          backgroundColor: AppTheme.background,
          appBar: AppBar(
            title: const Text('My Tickets', style: TextStyle(fontWeight: FontWeight.bold)),
            backgroundColor: AppTheme.surface,
            foregroundColor: AppTheme.brandPrimary,
            elevation: 0,
            bottom: PreferredSize(
              preferredSize: const Size.fromHeight(1),
              child: Container(color: AppTheme.border, height: 1),
            ),
          ),
          body: bookings.isEmpty
              ? const Center(
                  child: Text(
                    'No upcoming trips.\nFind a bus to book a ticket!',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: AppTheme.muted, fontSize: 16),
                  ),
                )
              : ListView.separated(
                  padding: const EdgeInsets.all(20),
                  itemCount: bookings.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 16),
                  itemBuilder: (context, index) {
                    final b = bookings[index];
                    final dep = b.departure;
                    
                    return InkWell(
                      onTap: () => _showQrDialog(context, b.id),
                      borderRadius: BorderRadius.circular(16),
                      child: Container(
                        padding: const EdgeInsets.all(20),
                        decoration: BoxDecoration(
                          color: AppTheme.surface,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppTheme.border),
                          boxShadow: [
                            BoxShadow(
                              color: AppTheme.ink.withOpacity(0.04),
                              blurRadius: 10,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        child: Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: AppTheme.brandLight,
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: const Icon(Icons.qr_code_2, color: AppTheme.brandPrimary, size: 32),
                            ),
                            const SizedBox(width: 16),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    '${dep.direction.origin} ? ${dep.direction.destination}',
                                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: AppTheme.ink),
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    '${formatDate(dep.dateTime)} · ${formatTime(dep.dateTime)}',
                                    style: const TextStyle(color: AppTheme.muted, fontSize: 13),
                                  ),
                                  const SizedBox(height: 8),
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: AppTheme.brandPrimary.withOpacity(0.1),
                                      borderRadius: BorderRadius.circular(6),
                                    ),
                                    child: Text(
                                      b.status,
                                      style: const TextStyle(color: AppTheme.brandPrimary, fontSize: 11, fontWeight: FontWeight.bold),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
        );
      },
    );
  }

  void _showQrDialog(BuildContext context, String bookingId) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppTheme.surface,
        title: const Text('Boarding Pass', textAlign: TextAlign.center),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.qr_code_2, size: 200, color: AppTheme.ink),
            const SizedBox(height: 16),
            Text(bookingId, style: const TextStyle(color: AppTheme.muted)),
            const SizedBox(height: 8),
            const Text('Show this to the driver', style: TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }
}
