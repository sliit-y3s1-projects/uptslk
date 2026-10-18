import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/services/auth_api_service.dart';
import 'package:mobile/services/session_storage.dart';
import 'package:mobile/state/auth_store.dart';

class _MemoryStorage implements SessionStorage {
  _MemoryStorage([this.token]);

  String? token;
  bool failWrites = false;

  @override
  Future<String?> readToken() async => token;

  @override
  Future<void> saveToken(String value) async {
    if (failWrites) throw Exception('Keychain unavailable');
    token = value;
  }

  @override
  Future<void> clearToken() async => token = null;
}

http.Response _json(Object body, [int status = 200]) => http.Response(
  jsonEncode(body),
  status,
  headers: {'content-type': 'application/json'},
);

Map<String, Object?> _user({String role = 'Commuter', String name = 'Sahan'}) =>
    {'id': 'user-1', 'name': name, 'email': 'sahan@example.com', 'role': role};

void main() {
  group('AuthStore session restore', () {
    test('restores the user from a saved token', () async {
      final storage = _MemoryStorage('saved-token');
      final store = AuthStore(
        AuthApiService(
          client: MockClient((request) async {
            expect(request.url.path, '/api/v1/auth/me');
            expect(request.headers['authorization'], 'Bearer saved-token');
            return _json(_user(role: 'Driver'));
          }),
        ),
        sessionStorage: storage,
      );
      addTearDown(store.dispose);

      await store.restoreSession();

      expect(store.isRestoring, isFalse);
      expect(store.isAuthenticated, isTrue);
      expect(store.token, 'saved-token');
      expect(store.user!.role, 'Driver');
    });

    test(
      'clears an expired token instead of keeping a broken session',
      () async {
        final storage = _MemoryStorage('expired-token');
        final store = AuthStore(
          AuthApiService(
            client: MockClient(
              (_) async => _json({'error': 'Unauthorized'}, 401),
            ),
          ),
          sessionStorage: storage,
        );
        addTearDown(store.dispose);

        await store.restoreSession();

        expect(store.isAuthenticated, isFalse);
        expect(store.user, isNull);
        expect(storage.token, isNull);
        expect(store.isRestoring, isFalse);
      },
    );

    test(
      'starts signed out without calling the API when nothing is saved',
      () async {
        var calls = 0;
        final store = AuthStore(
          AuthApiService(
            client: MockClient((_) async {
              calls++;
              return _json(_user());
            }),
          ),
          sessionStorage: _MemoryStorage(),
        );
        addTearDown(store.dispose);

        await store.restoreSession();

        expect(calls, 0);
        expect(store.isAuthenticated, isFalse);
        expect(store.isRestoring, isFalse);
      },
    );

    test(
      'treats an unreachable API as signed out and clears the token',
      () async {
        final storage = _MemoryStorage('saved-token');
        final store = AuthStore(
          AuthApiService(
            client: MockClient(
              (_) async => throw http.ClientException('offline'),
            ),
          ),
          sessionStorage: storage,
        );
        addTearDown(store.dispose);

        await store.restoreSession();

        expect(store.isAuthenticated, isFalse);
        expect(storage.token, isNull);
      },
    );
  });

  group('AuthStore sign in and out', () {
    test('signing in saves the token and loads the full profile', () async {
      final storage = _MemoryStorage();
      final store = AuthStore(
        AuthApiService(
          client: MockClient((request) async {
            if (request.url.path == '/api/v1/auth/login') {
              return _json({'token': 'new-token', ..._user(name: 'Short')});
            }
            expect(request.url.path, '/api/v1/auth/me');
            return _json(_user(name: 'Sahan Perera'));
          }),
        ),
        sessionStorage: storage,
      );
      addTearDown(store.dispose);
      var notifications = 0;
      store.addListener(() => notifications++);

      await store.signIn(email: 'sahan@example.com', password: 'Password123');

      expect(storage.token, 'new-token');
      expect(store.token, 'new-token');
      expect(store.user!.name, 'Sahan Perera');
      expect(notifications, greaterThanOrEqualTo(2));
    });

    test(
      'a failed sign in keeps the user signed out and saves nothing',
      () async {
        final storage = _MemoryStorage();
        final store = AuthStore(
          AuthApiService(
            client: MockClient(
              (_) async => _json({'error': 'Invalid credentials'}, 401),
            ),
          ),
          sessionStorage: storage,
        );
        addTearDown(store.dispose);

        await expectLater(
          store.signIn(email: 'x@example.com', password: 'wrong'),
          throwsA(isA<AuthApiException>()),
        );
        expect(store.isAuthenticated, isFalse);
        expect(storage.token, isNull);
      },
    );

    test('a keychain failure does not block signing in', () async {
      final storage = _MemoryStorage()..failWrites = true;
      final store = AuthStore(
        AuthApiService(
          client: MockClient((request) async {
            if (request.url.path == '/api/v1/auth/login') {
              return _json({'token': 'new-token', ..._user()});
            }
            return _json(_user());
          }),
        ),
        sessionStorage: storage,
      );
      addTearDown(store.dispose);

      await store.signIn(email: 'sahan@example.com', password: 'Password123');

      expect(store.isAuthenticated, isTrue);
    });

    test('signing out clears the session and tells the API', () async {
      final storage = _MemoryStorage('saved-token');
      final paths = <String>[];
      final store = AuthStore(
        AuthApiService(
          client: MockClient((request) async {
            paths.add(request.url.path);
            if (request.url.path == '/api/v1/auth/logout') {
              expect(request.headers['authorization'], 'Bearer saved-token');
              return http.Response('', 204);
            }
            return _json(_user());
          }),
        ),
        sessionStorage: storage,
      );
      addTearDown(store.dispose);
      await store.restoreSession();

      await store.signOut();

      expect(store.isAuthenticated, isFalse);
      expect(storage.token, isNull);
      expect(paths, contains('/api/v1/auth/logout'));
    });

    test(
      'updating the profile without a session asks the user to sign in',
      () async {
        final store = AuthStore(
          AuthApiService(client: MockClient((_) async => _json(_user()))),
          sessionStorage: _MemoryStorage(),
        );
        addTearDown(store.dispose);

        expect(
          () => store.updateProfile(name: 'New name'),
          throwsA(
            isA<AuthApiException>().having(
              (error) => error.message,
              'message',
              contains('sign in again'),
            ),
          ),
        );
      },
    );
  });
}
