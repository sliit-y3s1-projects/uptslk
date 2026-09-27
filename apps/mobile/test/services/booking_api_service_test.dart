import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/services/booking_api_service.dart';

void main() {
  group('BookingApiService', () {
    test('loads the current passenger fare quote', () async {
      final service = BookingApiService(
        client: MockClient((request) async {
          expect(request.method, 'GET');
          expect(request.url.path, '/api/v1/fare-rules/quote/me');
          expect(request.url.queryParameters['tripId'], 'trip-1');
          expect(request.headers['authorization'], 'Bearer token-1');
          return http.Response(
            jsonEncode({
              'fare': 180,
              'passenger': {'category': 'Regular'},
            }),
            200,
          );
        }),
      );

      final quote = await service.getFareQuote(
        token: 'token-1',
        tripId: 'trip-1',
      );

      expect(quote.fare, 180);
      expect(quote.category, 'Regular');
    });

    test(
      'explains when the running API is missing the fare endpoint',
      () async {
        final service = BookingApiService(
          client: MockClient((_) async => http.Response('', 404)),
        );

        expect(
          () => service.getFareQuote(token: 'token-1', tripId: 'trip-1'),
          throwsA(
            isA<BookingApiException>().having(
              (error) => error.message,
              'message',
              contains('Restart the API'),
            ),
          ),
        );
      },
    );

    test(
      'starts checkout and retains the order used for reconciliation',
      () async {
        final service = BookingApiService(
          client: MockClient((request) async {
            expect(request.method, 'POST');
            expect(request.url.path, '/api/v1/payments/checkout/me');
          expect(jsonDecode(request.body), {
            'tripId': 'trip-1',
            'passengerCount': 2,
            'useMobileReturnUrl': true,
          });
            return http.Response(
              jsonEncode({
                'url': 'https://checkout.stripe.test/session',
                'orderId': 'UPTS-ORDER-1',
              }),
              200,
            );
          }),
        );

        final checkout = await service.startCheckout(
          token: 'token-1',
          tripId: 'trip-1',
          passengerCount: 2,
        );

        expect(checkout.url, 'https://checkout.stripe.test/session');
        expect(checkout.orderId, 'UPTS-ORDER-1');
      },
    );

    test('loads API-backed tickets and maps the QR code', () async {
      final service = BookingApiService(
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/bookings/me');
          return http.Response(
            jsonEncode([
              {
                'id': 'booking-1',
                'status': 'Confirmed',
                'passengerCount': 2,
                'fare': 360,
                'qrCode': 'BKG-BOARDING-1',
                'createdAt': '2026-09-27T06:00:00Z',
                'tripTime': '2026-09-28T06:30:00Z',
                'route': 'EX-KM-01',
                'routeName': 'Kadawatha to Makumbura',
              },
            ]),
            200,
          );
        }),
      );

      final tickets = await service.getMyTickets('token-1');

      expect(tickets, hasLength(1));
      expect(tickets.single.qrCode, 'BKG-BOARDING-1');
      expect(tickets.single.isActive, isTrue);
      expect(tickets.single.passengerCount, 2);
    });
  });
}
