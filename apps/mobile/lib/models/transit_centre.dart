class TransitCentre {
  const TransitCentre({
    required this.id,
    required this.name,
    required this.city,
    required this.status,
  });

  final String id;
  final String name;
  final String city;
  final String status;

  factory TransitCentre.fromJson(Map<String, dynamic> json) => TransitCentre(
    id: json['id']?.toString() ?? '',
    name: json['name']?.toString() ?? '',
    city: json['city']?.toString() ?? '',
    status: json['status']?.toString() ?? '',
  );
}
