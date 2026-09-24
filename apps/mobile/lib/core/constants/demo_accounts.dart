import '../../models/app_user.dart';

class DemoAccounts {
  static const AppUser commuter = AppUser(
    id: 'usr-commuter',
    name: 'Sahan Perera',
    email: 'commuter@demo.upts.lk',
    role: 'commuter',
  );

  static const AppUser driver = AppUser(
    id: 'usr-driver',
    name: 'Nimal Silva',
    email: 'nimal.driver@demo.upts.lk',
    role: 'driver',
  );
}
