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

class DutyDetailsPage extends StatelessWidget {
  final String dutyId;

  const DutyDetailsPage({super.key, required this.dutyId});

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: demoStore,
      builder: (context, child) {
        // Find the duty by ID every time it builds, so we get the fresh status!
        final duty = demoStore.myDuties.firstWhere((d) => d.id == dutyId);
        final dep = duty.departure;
        
        final statuses = ['Scheduled', 'Ready', 'Boarding', 'Departed', 'Completed'];
        final isCompleted = duty.status == 'Completed';
        final currentIndex = statuses.indexOf(duty.status);

        void nextStatus() {
          if (currentIndex < statuses.length - 1) {
            demoStore.updateDutyStatus(dutyId, statuses[currentIndex + 1]);
          }
        }

        return Scaffold(
          backgroundColor: AppTheme.background,
          appBar: AppBar(
            title: const Text('Duty Details', style: TextStyle(fontSize: 16)),
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
              children: [
                // Status Card
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: isCompleted ? AppTheme.success : AppTheme.brandPrimary,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.white.withOpacity(0.2),
                          shape: BoxShape.circle,
                        ),
                        child: Icon(isCompleted ? Icons.check : Icons.directions_bus, color: Colors.white),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Current Status', style: TextStyle(color: AppTheme.brandLight, fontSize: 12)),
                            Text(duty.status, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.white)),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                
                const SizedBox(height: 24),
                
                // Info Card
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: AppTheme.surface,
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: AppTheme.border),
                  ),
                  child: Column(
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          _buildInfoCol('Time', formatTime(dep.dateTime)),
                          _buildInfoCol('Route', dep.direction.routeNumber),
                          _buildInfoCol('Bay', dep.bayCode),
                        ],
                      ),
                      const Padding(
                        padding: EdgeInsets.symmetric(vertical: 16),
                        child: Divider(color: AppTheme.border),
                      ),
                      Row(
                        children: [
                          const Icon(Icons.group, color: AppTheme.muted),
                          const SizedBox(width: 12),
                          Text('${duty.passengerCount} Expected Passengers', style: const TextStyle(fontWeight: FontWeight.w600, color: AppTheme.ink)),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          bottomNavigationBar: SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: isCompleted ? null : nextStatus,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.brandPrimary,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    disabledBackgroundColor: AppTheme.border,
                  ),
                  child: Text(
                    isCompleted ? 'Duty Finished' : 'Update to ${statuses[currentIndex + 1]}',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ),
          ),
        );
      }
    );
  }

  Widget _buildInfoCol(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(color: AppTheme.muted, fontSize: 12)),
        const SizedBox(height: 4),
        Text(value, style: const TextStyle(fontWeight: FontWeight.bold, color: AppTheme.ink, fontSize: 16)),
      ],
    );
  }
}
