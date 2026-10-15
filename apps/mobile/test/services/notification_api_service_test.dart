import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/services/notification_api_service.dart';

void main() {
  group('NotificationApiService', () {
    test('loads the signed-in user\'s notifications', () async {
      final service = NotificationApiService(
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/notifications');
          expect(request.headers['authorization'], 'Bearer token');
          return http.Response(
            jsonEncode([
              {
                'id': 'n-1',
                'type': 'TripUpdated',
                'title': 'Route 138 trip changed',
                'message': 'The departure bay is now B3.',
                'tripId': 'trip-1',
                'bookingId': null,
                'readAt': null,
                'createdAt': '2026-10-15T03:00:00Z',
              },
            ]),
            200,
          );
        }),
      );

      final notifications = await service.list('token');

      expect(notifications.single.type, 'TripUpdated');
      expect(notifications.single.tripId, 'trip-1');
      expect(notifications.single.isRead, isFalse);
    });

    test('marks every notification read', () async {
      final service = NotificationApiService(
        client: MockClient((request) async {
          expect(request.method, 'POST');
          expect(request.url.path, '/api/v1/notifications/read-all');
          return http.Response('', 204);
        }),
      );

      await service.markAllRead('token');
    });
  });
}
