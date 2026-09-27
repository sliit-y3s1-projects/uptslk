class ApiConfig {
  const ApiConfig._();

  /// Android emulator default. Override for a physical device or deployed API:
  /// flutter run --dart-define=UPTSLK_API_BASE_URL=https://api.example.com
  static const baseUrl = String.fromEnvironment(
    'UPTSLK_API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5250',
  );
}
