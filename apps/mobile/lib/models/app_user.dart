class AppUser {
  final String id;
  final String name;
  final String email;
  final String role;
  final String? centreId;
  final String? homeLocation;
  final String? nicNumber;
  final String? gender;
  final String? profilePhotoUrl;
  final String? nicVerificationStatus;

  const AppUser({
    required this.id,
    required this.name,
    required this.email,
    required this.role,
    this.centreId,
    this.homeLocation,
    this.nicNumber,
    this.gender,
    this.profilePhotoUrl,
    this.nicVerificationStatus,
  });

  factory AppUser.fromJson(Map<String, dynamic> json) {
    return AppUser(
      id: (json['userId'] ?? json['id'] ?? '').toString(),
      name: (json['name'] ?? '').toString(),
      email: (json['email'] ?? '').toString(),
      role: (json['role'] ?? '').toString(),
      centreId: json['centreId']?.toString(),
      homeLocation: json['homeLocation']?.toString(),
      nicNumber: json['nicNumber']?.toString(),
      gender: normalizeGender(json['gender']?.toString()),
      profilePhotoUrl: json['profilePhotoUrl']?.toString(),
      nicVerificationStatus: json['nicVerificationStatus']?.toString(),
    );
  }

  AppUser copyWith({
    String? name,
    String? homeLocation,
    String? nicNumber,
    String? gender,
    String? profilePhotoUrl,
    String? nicVerificationStatus,
  }) {
    return AppUser(
      id: id,
      name: name ?? this.name,
      email: email,
      role: role,
      centreId: centreId,
      homeLocation: homeLocation ?? this.homeLocation,
      nicNumber: nicNumber ?? this.nicNumber,
      gender: gender ?? this.gender,
      profilePhotoUrl: profilePhotoUrl ?? this.profilePhotoUrl,
      nicVerificationStatus:
          nicVerificationStatus ?? this.nicVerificationStatus,
    );
  }

  String get roleLabel {
    if (role.isEmpty) return 'Account';
    return role.replaceAllMapped(RegExp(r'(?<=[a-z])(?=[A-Z])'), (_) => ' ');
  }
}

/// Keep server-provided values intact while standardising known labels.
String? normalizeGender(String? value) {
  final trimmed = value?.trim();
  if (trimmed == null || trimmed.isEmpty) return null;
  return switch (trimmed.toLowerCase()) {
    'male' => 'Male',
    'female' => 'Female',
    'other' => 'Other',
    'prefer not to say' => 'Prefer not to say',
    _ => trimmed,
  };
}
