import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/app_notification.dart';
import '../services/auth_api_service.dart';
import '../services/notification_api_service.dart';
import '../services/notification_hub_client.dart';

class NotificationStore extends ChangeNotifier {
  NotificationStore({NotificationApiService? api, NotificationHubClient? hub})
    : _api = api ?? NotificationApiService(),
      _hub = hub ?? NotificationHubClient();

  static const _retryDelay = Duration(seconds: 30);

  final NotificationApiService _api;
  final NotificationHubClient _hub;
  final _incoming = StreamController<AppNotification>.broadcast();
  final _openRequests = StreamController<void>.broadcast();

  String? _token;
  Timer? _retryTimer;
  List<AppNotification> _items = const [];

  List<AppNotification> get items => _items;
  int get unreadCount => _items.where((item) => !item.isRead).length;

  /// Notifications pushed live by the server while the app is connected.
  Stream<AppNotification> get incoming => _incoming.stream;

  /// Fires when the user taps a notification in the phone's notification panel.
  Stream<void> get openRequests => _openRequests.stream;

  void requestOpen() => _openRequests.add(null);

  Future<void> start(String token) async {
    if (_token == token) return;
    _token = token;
    await refresh();
    await _connect(token);
  }

  Future<void> stop() async {
    _token = null;
    _retryTimer?.cancel();
    if (_items.isNotEmpty) {
      _items = const [];
      notifyListeners();
    }
    await _hub.disconnect();
  }

  Future<void> refresh() async {
    final token = _token;
    if (token == null) return;
    try {
      final items = await _api.list(token);
      if (_token != token) return;
      _items = items;
      notifyListeners();
    } on AuthApiException {
      // Keep the last loaded list; the next refresh or reconnect retries.
    }
  }

  Future<void> markRead(AppNotification notification) async {
    final token = _token;
    if (token == null || notification.isRead) return;
    _replaceAll((item) => item.id == notification.id);
    try {
      await _api.markRead(token, notification.id);
    } on AuthApiException {
      // Read state is cosmetic; the server copy is corrected on next refresh.
    }
  }

  Future<void> markAllRead() async {
    final token = _token;
    if (token == null || unreadCount == 0) return;
    _replaceAll((item) => !item.isRead);
    try {
      await _api.markAllRead(token);
    } on AuthApiException {
      // See markRead.
    }
  }

  void _replaceAll(bool Function(AppNotification item) test) {
    final now = DateTime.now();
    _items = [
      for (final item in _items) test(item) ? item.markedRead(now) : item,
    ];
    notifyListeners();
  }

  Future<void> _connect(String token) async {
    _retryTimer?.cancel();
    try {
      await _hub.connect(
        token: token,
        onNotification: _receive,
        onReconnected: refresh,
        onClosed: _scheduleReconnect,
      );
      if (_token != token) await _hub.disconnect();
    } catch (_) {
      _scheduleReconnect();
    }
  }

  void _scheduleReconnect() {
    final token = _token;
    if (token == null) return;
    _retryTimer?.cancel();
    _retryTimer = Timer(_retryDelay, () async {
      if (_token != token) return;
      await refresh();
      await _connect(token);
    });
  }

  void _receive(AppNotification notification) {
    _items = [
      notification,
      ..._items.where((item) => item.id != notification.id),
    ];
    notifyListeners();
    _incoming.add(notification);
  }

  @override
  void dispose() {
    _retryTimer?.cancel();
    _incoming.close();
    _openRequests.close();
    unawaited(_hub.disconnect());
    super.dispose();
  }
}
