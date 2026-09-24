import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';
import '../../core/constants/demo_accounts.dart';
import '../../models/app_user.dart';

class LoginPage extends StatefulWidget {
  final Function(AppUser) onLogin;

  const LoginPage({super.key, required this.onLogin});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  void _handleLogin() {
    final email = _emailController.text.trim();
    if (email == DemoAccounts.commuter.email) {
      widget.onLogin(DemoAccounts.commuter);
    } else if (email == DemoAccounts.driver.email) {
      widget.onLogin(DemoAccounts.driver);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Use one of the demo accounts shown below.'),
          backgroundColor: AppTheme.danger,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
      );
    }
  }

  void _prefill(AppUser user) {
    setState(() {
      _emailController.text = user.email;
      _passwordController.text = 'Demo12345';
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.background,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 32.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const SizedBox(height: 32),
              // Logo Area
              Container(
                height: 72,
                width: 72,
                decoration: BoxDecoration(
                  color: AppTheme.brandLight,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.directions_bus_filled,
                  size: 36,
                  color: AppTheme.brandPrimary,
                ),
              ),
              const SizedBox(height: 24),
              const Text(
                'UPTSLK',
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                  color: AppTheme.brandPrimary,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              const Text(
                'Travel made simple',
                style: TextStyle(
                  fontSize: 16,
                  color: AppTheme.muted,
                  fontWeight: FontWeight.w500,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 48),

              // Inputs
              TextField(
                controller: _emailController,
                style: const TextStyle(color: AppTheme.ink),
                decoration: InputDecoration(
                  labelText: 'Email address',
                  labelStyle: const TextStyle(color: AppTheme.muted),
                  filled: true,
                  fillColor: AppTheme.surface,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: AppTheme.border),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: AppTheme.border),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: AppTheme.brandPrimary, width: 2),
                  ),
                  prefixIcon: const Icon(Icons.email_outlined, color: AppTheme.muted),
                ),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _passwordController,
                obscureText: true,
                style: const TextStyle(color: AppTheme.ink),
                decoration: InputDecoration(
                  labelText: 'Password',
                  labelStyle: const TextStyle(color: AppTheme.muted),
                  filled: true,
                  fillColor: AppTheme.surface,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: AppTheme.border),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: AppTheme.border),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: AppTheme.brandPrimary, width: 2),
                  ),
                  prefixIcon: const Icon(Icons.lock_outline, color: AppTheme.muted),
                ),
              ),
              const SizedBox(height: 24),
              
              // Sign In Button
              ElevatedButton(
                onPressed: _handleLogin,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.brandPrimary,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                child: const Text(
                  'Sign in',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                ),
              ),
              
              const SizedBox(height: 48),
              const Divider(color: AppTheme.border),
              const SizedBox(height: 24),
              
              const Text(
                'Demo access cards',
                style: TextStyle(
                  fontWeight: FontWeight.w600,
                  color: AppTheme.muted,
                  fontSize: 14,
                ),
              ),
              const SizedBox(height: 16),
              
              // Cards
              _buildDemoCard(
                'Commuter',
                DemoAccounts.commuter,
                Icons.person_outline,
              ),
              const SizedBox(height: 12),
              _buildDemoCard(
                'Driver',
                DemoAccounts.driver,
                Icons.badge_outlined,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDemoCard(String role, AppUser user, IconData icon) {
    return InkWell(
      onTap: () => _prefill(user),
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: AppTheme.surface,
          border: Border.all(color: AppTheme.border),
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: AppTheme.ink.withOpacity(0.03),
              blurRadius: 10,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: AppTheme.brandLight,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(icon, color: AppTheme.brandPrimary, size: 24),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    role,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                      color: AppTheme.brandPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    user.email,
                    style: const TextStyle(color: AppTheme.muted, fontSize: 14),
                  ),
                ],
              ),
            ),
            const Icon(Icons.arrow_forward_ios, size: 16, color: AppTheme.muted),
          ],
        ),
      ),
    );
  }
}
