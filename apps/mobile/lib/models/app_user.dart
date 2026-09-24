class AppUser {
  final String id;
  final String name;
  final String email;
  final String role; // 'commuter' or 'driver'

  const AppUser({
    required this.id,
    required this.name,
    required this.email,
    required this.role,
  });
}
