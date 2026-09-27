import 'package:flutter/material.dart';

import 'core/theme/app_theme.dart';
import 'state/demo_store.dart';
import 'state/auth_store.dart';
import 'services/auth_api_service.dart';
import 'features/auth/login_page.dart';
import 'features/commuter/commuter_shell.dart';
import 'features/driver/driver_shell.dart';
import 'features/profile/profile_page.dart';

// Legacy mock screens still reference this store. They are not reachable from
// the current API-backed authentication milestone and will be replaced feature
// by feature as each mobile API flow is integrated.
final demoStore = DemoStore();
final authStore = AuthStore(AuthApiService());

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await authStore.restoreSession();
  runApp(const UPTSLKApp());
}

class UPTSLKApp extends StatelessWidget {
  const UPTSLKApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'UPTSLK',
      theme: AppTheme.lightTheme,
      home: ListenableBuilder(
        listenable: authStore,
        builder: (context, child) {
          final user = authStore.user;
          if (user == null) {
            return LoginPage(authStore: authStore);
          }

          if (user.role == 'Commuter') {
            return CommuterShell(authStore: authStore);
          }

          if (user.role == 'Driver') {
            return DriverShell(authStore: authStore);
          }

          return ProfilePage(authStore: authStore, user: user);
        },
      ),
    );
  }
}
