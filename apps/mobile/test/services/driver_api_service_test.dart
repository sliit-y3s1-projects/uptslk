import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/models/driver_assignment.dart';
import 'package:mobile/services/driver_api_service.dart';

void main() {
  group('DriverApiService', () {
    test('requests the seven-day duty window', () async {
      final service = DriverApiService(
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/drivers/me/trips');
          expect(request.url.queryParameters, {
            'fromDate': '2026-09-30',
            'toDate': '2026-10-06',
          });
          expect(request.headers['authorization'], 'Bearer token');
          return http.Response('[]', 200);
        }),
      );

      final duties = await service.getDuties(
        token: 'token',
        fromDate: DateTime(2026, 9, 30),
        toDate: DateTime(2026, 10, 6),
      );
      expect(duties, isEmpty);
    });

    test('sends a reason when marking a trip delayed', () async {
      final service = DriverApiService(
        client: MockClient((request) async {
          expect(request.method, 'PATCH');
          expect(request.url.path, '/api/v1/drivers/me/trips/trip-1/status');
          expect(jsonDecode(request.body), {
            'status': 'Delayed',
            'note': 'Road blocked',
          });
          return http.Response('', 204);
        }),
      );

      await service.updateDutyStatus(
        token: 'token',
        tripId: 'trip-1',
        status: 'Delayed',
        note: 'Road blocked',
      );
    });

    test('reports an incident against the assigned trip', () async {
      final service = DriverApiService(
        client: MockClient((request) async {
          expect(request.method, 'POST');
          expect(request.url.path, '/api/v1/drivers/me/trips/trip-1/incidents');
          expect(jsonDecode(request.body), {
            'type': 'Breakdown',
            'severity': 'High',
            'title': 'Engine stopped',
            'description': 'Stopped at the next bay',
          });
          return http.Response('{"id":"incident-1"}', 201);
        }),
      );

      await service.reportIncident(
        token: 'token',
        tripId: 'trip-1',
        type: 'Breakdown',
        severity: 'High',
        title: 'Engine stopped',
        description: 'Stopped at the next bay',
      );
    });
  });

  test('groups duties by Sri Lankan service date', () {
    final duty = DriverAssignment.fromJson({
      'scheduledTime': '2026-09-30T20:00:00Z',
    });
    expect(duty.serviceDate, DateTime(2026, 10, 1));
  });
}
