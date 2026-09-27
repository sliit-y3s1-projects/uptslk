import 'package:flutter/foundation.dart';

import '../models/app_user.dart';
import '../services/auth_api_service.dart';
import '../services/session_storage.dart';

class AuthStore extends ChangeNotifier {
  AuthStore(this._api, {SessionStorage? sessionStorage})
    : _sessionStorage = sessionStorage ?? SecureSessionStorage();

  final AuthApiService _api;
  final SessionStorage _sessionStorage;

  AppUser? _user;
  String? _token;
  bool _isRestoring = true;

  AppUser? get user => _user;
  String? get token => _token;
  bool get isAuthenticated => _user != null && _token != null;
  bool get isRestoring => _isRestoring;

  Future<void> restoreSession() async {
    try {
      final token = await _sessionStorage.readToken();
      if (token != null && token.isNotEmpty) {
        _user = await _api.getCurrentUser(token);
        _token = token;
      }
    } on AuthApiException {
      await _clearStoredSession();
      _token = null;
      _user = null;
    } catch (_) {
      await _clearStoredSession();
      _token = null;
      _user = null;
    } finally {
      _isRestoring = false;
      notifyListeners();
    }
  }

  Future<void> signIn({required String email, required String password}) async {
    final result = await _api.login(email: email, password: password);
    _token = result.token;
    _user = result.user;
    await _saveSession(result.token);
    notifyListeners();
    await _refreshProfile();
  }

  Future<void> signUp({
    required String name,
    required String email,
    required String password,
  }) async {
    final result = await _api.register(
      name: name,
      email: email,
      password: password,
    );
    _token = result.token;
    _user = result.user;
    await _saveSession(result.token);
    notifyListeners();
    await _refreshProfile();
  }

  Future<void> _refreshProfile() async {
    final token = _token;
    if (token == null) return;
    try {
      _user = await _api.getCurrentUser(token);
      notifyListeners();
    } on AuthApiException {
      // Login/register already returned a valid minimal user. The profile page
      // remains usable if the follow-up read is temporarily unavailable.
    }
  }

  Future<void> updateProfile({
    required String name,
    String? homeLocation,
    String? nicNumber,
    String? gender,
  }) async {
    final token = _requireToken();
    _user = await _api.updateProfile(
      token: token,
      name: name,
      homeLocation: homeLocation,
      nicNumber: nicNumber,
      gender: gender,
    );
    notifyListeners();
  }

  Future<void> uploadProfilePhoto({
    required List<int> bytes,
    required String fileName,
    required String contentType,
  }) async {
    final token = _requireToken();
    final photoUrl = await _api.uploadProfilePhoto(
      token: token,
      bytes: Uint8List.fromList(bytes),
      fileName: fileName,
      contentType: contentType,
    );
    final currentUser = _user;
    if (currentUser != null) {
      _user = currentUser.copyWith(profilePhotoUrl: photoUrl);
      notifyListeners();
    }
  }

  Future<void> signOut() async {
    final token = _token;
    _token = null;
    _user = null;
    await _clearStoredSession();
    notifyListeners();
    if (token != null) await _api.logout(token);
  }

  Future<void> _clearStoredSession() async {
    try {
      await _sessionStorage.clearToken();
    } catch (_) {
      // Keep the app usable if the device keychain is temporarily unavailable.
    }
  }

  Future<void> _saveSession(String token) async {
    try {
      await _sessionStorage.saveToken(token);
    } catch (_) {
      // The current session remains valid even if secure persistence fails.
    }
  }

  String _requireToken() {
    final token = _token;
    if (token == null || token.isEmpty) {
      throw const AuthApiException(
        'Your session has ended. Please sign in again.',
      );
    }
    return token;
  }
}
