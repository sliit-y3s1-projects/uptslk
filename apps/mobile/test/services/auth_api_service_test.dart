import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/services/auth_api_service.dart';

void main() {
  group('AuthApiService', () {
    test('signs in and maps the API user response', () async {
      final client = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, '/api/v1/auth/login');
        expect(jsonDecode(request.body), {
          'email': 'commuter@example.com',
          'password': 'Password123',
        });
        return http.Response(
          jsonEncode({
            'token': 'jwt-token',
            'userId': 'user-1',
            'name': 'Sahan Perera',
            'email': 'commuter@example.com',
            'role': 'Commuter',
            'centreId': null,
            'profilePhotoUrl': null,
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });
      final service = AuthApiService(client: client);

      final result = await service.login(
        email: 'commuter@example.com',
        password: 'Password123',
      );

      expect(result.token, 'jwt-token');
      expect(result.user.id, 'user-1');
      expect(result.user.name, 'Sahan Perera');
      expect(result.user.roleLabel, 'Commuter');
    });

    test('returns the API error message for invalid credentials', () async {
      final service = AuthApiService(
        client: MockClient(
          (_) async => http.Response(
            jsonEncode({'error': 'Invalid credentials'}),
            401,
            headers: {'content-type': 'application/json'},
          ),
        ),
      );

      expect(
        () => service.login(email: 'wrong@example.com', password: 'wrong'),
        throwsA(
          isA<AuthApiException>().having(
            (error) => error.message,
            'message',
            'Invalid credentials',
          ),
        ),
      );
    });
  });
}
