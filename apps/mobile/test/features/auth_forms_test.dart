import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/core/theme/app_theme.dart';
import 'package:mobile/features/auth/auth_widgets.dart';
import 'package:mobile/features/auth/login_page.dart';
import 'package:mobile/features/auth/register_page.dart';
import 'package:mobile/services/auth_api_service.dart';
import 'package:mobile/services/session_storage.dart';
import 'package:mobile/state/auth_store.dart';

class _MemoryStorage implements SessionStorage {
  String? token;

  @override
  Future<String?> readToken() async => token;

  @override
  Future<void> saveToken(String value) async => token = value;

  @override
  Future<void> clearToken() async => token = null;
}

/// Records every request and answers with [respond].
class _Api {
  _Api(this.respond);

  final http.Response Function(http.Request request) respond;
  final requests = <http.Request>[];

  late final client = MockClient((request) async {
    requests.add(request);
    return respond(request);
  });
}

http.Response _json(Object body, [int status = 200]) => http.Response(
  jsonEncode(body),
  status,
  headers: {'content-type': 'application/json'},
);

final _commuter = {
  'token': 'jwt-token',
  'userId': 'user-1',
  'name': 'Nimali Silva',
  'email': 'nimali@example.com',
  'role': 'Commuter',
};

Future<AuthStore> _pumpLogin(WidgetTester tester, _Api api) async {
  tester.view.physicalSize = const Size(430, 1200);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final store = AuthStore(
    AuthApiService(client: api.client),
    sessionStorage: _MemoryStorage(),
  );
  addTearDown(store.dispose);
  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.lightTheme,
      home: LoginPage(authStore: store),
    ),
  );
  return store;
}

Finder _field(String label) => find.descendant(
  of: find.widgetWithText(AuthField, label),
  matching: find.byType(TextField),
);

Future<void> _tapButton(WidgetTester tester, String label) async {
  final button = find.widgetWithText(PrimaryAuthButton, label);
  await tester.ensureVisible(button);
  await tester.tap(button);
  await tester.pumpAndSettle();
}

Future<void> _openRegister(WidgetTester tester) async {
  final link = find.widgetWithText(TextButton, 'Create account');
  await tester.ensureVisible(link);
  await tester.tap(link);
  await tester.pumpAndSettle();
}

Future<void> _fillRegister(
  WidgetTester tester, {
  String name = 'Nimali Silva',
  String email = 'nimali@example.com',
  String password = 'Password123',
  String? confirm,
}) async {
  await tester.enterText(_field('Full name'), name);
  await tester.enterText(_field('Email address'), email);
  await tester.enterText(_field('Password'), password);
  await tester.enterText(_field('Confirm password'), confirm ?? password);
}

void main() {
  group('Login form', () {
    testWidgets('asks for both fields and sends nothing when they are empty', (
      tester,
    ) async {
      final api = _Api((_) => _json(_commuter));
      final store = await _pumpLogin(tester, api);

      await tester.enterText(_field('Email address'), '   ');
      await _tapButton(tester, 'Sign in');

      expect(
        find.text('Enter your email address and password.'),
        findsOneWidget,
      );
      expect(api.requests, isEmpty);
      expect(store.isAuthenticated, isFalse);
    });

    testWidgets('shows the API message when the credentials are wrong', (
      tester,
    ) async {
      final api = _Api(
        (_) => _json({'error': 'Invalid email or password.'}, 401),
      );
      final store = await _pumpLogin(tester, api);

      await tester.enterText(_field('Email address'), 'nimali@example.com');
      await tester.enterText(_field('Password'), 'wrong-password');
      await _tapButton(tester, 'Sign in');

      expect(find.text('Invalid email or password.'), findsOneWidget);
      expect(store.isAuthenticated, isFalse);
    });

    testWidgets('trims the email and signs the user in', (tester) async {
      final api = _Api((request) => _json(_commuter));
      final store = await _pumpLogin(tester, api);

      await tester.enterText(_field('Email address'), '  nimali@example.com ');
      await tester.enterText(_field('Password'), 'Password123');
      await _tapButton(tester, 'Sign in');

      final login = api.requests.first;
      expect(login.url.path, '/api/v1/auth/login');
      expect(jsonDecode(login.body), {
        'email': 'nimali@example.com',
        'password': 'Password123',
      });
      expect(store.isAuthenticated, isTrue);
      expect(store.user!.role, 'Commuter');
    });

    testWidgets('shows a friendly message when the API cannot be reached', (
      tester,
    ) async {
      final api = _Api((_) => throw http.ClientException('offline'));
      await _pumpLogin(tester, api);

      await tester.enterText(_field('Email address'), 'nimali@example.com');
      await tester.enterText(_field('Password'), 'Password123');
      await _tapButton(tester, 'Sign in');

      expect(find.textContaining('Unable to reach UPTSLK'), findsOneWidget);
    });
  });

  group('Login to register navigation', () {
    testWidgets('Create account opens the register page and Back returns', (
      tester,
    ) async {
      await _pumpLogin(tester, _Api((_) => _json(_commuter)));

      await _openRegister(tester);
      expect(find.byType(RegisterPage), findsOneWidget);
      expect(
        find.widgetWithText(AuthField, 'Confirm password'),
        findsOneWidget,
      );

      await tester.tap(find.widgetWithText(TextButton, 'Back'));
      await tester.pumpAndSettle();
      expect(find.byType(RegisterPage), findsNothing);
      expect(find.byType(LoginPage), findsOneWidget);
    });
  });

  group('Register form validation', () {
    final cases = <String, Map<String, String?>>{
      'Complete your name, email address, and password.': {'name': ' '},
      'Enter a valid email address.': {'email': 'nimali.example.com'},
      'Use a password with at least 8 characters.': {'password': 'Pass123'},
      'Your passwords do not match.': {'confirm': 'Password124'},
    };
    for (final entry in cases.entries) {
      testWidgets('rejects bad input: ${entry.key}', (tester) async {
        final api = _Api((_) => _json(_commuter));
        final store = await _pumpLogin(tester, api);
        await _openRegister(tester);

        await _fillRegister(
          tester,
          name: entry.value['name'] ?? 'Nimali Silva',
          email: entry.value['email'] ?? 'nimali@example.com',
          password: entry.value['password'] ?? 'Password123',
          confirm: entry.value['confirm'],
        );
        await _tapButton(tester, 'Create account');

        expect(find.text(entry.key), findsOneWidget);
        expect(api.requests, isEmpty);
        expect(store.isAuthenticated, isFalse);
        expect(find.byType(RegisterPage), findsOneWidget);
      });
    }

    testWidgets('accepts a password of exactly 8 characters', (tester) async {
      final api = _Api((_) => _json(_commuter));
      await _pumpLogin(tester, api);
      await _openRegister(tester);

      await _fillRegister(tester, password: 'Pass1234');
      await _tapButton(tester, 'Create account');

      expect(api.requests.first.url.path, '/api/v1/auth/register');
    });
  });

  group('Register submission', () {
    testWidgets('creates the account, signs in and returns from the page', (
      tester,
    ) async {
      final api = _Api((_) => _json(_commuter));
      final store = await _pumpLogin(tester, api);
      await _openRegister(tester);

      await _fillRegister(tester, name: '  Nimali Silva ');
      await _tapButton(tester, 'Create account');

      final register = api.requests.first;
      expect(register.url.path, '/api/v1/auth/register');
      expect(jsonDecode(register.body), {
        'name': 'Nimali Silva',
        'email': 'nimali@example.com',
        'password': 'Password123',
      });
      expect(store.isAuthenticated, isTrue);
      expect(find.byType(RegisterPage), findsNothing);
    });

    testWidgets('keeps the user on the page when the email is taken', (
      tester,
    ) async {
      final api = _Api(
        (_) =>
            _json({'error': 'An account with this email already exists.'}, 409),
      );
      final store = await _pumpLogin(tester, api);
      await _openRegister(tester);

      await _fillRegister(tester);
      await _tapButton(tester, 'Create account');

      expect(
        find.text('An account with this email already exists.'),
        findsOneWidget,
      );
      expect(find.byType(RegisterPage), findsOneWidget);
      expect(store.isAuthenticated, isFalse);
    });
  });
}
