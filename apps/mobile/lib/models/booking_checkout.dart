class FareQuote {
  const FareQuote({required this.fare, required this.category});
  final double fare;
  final String category;

  factory FareQuote.fromJson(Map<String, dynamic> json) {
    final passenger = json['passenger'];
    return FareQuote(
      fare: json['fare'] is num
          ? (json['fare'] as num).toDouble()
          : double.tryParse(json['fare']?.toString() ?? '') ?? 0,
      category: passenger is Map
          ? passenger['category']?.toString() ?? 'Passenger'
          : 'Passenger',
    );
  }
}

class CheckoutSession {
  const CheckoutSession({required this.url, required this.orderId});
  final String url;
  final String orderId;
}

class BookingPaymentStatus {
  const BookingPaymentStatus({required this.bookingId, required this.status});
  final String bookingId;
  final String status;

  bool get succeeded => status == 'Succeeded';
  bool get pending => status == 'Initiated' || status == 'Pending';
}
