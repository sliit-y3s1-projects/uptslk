import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../core/config/api_config.dart';
import '../models/app_user.dart';

class AuthApiException implements Exception {
  const AuthApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class AuthResult {
  const AuthResult({required this.token, required this.user});

  final String token;
  final AppUser user;
}

class AuthApiService {
  AuthApiService({http.Client? client}) : _client = client ?? http.Client();

  final http.Client _client;

  Future<AuthResult> login({required String email, required String password}) {
    return _authenticate('/api/v1/auth/login', {
      'email': email,
      'password': password,
    });
  }

  Future<AuthResult> register({
    required String name,
    required String email,
    required String password,
  }) {
    return _authenticate('/api/v1/auth/register', {
      'name': name,
      'email': email,
      'password': password,
    });
  }

  Future<AppUser> getCurrentUser(String token) async {
    try {
      final response = await _client.get(
        _uri('/api/v1/auth/me'),
        headers: _headers(token: token),
      );
      final body = _decode(response);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AuthApiException(
          _errorMessage(body, 'Unable to load your profile.'),
          statusCode: response.statusCode,
        );
      }
      return AppUser.fromJson(_asMap(body));
    } on AuthApiException {
      rethrow;
    } on http.ClientException {
      throw const AuthApiException(
        'Unable to reach UPTSLK. Check that the API is running and the mobile API URL is correct.',
      );
    } on FormatException {
      throw const AuthApiException('UPTSLK returned an unexpected response.');
    }
  }

  Future<AppUser> updateProfile({
    required String token,
    required String name,
    String? homeLocation,
    String? nicNumber,
    String? gender,
  }) async {
    return _authenticatedUserRequest(
      method: 'PATCH',
      path: '/api/v1/auth/me',
      token: token,
      body: {
        'name': name,
        'homeLocation': homeLocation,
        'nicNumber': nicNumber,
        'gender': gender,
      },
      fallback: 'Unable to update your profile.',
    );
  }

  Future<String> uploadProfilePhoto({
    required String token,
    required Uint8List bytes,
    required String fileName,
    required String contentType,
  }) async {
    try {
      final request =
          http.MultipartRequest('POST', _uri('/api/v1/auth/me/profile-photo'))
            ..headers['Accept'] = 'application/json'
            ..headers['Authorization'] = 'Bearer $token'
            ..files.add(
              http.MultipartFile.fromBytes(
                'file',
                bytes,
                filename: fileName,
                contentType: http.MediaType.parse(contentType),
              ),
            );
      final streamed = await _client.send(request);
      final response = await http.Response.fromStream(streamed);
      final body = _decode(response);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AuthApiException(
          _errorMessage(body, 'Unable to upload your profile photo.'),
          statusCode: response.statusCode,
        );
      }
      final url = _asMap(body)['profilePhotoUrl']?.toString();
      if (url == null || url.isEmpty) {
        throw const AuthApiException(
          'The server did not return a profile photo URL.',
        );
      }
      return url;
    } on AuthApiException {
      rethrow;
    } on http.ClientException {
      throw const AuthApiException(
        'Unable to reach UPTSLK while uploading the photo.',
      );
    } on FormatException {
      throw const AuthApiException('UPTSLK returned an unexpected response.');
    }
  }

  Future<void> logout(String token) async {
    try {
      await _client.post(
        _uri('/api/v1/auth/logout'),
        headers: _headers(token: token),
      );
    } on http.ClientException {
      // A local sign-out must still succeed if the API cannot be reached.
    }
  }

  Future<AppUser> _authenticatedUserRequest({
    required String method,
    required String path,
    required String token,
    required Map<String, dynamic> body,
    required String fallback,
  }) async {
    try {
      final request = http.Request(method, _uri(path))
        ..headers.addAll(_headers(token: token))
        ..body = jsonEncode(body);
      final streamed = await _client.send(request);
      final response = await http.Response.fromStream(streamed);
      final decoded = _decode(response);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AuthApiException(
          _errorMessage(decoded, fallback),
          statusCode: response.statusCode,
        );
      }
      return AppUser.fromJson(_asMap(decoded));
    } on AuthApiException {
      rethrow;
    } on http.ClientException {
      throw const AuthApiException(
        'Unable to reach UPTSLK. Check your connection and try again.',
      );
    } on FormatException {
      throw const AuthApiException('UPTSLK returned an unexpected response.');
    }
  }

  Future<AuthResult> _authenticate(
    String path,
    Map<String, String> payload,
  ) async {
    try {
      final response = await _client.post(
        _uri(path),
        headers: _headers(),
        body: jsonEncode(payload),
      );
      final body = _decode(response);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AuthApiException(
          _errorMessage(body, 'We could not complete that request.'),
          statusCode: response.statusCode,
        );
      }

      final result = _asMap(body);
      final token = result['token']?.toString();
      if (token == null || token.isEmpty) {
        throw const AuthApiException(
          'The server did not return a session token.',
        );
      }
      return AuthResult(token: token, user: AppUser.fromJson(result));
    } on AuthApiException {
      rethrow;
    } on http.ClientException {
      throw const AuthApiException(
        'Unable to reach UPTSLK. Check that the API is running and the mobile API URL is correct.',
      );
    } on FormatException {
      throw const AuthApiException('UPTSLK returned an unexpected response.');
    }
  }

  Uri _uri(String path) {
    final baseUrl = ApiConfig.baseUrl.replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$baseUrl$path');
  }

  Map<String, String> _headers({String? token}) => {
    'Accept': 'application/json',
    'Content-Type': 'application/json',
    if (token != null) 'Authorization': 'Bearer $token',
  };

  dynamic _decode(http.Response response) {
    if (response.body.isEmpty) return <String, dynamic>{};
    return jsonDecode(response.body);
  }

  Map<String, dynamic> _asMap(dynamic value) {
    if (value is Map<String, dynamic>) return value;
    if (value is Map) return Map<String, dynamic>.from(value);
    throw const FormatException('Expected a JSON object.');
  }

  String _errorMessage(dynamic value, String fallback) {
    if (value is! Map) return fallback;
    final error = value['error'] ?? value['detail'] ?? value['title'];
    if (error is List) return error.map((item) => item.toString()).join('\n');
    final message = error?.toString().trim();
    return message == null || message.isEmpty ? fallback : message;
  }
}
