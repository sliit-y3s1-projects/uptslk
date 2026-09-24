import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../main.dart';
import 'duty_details_page.dart';

String formatTime(DateTime d) {
  final hr = d.hour == 0 ? 12 : (d.hour > 12 ? d.hour - 12 : d.hour);
  final hrStr = hr.toString().padLeft(2, '0');
  final minStr = d.minute.toString().padLeft(2, '0');
  final amPm = d.hour >= 12 ? 'PM' : 'AM';
  return '$hrStr:$minStr $amPm';
}

class DriverHomePage extends StatelessWidget {
  const DriverHomePage({super.key});

  @override
  Widget build(BuildContext context) {
    final userName = demoStore.currentUser?.name.split(' ').first ?? 'Driver';

    return ListenableBuilder(
      listenable: demoStore,
      builder: (context, child) {
        final duties = demoStore.myDuties;
        
        return Scaffold(
          backgroundColor: AppTheme.background,
          appBar: AppBar(
            backgroundColor: AppTheme.brandPrimary,
            elevation: 0,
            title: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Welcome back,', style: TextStyle(fontSize: 14, color: AppTheme.brandLight)),
                Text(userName, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.white)),
              ],
            ),
          ),
          body: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(20),
                color: AppTheme.brandPrimary,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  decoration: BoxDecoration(
                    color: Colors.white.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Row(
                    children: [
                      Icon(Icons.directions_bus, color: Colors.white),
                      SizedBox(width: 12),
                      Text('Bus ND-8899 assigned for today', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w600)),
                    ],
                  ),
                ),
              ),
              
              const Padding(
                padding: EdgeInsets.fromLTRB(20, 24, 20, 12),
                child: Text('Today''s Duties', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTheme.ink)),
              ),
              
              Expanded(
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  itemCount: duties.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 16),
                  itemBuilder: (context, index) {
                    final duty = duties[index];
                    final dep = duty.departure;
                    final isCompleted = duty.status == 'Completed';
                    
                    return InkWell(
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(builder: (context) => DutyDetailsPage(dutyId: duty.id)),
                        );
                      },
                      borderRadius: BorderRadius.circular(16),
                      child: Container(
                        padding: const EdgeInsets.all(20),
                        decoration: BoxDecoration(
                          color: AppTheme.surface,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppTheme.border),
                          boxShadow: [BoxShadow(color: AppTheme.ink.withOpacity(0.04), blurRadius: 10, offset: const Offset(0, 4))],
                        ),
                        child: Column(
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(formatTime(dep.dateTime), style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTheme.brandPrimary)),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                  decoration: BoxDecoration(
                                    color: isCompleted ? AppTheme.success.withOpacity(0.1) : (duty.status == 'Scheduled' ? AppTheme.muted.withOpacity(0.1) : AppTheme.brandPrimary.withOpacity(0.1)),
                                    borderRadius: BorderRadius.circular(6),
                                  ),
                                  child: Text(duty.status, style: TextStyle(color: isCompleted ? AppTheme.success : (duty.status == 'Scheduled' ? AppTheme.muted : AppTheme.brandPrimary), fontSize: 12, fontWeight: FontWeight.bold)),
                                ),
                              ],
                            ),
                            const Padding(
                              padding: EdgeInsets.symmetric(vertical: 16),
                              child: Divider(color: AppTheme.border),
                            ),
                            Row(
                              children: [
                                const Icon(Icons.route, color: AppTheme.muted, size: 20),
                                const SizedBox(width: 8),
                                Expanded(child: Text('${dep.direction.origin} ? ${dep.direction.destination}', style: const TextStyle(fontWeight: FontWeight.w600, color: AppTheme.ink))),
                              ],
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                const Icon(Icons.people_outline, color: AppTheme.muted, size: 20),
                                const SizedBox(width: 8),
                                Text('${duty.passengerCount} Passengers boarded', style: const TextStyle(color: AppTheme.muted)),
                              ],
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        );
      }
    );
  }
}
