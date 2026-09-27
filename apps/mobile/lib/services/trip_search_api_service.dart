import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/config/api_config.dart';
import '../models/journey_option.dart';
import '../models/transit_centre.dart';
import '../models/trip_search_result.dart';

class TripSearchException implements Exception {
  const TripSearchException(this.message);
  final String message;

  @override
  String toString() => message;
}

class TripSearchApiService {
  TripSearchApiService({http.Client? client})
    : _client = client ?? http.Client();
  final http.Client _client;

  Future<List<TransitCentre>> getOperatingCentres() async {
    final response = await _get('/api/v1/centres?status=Operating');
    final values = _asList(_decode(response));
    return values
        .map((value) => TransitCentre.fromJson(_asMap(value)))
        .where((centre) => centre.id.isNotEmpty && centre.name.isNotEmpty)
        .toList();
  }

  Future<List<JourneyOption>> getJourneyOptions() async {
    final response = await _get('/api/v1/routes');
    final routes = _asList(_decode(response));
    return routes
        .expand((value) {
          final route = _asMap(value);
          if (route['isActive'] != true) return const <JourneyOption>[];
          final routeId = route['id']?.toString() ?? '';
          final routeNumber = route['routeNumber']?.toString() ?? '';
          final routeName = route['name']?.toString() ?? '';
          final directions = route['directions'];
          if (directions is List && directions.isNotEmpty) {
            return directions
                .map(_asMap)
                .where((direction) => direction['isActive'] == true)
                .map(
                  (direction) => JourneyOption(
                    id: direction['id']?.toString() ?? '',
                    routeId: routeId,
                    routeNumber: routeNumber,
                    name: routeName,
                    origin: _nestedName(direction['startCentre']),
                    destination: _nestedName(direction['endCentre']),
                  ),
                )
                .where(
                  (journey) =>
                      journey.id.isNotEmpty &&
                      journey.origin.isNotEmpty &&
                      journey.destination.isNotEmpty,
                );
          }
          return [
            JourneyOption(
              id: '',
              routeId: routeId,
              routeNumber: routeNumber,
              name: routeName,
              origin: route['origin']?.toString() ?? '',
              destination: route['destination']?.toString() ?? '',
            ),
          ];
        })
        .where((journey) => journey.routeId.isNotEmpty)
        .toList();
  }

  Future<List<TripSearchResult>> searchTrips({
    required DateTime date,
    String? routeId,
    String? directionId,
    String? origin,
    String? destination,
  }) async {
    final dateValue = [
      date.year.toString().padLeft(4, '0'),
      date.month.toString().padLeft(2, '0'),
      date.day.toString().padLeft(2, '0'),
    ].join('-');
    final query = <String, String>{
      'date': dateValue,
      if (routeId != null && routeId.isNotEmpty) 'routeId': routeId,
      if (directionId != null && directionId.isNotEmpty)
        'directionId': directionId,
    };
    final response = await _get(
      Uri(path: '/api/v1/trips', queryParameters: query).toString(),
    );
    final originKey = _key(origin ?? '');
    final destinationKey = _key(destination ?? '');
    return _asList(_decode(response))
        .map((value) => TripSearchResult.fromJson(_asMap(value)))
        .where(
          (trip) =>
              (originKey.isEmpty || _key(trip.origin) == originKey) &&
              (destinationKey.isEmpty ||
                  _key(trip.destination) == destinationKey),
        )
        .toList();
  }

  Future<http.Response> _get(String path) async {
    try {
      final baseUrl = ApiConfig.baseUrl.replaceFirst(RegExp(r'/+$'), '');
      final response = await _client.get(
        Uri.parse('$baseUrl$path'),
        headers: const {'Accept': 'application/json'},
      );
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw TripSearchException(_errorMessage(response));
      }
      return response;
    } on TripSearchException {
      rethrow;
    } on http.ClientException {
      throw const TripSearchException(
        'Unable to reach UPTSLK. Check your connection and try again.',
      );
    }
  }

  dynamic _decode(http.Response response) {
    if (response.body.isEmpty) return const [];
    try {
      return jsonDecode(response.body);
    } on FormatException {
      throw const TripSearchException(
        'UPTSLK returned an unexpected response.',
      );
    }
  }

  List<dynamic> _asList(dynamic value) {
    if (value is List) return value;
    throw const TripSearchException('UPTSLK returned an unexpected response.');
  }

  Map<String, dynamic> _asMap(dynamic value) {
    if (value is Map<String, dynamic>) return value;
    if (value is Map) return Map<String, dynamic>.from(value);
    throw const TripSearchException('UPTSLK returned an unexpected response.');
  }

  String _errorMessage(http.Response response) {
    try {
      final body = _decode(response);
      if (body is Map) {
        final value = body['error'] ?? body['detail'] ?? body['title'];
        if (value != null && value.toString().trim().isNotEmpty) {
          return value.toString();
        }
      }
    } on TripSearchException {
      // Use the HTTP fallback below for malformed error bodies.
    }
    return 'Unable to load departures right now.';
  }

  String _key(String value) => value.trim().toLowerCase();

  String _nestedName(dynamic value) =>
      value is Map ? value['name']?.toString() ?? '' : '';
}
