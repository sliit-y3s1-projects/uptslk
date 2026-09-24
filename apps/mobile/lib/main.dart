import 'package:flutter/material.dart';
import 'core/theme/app_theme.dart';
import 'state/demo_store.dart';
import 'features/auth/login_page.dart';
import 'features/commuter/commuter_shell.dart';
import 'features/driver/driver_shell.dart';

final demoStore = DemoStore();

void main() {
  runApp(const UPTSLKApp());
}

class UPTSLKApp extends StatelessWidget {
  const UPTSLKApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'UPTSLK Demo',
      theme: AppTheme.lightTheme,
      home: ListenableBuilder(
        listenable: demoStore,
        builder: (context, child) {
          if (demoStore.currentUser == null) {
            return LoginPage(
              onLogin: (user) => demoStore.login(user),
            );
          }
          
          // Route based on role
          if (demoStore.currentUser!.role == 'commuter') {
            return const CommuterShell();
          } else {
            return const DriverShell();
          }
        },
      ),
    );
  }
}
