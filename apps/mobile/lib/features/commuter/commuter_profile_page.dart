import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../main.dart';

class CommuterProfilePage extends StatelessWidget {
  const CommuterProfilePage({super.key});

  @override
  Widget build(BuildContext context) {
    final user = demoStore.currentUser;
    if (user == null) return const SizedBox.shrink();

    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        title: const Text('Profile', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: AppTheme.surface,
        foregroundColor: AppTheme.brandPrimary,
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
            const SizedBox(height: 20),
            CircleAvatar(
              radius: 48,
              backgroundColor: AppTheme.brandLight,
              child: Text(
                user.name[0].toUpperCase(),
                style: const TextStyle(fontSize: 32, fontWeight: FontWeight.bold, color: AppTheme.brandPrimary),
              ),
            ),
            const SizedBox(height: 16),
            Text(user.name, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: AppTheme.ink)),
            Text(user.email, style: const TextStyle(fontSize: 16, color: AppTheme.muted)),
            
            const SizedBox(height: 48),
            
            Container(
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppTheme.border),
              ),
              child: Column(
                children: [
                  _buildMenuRow(Icons.account_balance_wallet, 'Wallet & Payments'),
                  const Divider(color: AppTheme.border, height: 1),
                  _buildMenuRow(Icons.history, 'Travel History'),
                  const Divider(color: AppTheme.border, height: 1),
                  _buildMenuRow(Icons.settings, 'Settings'),
                ],
              ),
            ),
            
            const SizedBox(height: 32),
            
            SizedBox(
              width: double.infinity,
              child: OutlinedButton(
                onPressed: () {
                  demoStore.logout();
                },
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppTheme.danger,
                  side: const BorderSide(color: AppTheme.danger),
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                child: const Text('Logout', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMenuRow(IconData icon, String title) {
    return ListTile(
      leading: Icon(icon, color: AppTheme.brandPrimary),
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
      trailing: const Icon(Icons.chevron_right, color: AppTheme.muted),
      onTap: () {},
    );
  }
}
