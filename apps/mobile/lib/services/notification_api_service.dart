import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/config/api_config.dart';
import '../models/app_notification.dart';
import 'auth_api_service.dart';

class NotificationApiService {
  NotificationApiService({http.Client? client})
    : _client = client ?? http.Client();

  final http.Client _client;

  Future<List<AppNotification>> list(String token) async {
    final response = await _send('GET', '/api/v1/notifications', token);
    final body = jsonDecode(response.body);
    if (body is! List) {
      throw const AuthApiException('UPTSLK returned an unexpected response.');
    }
    return body
        .whereType<Map>()
        .map(
          (item) => AppNotification.fromJson(Map<String, dynamic>.from(item)),
        )
        .toList();
  }

  Future<void> markRead(String token, String id) =>
      _send('POST', '/api/v1/notifications/$id/read', token);

  Future<void> markAllRead(String token) =>
      _send('POST', '/api/v1/notifications/read-all', token);

  Future<http.Response> _send(String method, String path, String token) async {
    try {
      final baseUrl = ApiConfig.baseUrl.replaceFirst(RegExp(r'/+$'), '');
      final request = http.Request(method, Uri.parse('$baseUrl$path'))
        ..headers.addAll({
          'Accept': 'application/json',
          'Authorization': 'Bearer $token',
        });
      final response = await http.Response.fromStream(
        await _client.send(request),
      );
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AuthApiException(
          'Unable to load notifications.',
          statusCode: response.statusCode,
        );
      }
      return response;
    } on http.ClientException {
      throw const AuthApiException(
        'Unable to reach UPTSLK. Check your connection and try again.',
      );
    } on FormatException {
      throw const AuthApiException('UPTSLK returned an unexpected response.');
    }
  }
}
