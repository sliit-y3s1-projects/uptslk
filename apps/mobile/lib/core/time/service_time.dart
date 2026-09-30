/// Sri Lankan wall-clock values for display only; compare original instants.
DateTime sriLankaTime(DateTime instant) =>
    instant.toUtc().add(const Duration(hours: 5, minutes: 30));

DateTime serviceToday() {
  final now = sriLankaTime(DateTime.now());
  return DateTime(now.year, now.month, now.day);
}
