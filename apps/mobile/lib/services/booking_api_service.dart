import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/config/api_config.dart';
import '../models/booking_checkout.dart';
import '../models/mobile_ticket.dart';

class BookingApiException implements Exception {
  const BookingApiException(this.message, {this.statusCode});
  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class BookingApiService {
  BookingApiService({http.Client? client}) : _client = client ?? http.Client();
  final http.Client _client;

  Future<FareQuote> getFareQuote({
    required String token,
    required String tripId,
  }) async {
    final response = await _client.get(
      _uri(
        '/api/v1/fare-rules/quote/me?tripId=${Uri.encodeQueryComponent(tripId)}',
      ),
      headers: _headers(token),
    );
    final body = _validate(
      response,
      response.statusCode == 404
          ? 'The running API does not include the mobile fare service. Restart the API and try again.'
          : 'Unable to calculate the fare.',
    );
    return FareQuote.fromJson(_asMap(body));
  }

  Future<CheckoutSession> startCheckout({
    required String token,
    required String tripId,
    required int passengerCount,
  }) async {
    final response = await _client.post(
      _uri('/api/v1/payments/checkout/me'),
      headers: _headers(token),
      body: jsonEncode({
        'tripId': tripId,
        'passengerCount': passengerCount,
        'useMobileReturnUrl': true,
      }),
    );
    final body = _asMap(_validate(response, 'Unable to start secure payment.'));
    final url = body['url']?.toString() ?? '';
    final orderId = body['orderId']?.toString() ?? '';
    if (url.isEmpty || orderId.isEmpty) {
      throw const BookingApiException(
        'The payment service returned an incomplete checkout session.',
      );
    }
    return CheckoutSession(url: url, orderId: orderId);
  }

  Future<BookingPaymentStatus> getPaymentStatus(String orderId) async {
    final response = await _client.get(
      _uri('/api/v1/payments/orders/${Uri.encodeComponent(orderId)}'),
      headers: const {'Accept': 'application/json'},
    );
    final body = _asMap(
      _validate(response, 'Unable to confirm the payment status.'),
    );
    return BookingPaymentStatus(
      bookingId: body['bookingId']?.toString() ?? '',
      status: body['status']?.toString() ?? '',
    );
  }

  Future<List<MobileTicket>> getMyTickets(String token) async {
    final response = await _client.get(
      _uri('/api/v1/bookings/me'),
      headers: _headers(token),
    );
    final body = _validate(response, 'Unable to load your tickets.');
    if (body is! List) {
      throw const BookingApiException(
        'UPTSLK returned an unexpected response.',
      );
    }
    return body.map((value) => MobileTicket.fromJson(_asMap(value))).toList();
  }

  Uri _uri(String path) {
    final baseUrl = ApiConfig.baseUrl.replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$baseUrl$path');
  }

  Map<String, String> _headers(String token) => {
    'Accept': 'application/json',
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $token',
  };

  dynamic _validate(http.Response response, String fallback) {
    dynamic body;
    try {
      body = response.body.isEmpty
          ? <String, dynamic>{}
          : jsonDecode(response.body);
    } on FormatException {
      throw const BookingApiException(
        'UPTSLK returned an unexpected response.',
      );
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      final error = body is Map
          ? body['error'] ?? body['detail'] ?? body['title']
          : null;
      throw BookingApiException(
        error?.toString() ?? fallback,
        statusCode: response.statusCode,
      );
    }
    return body;
  }

  Map<String, dynamic> _asMap(dynamic value) {
    if (value is Map<String, dynamic>) return value;
    if (value is Map) return Map<String, dynamic>.from(value);
    throw const BookingApiException('UPTSLK returned an unexpected response.');
  }
}
