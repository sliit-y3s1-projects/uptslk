import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/services/trip_search_api_service.dart';

http.Response _json(Object body, [int status = 200]) => http.Response(
  jsonEncode(body),
  status,
  headers: {'content-type': 'application/json'},
);

Map<String, Object?> _trip({
  required String id,
  String status = 'Scheduled',
  Duration from = const Duration(hours: 2),
  String origin = 'Kadawatha Centre',
  String destination = 'Makumbura Centre',
  Object available = 12,
}) => {
  'id': id,
  'routeId': 'route-1',
  'routeDirectionId': 'dir-1',
  'routeNumber': 'EX-KM-01',
  'routeName': 'Kadawatha - Makumbura Express',
  'origin': origin,
  'destination': destination,
  'scheduledTime': DateTime.now().toUtc().add(from).toIso8601String(),
  'status': status,
  'bay': 'B1',
  'available': available,
  'capacity': 40,
};

void main() {
  group('TripSearchApiService centres', () {
    test('asks only for operating centres and drops incomplete rows', () async {
      final service = TripSearchApiService(
        client: MockClient((request) async {
          expect(request.url.path, '/api/v1/centres');
          expect(request.url.queryParameters, {'status': 'Operating'});
          return _json([
            {
              'id': 'a',
              'name': 'Kadawatha Centre',
              'city': 'Kadawatha',
              'status': 'Operating',
            },
            {'id': '', 'name': 'No id'},
            {'id': 'c', 'name': ''},
          ]);
        }),
      );

      final centres = await service.getOperatingCentres();

      expect(centres.map((centre) => centre.id), ['a']);
      expect(centres.single.city, 'Kadawatha');
    });
  });

  group('TripSearchApiService journey options', () {
    test(
      'lists active directions of active routes, and routes without directions',
      () async {
        final service = TripSearchApiService(
          client: MockClient(
            (_) async => _json([
              {
                'id': 'route-1',
                'routeNumber': 'EX-KM-01',
                'name': 'Express',
                'isActive': true,
                'directions': [
                  {
                    'id': 'dir-out',
                    'isActive': true,
                    'startCentre': {'name': 'Kadawatha Centre'},
                    'endCentre': {'name': 'Makumbura Centre'},
                  },
                  {
                    'id': 'dir-closed',
                    'isActive': false,
                    'startCentre': {'name': 'Makumbura Centre'},
                    'endCentre': {'name': 'Kadawatha Centre'},
                  },
                ],
              },
              {
                'id': 'route-2',
                'routeNumber': '138',
                'name': 'City',
                'isActive': true,
                'origin': 'Pettah',
                'destination': 'Maharagama',
              },
              {
                'id': 'route-3',
                'routeNumber': 'OLD',
                'name': 'Archived',
                'isActive': false,
              },
            ]),
          ),
        );

        final options = await service.getJourneyOptions();

        expect(options.map((option) => option.id), ['dir-out', '']);
        expect(options.first.origin, 'Kadawatha Centre');
        expect(options.first.destination, 'Makumbura Centre');
        expect(options.last.routeNumber, '138');
        expect(options.last.origin, 'Pettah');
      },
    );
  });

  group('TripSearchApiService trip search', () {
    test(
      'sends the date, route and direction, and asks for bookable trips only',
      () async {
        final service = TripSearchApiService(
          client: MockClient((request) async {
            expect(request.url.path, '/api/v1/trips');
            expect(request.url.queryParameters, {
              'bookableOnly': 'true',
              'date': '2026-03-05',
              'routeId': 'route-1',
              'directionId': 'dir-1',
            });
            return _json([]);
          }),
        );

        await service.searchTrips(
          date: DateTime(2026, 3, 5),
          routeId: 'route-1',
          directionId: 'dir-1',
        );
      },
    );

    test('leaves out empty route and direction filters', () async {
      final service = TripSearchApiService(
        client: MockClient((request) async {
          expect(
            request.url.queryParameters.keys,
            unorderedEquals(['bookableOnly', 'date']),
          );
          return _json([]);
        }),
      );

      await service.searchTrips(
        date: DateTime(2026, 12, 31),
        routeId: '',
        directionId: '',
      );
    });

    test('hides departed, finished and cancelled trips', () async {
      final service = TripSearchApiService(
        client: MockClient(
          (_) async => _json([
            _trip(id: 'scheduled'),
            _trip(id: 'boarding', status: 'Boarding'),
            _trip(id: 'departed', from: const Duration(minutes: -5)),
            _trip(id: 'dispatched', status: 'Dispatched'),
            _trip(id: 'cancelled', status: 'Cancelled'),
          ]),
        ),
      );

      final trips = await service.searchTrips(date: DateTime.now());

      expect(trips.map((trip) => trip.id), ['scheduled', 'boarding']);
    });

    test('matches origin and destination ignoring case and spaces', () async {
      final service = TripSearchApiService(
        client: MockClient(
          (_) async => _json([
            _trip(id: 'match'),
            _trip(
              id: 'other-way',
              origin: 'Makumbura Centre',
              destination: 'Kadawatha Centre',
            ),
          ]),
        ),
      );

      final trips = await service.searchTrips(
        date: DateTime.now(),
        origin: '  kadawatha centre ',
        destination: 'MAKUMBURA CENTRE',
      );

      expect(trips.map((trip) => trip.id), ['match']);
    });

    test('a full trip is shown but cannot be booked', () async {
      final service = TripSearchApiService(
        client: MockClient(
          (_) async => _json([
            _trip(id: 'full', available: 0),
            _trip(id: 'seats', available: '3'),
          ]),
        ),
      );

      final trips = await service.searchTrips(date: DateTime.now());

      expect(trips.firstWhere((trip) => trip.id == 'full').isBookable, isFalse);
      final withSeats = trips.firstWhere((trip) => trip.id == 'seats');
      expect(withSeats.available, 3);
      expect(withSeats.isBookable, isTrue);
    });
  });

  group('TripSearchApiService errors', () {
    test('uses the error message the API sends', () async {
      final service = TripSearchApiService(
        client: MockClient(
          (_) async => _json({'error': 'Route not found.'}, 404),
        ),
      );

      expect(
        service.searchTrips(date: DateTime.now()),
        throwsA(
          isA<TripSearchException>().having(
            (e) => e.message,
            'message',
            'Route not found.',
          ),
        ),
      );
    });

    test(
      'falls back to a general message for an error without a body',
      () async {
        final service = TripSearchApiService(
          client: MockClient((_) async => http.Response('', 500)),
        );

        expect(
          service.getOperatingCentres(),
          throwsA(
            isA<TripSearchException>().having(
              (e) => e.message,
              'message',
              'Unable to load departures right now.',
            ),
          ),
        );
      },
    );

    test('explains when the phone cannot reach the API', () async {
      final service = TripSearchApiService(
        client: MockClient((_) async => throw http.ClientException('offline')),
      );

      expect(
        service.getJourneyOptions(),
        throwsA(
          isA<TripSearchException>().having(
            (e) => e.message,
            'message',
            contains('Unable to reach UPTSLK'),
          ),
        ),
      );
    });

    test('rejects a response that is not JSON or not a list', () async {
      final notJson = TripSearchApiService(
        client: MockClient((_) async => http.Response('<html>', 200)),
      );
      final notList = TripSearchApiService(
        client: MockClient((_) async => _json({'items': []})),
      );

      expect(
        notJson.getOperatingCentres(),
        throwsA(isA<TripSearchException>()),
      );
      expect(
        notList.getOperatingCentres(),
        throwsA(isA<TripSearchException>()),
      );
    });
  });
}
