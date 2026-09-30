import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/config/api_config.dart';
import '../models/driver_assignment.dart';
import 'auth_api_service.dart';

class DriverApiService {
  DriverApiService({http.Client? client}) : _client = client ?? http.Client();

  final http.Client _client;

  Future<DriverOperationalProfile> getProfile(String token) async {
    final body = await _get('/api/v1/drivers/me', token);
    return DriverOperationalProfile.fromJson(_asMap(body));
  }

  Future<List<DriverAssignment>> getDuties({
    required String token,
    required DateTime fromDate,
    required DateTime toDate,
  }) async {
    final body = await _get(
      '/api/v1/drivers/me/trips?fromDate=${_day(fromDate)}&toDate=${_day(toDate)}',
      token,
    );
    if (body is! List) {
      throw const AuthApiException('UPTSLK returned an unexpected response.');
    }
    return body.map((item) => DriverAssignment.fromJson(_asMap(item))).toList();
  }

  Future<void> updateDutyStatus({
    required String token,
    required String tripId,
    required String status,
    String? note,
  }) async {
    final body = <String, dynamic>{'status': status};
    if (note != null) body['note'] = note;
    await _request(
      method: 'PATCH',
      path: '/api/v1/drivers/me/trips/$tripId/status',
      token: token,
      body: body,
    );
  }

  Future<void> reportIncident({
    required String token,
    required String tripId,
    required String type,
    required String severity,
    required String title,
    required String description,
  }) async {
    await _request(
      method: 'POST',
      path: '/api/v1/drivers/me/trips/$tripId/incidents',
      token: token,
      body: {
        'type': type,
        'severity': severity,
        'title': title,
        'description': description,
      },
    );
  }

  String _day(DateTime value) =>
      '${value.year.toString().padLeft(4, '0')}-${value.month.toString().padLeft(2, '0')}-${value.day.toString().padLeft(2, '0')}';

  Future<dynamic> _get(String path, String token) =>
      _request(method: 'GET', path: path, token: token);

  Future<dynamic> _request({
    required String method,
    required String path,
    required String token,
    Map<String, dynamic>? body,
  }) async {
    try {
      final request = http.Request(method, _uri(path))
        ..headers.addAll({
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        });
      if (body != null) request.body = jsonEncode(body);
      final streamed = await _client.send(request);
      final response = await http.Response.fromStream(streamed);
      final decoded = response.body.isEmpty
          ? <String, dynamic>{}
          : jsonDecode(response.body);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AuthApiException(
          _errorMessage(decoded),
          statusCode: response.statusCode,
        );
      }
      return decoded;
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

  Uri _uri(String path) {
    final baseUrl = ApiConfig.baseUrl.replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$baseUrl$path');
  }

  Map<String, dynamic> _asMap(dynamic value) {
    if (value is Map<String, dynamic>) return value;
    if (value is Map) return Map<String, dynamic>.from(value);
    throw const FormatException('Expected a JSON object.');
  }

  String _errorMessage(dynamic value) {
    if (value is Map) {
      final error = value['error'] ?? value['detail'] ?? value['title'];
      if (error is List) return error.join('\n');
      if (error != null && error.toString().trim().isNotEmpty) {
        return error.toString();
      }
    }
    return 'Unable to load driver duties.';
  }
}
