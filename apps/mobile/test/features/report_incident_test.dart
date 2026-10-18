import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/core/theme/app_theme.dart';
import 'package:mobile/features/driver/report_incident_page.dart';
import 'package:mobile/models/driver_assignment.dart';
import 'package:mobile/services/auth_api_service.dart';
import 'package:mobile/services/driver_api_service.dart';
import 'package:mobile/services/session_storage.dart';
import 'package:mobile/state/auth_store.dart';

class _MemoryStorage implements SessionStorage {
  _MemoryStorage(this.token);

  String? token;

  @override
  Future<String?> readToken() async => token;

  @override
  Future<void> saveToken(String value) async => token = value;

  @override
  Future<void> clearToken() async => token = null;
}

final _duty = DriverAssignment(
  id: 'trip-1',
  routeId: 'route-1',
  routeNumber: 'EX-KM-01',
  routeName: 'Kadawatha - Makumbura Express',
  origin: 'Kadawatha Centre',
  destination: 'Makumbura Centre',
  scheduledTime: DateTime.utc(2026, 10, 18, 4),
  status: 'Scheduled',
  vehiclePlate: 'NB-1234',
  vehicleModel: 'Lanka Ashok Leyland',
  capacity: 40,
  bayCode: 'B1',
  bayName: 'Bay 1',
  passengerCount: 12,
);

/// Opens the report page from a launcher so the test can see what it returns.
Future<({List<http.Request> requests, List<Object?> results})> _pump(
  WidgetTester tester, {
  String? token = 'driver-token',
  required http.Response Function(http.Request request) respond,
}) async {
  tester.view.physicalSize = const Size(430, 1200);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final requests = <http.Request>[];
  final results = <Object?>[];
  final auth = AuthStore(
    AuthApiService(
      client: MockClient(
        (_) async => http.Response(
          jsonEncode({'id': 'driver', 'name': 'Driver', 'role': 'Driver'}),
          200,
        ),
      ),
    ),
    sessionStorage: _MemoryStorage(token),
  );
  addTearDown(auth.dispose);
  await auth.restoreSession();
  final service = DriverApiService(
    client: MockClient((request) async {
      requests.add(request);
      return respond(request);
    }),
  );

  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.lightTheme,
      home: Builder(
        builder: (context) => TextButton(
          onPressed: () async => results.add(
            await Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => ReportIncidentPage(
                  duty: _duty,
                  authStore: auth,
                  service: service,
                ),
              ),
            ),
          ),
          child: const Text('Open'),
        ),
      ),
    ),
  );
  await tester.tap(find.text('Open'));
  await tester.pumpAndSettle();
  return (requests: requests, results: results);
}

Finder _hint(String hint) => find.byWidgetPredicate(
  (widget) => widget is TextField && widget.decoration?.hintText == hint,
);

Future<void> _send(WidgetTester tester) async {
  await tester.ensureVisible(find.text('Send report'));
  await tester.tap(find.text('Send report'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('shows which trip the report is for', (tester) async {
    await _pump(tester, respond: (_) => http.Response('', 204));

    expect(
      find.text('EX-KM-01 · Kadawatha Centre to Makumbura Centre'),
      findsOneWidget,
    );
    expect(find.text('Report incident'), findsOneWidget);
  });

  testWidgets('needs a summary and a description before sending', (
    tester,
  ) async {
    final run = await _pump(tester, respond: (_) => http.Response('', 204));

    await tester.enterText(_hint('Briefly describe the issue'), 'Flat tyre');
    await tester.enterText(
      _hint('What happened and where is the bus now?'),
      '   ',
    );
    await _send(tester);

    expect(
      find.text('Enter a summary and describe what happened.'),
      findsOneWidget,
    );
    expect(run.requests, isEmpty);
  });

  testWidgets('sends the chosen type and severity, then closes with success', (
    tester,
  ) async {
    final run = await _pump(tester, respond: (_) => http.Response('', 201));

    await tester.tap(find.widgetWithText(ChoiceChip, 'Safety'));
    await tester.tap(find.widgetWithText(ChoiceChip, 'High'));
    await tester.pump();
    await tester.enterText(
      _hint('Briefly describe the issue'),
      '  Smoke from engine ',
    );
    await tester.enterText(
      _hint('What happened and where is the bus now?'),
      'Stopped near Kiribathgoda junction.',
    );
    await _send(tester);

    final request = run.requests.single;
    expect(request.method, 'POST');
    expect(request.url.path, '/api/v1/drivers/me/trips/trip-1/incidents');
    expect(request.headers['authorization'], 'Bearer driver-token');
    expect(jsonDecode(request.body), {
      'type': 'Safety',
      'severity': 'High',
      'title': 'Smoke from engine',
      'description': 'Stopped near Kiribathgoda junction.',
    });
    expect(run.results, [true]);
    expect(find.byType(ReportIncidentPage), findsNothing);
  });

  testWidgets('defaults to a medium breakdown report', (tester) async {
    final run = await _pump(tester, respond: (_) => http.Response('', 201));

    await tester.enterText(
      _hint('Briefly describe the issue'),
      'Engine stopped',
    );
    await tester.enterText(
      _hint('What happened and where is the bus now?'),
      'At the depot.',
    );
    await _send(tester);

    final body = jsonDecode(run.requests.single.body) as Map<String, dynamic>;
    expect(body['type'], 'Breakdown');
    expect(body['severity'], 'Medium');
  });

  testWidgets(
    'shows the API reason and stays open when the report is refused',
    (tester) async {
      final run = await _pump(
        tester,
        respond: (_) => http.Response(
          jsonEncode({
            'error': 'Incidents cannot be reported for a completed trip.',
          }),
          400,
          headers: {'content-type': 'application/json'},
        ),
      );

      await tester.enterText(_hint('Briefly describe the issue'), 'Late');
      await tester.enterText(
        _hint('What happened and where is the bus now?'),
        'Traffic.',
      );
      await _send(tester);

      expect(
        find.text('Incidents cannot be reported for a completed trip.'),
        findsOneWidget,
      );
      expect(find.byType(ReportIncidentPage), findsOneWidget);
      expect(run.results, isEmpty);
    },
  );

  testWidgets('asks the driver to sign in again when the session is gone', (
    tester,
  ) async {
    final run = await _pump(
      tester,
      token: null,
      respond: (_) => http.Response('', 201),
    );

    await tester.enterText(_hint('Briefly describe the issue'), 'Late');
    await tester.enterText(
      _hint('What happened and where is the bus now?'),
      'Traffic.',
    );
    await _send(tester);

    expect(find.text('Your session has ended. Sign in again.'), findsOneWidget);
    expect(run.requests, isEmpty);
  });
}
