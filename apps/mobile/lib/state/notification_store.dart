import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/app_notification.dart';
import '../services/auth_api_service.dart';
import '../services/notification_api_service.dart';
import '../services/notification_hub_client.dart';

class NotificationStore extends ChangeNotifier {
  NotificationStore({
    NotificationApiService? api,
    NotificationHubClient Function()? hubFactory,
  }) : _api = api ?? NotificationApiService(),
       _hubFactory = hubFactory ?? NotificationHubClient.new;

  static const _retryDelay = Duration(seconds: 30);

  final NotificationApiService _api;
  final NotificationHubClient Function() _hubFactory;
  final _incoming = StreamController<AppNotification>.broadcast();
  final _openRequests = StreamController<void>.broadcast();

  /// Increases on every start and stop, so callbacks from an older session are ignored.
  int _session = 0;
  String? _token;
  NotificationHubClient? _hub;
  Timer? _retryTimer;
  List<AppNotification> _items = const [];

  /// Notifications received live while a list request is in flight.
  final List<List<AppNotification>> _liveDuringRefresh = [];

  List<AppNotification> get items => _items;
  int get unreadCount => _items.where((item) => !item.isRead).length;

  /// Notifications pushed live by the server while the app is connected.
  Stream<AppNotification> get incoming => _incoming.stream;

  /// Fires when the user taps a notification in the phone's notification panel.
  Stream<void> get openRequests => _openRequests.stream;

  void requestOpen() => _openRequests.add(null);

  Future<void> start(String token) async {
    if (_token == token) return;
    final previousHub = _endSession();
    _token = token;
    final session = _session;
    final hub = _hub = _hubFactory();
    await previousHub?.disconnect();
    await _refresh(session);
    if (session == _session) await _connect(session, token, hub);
  }

  Future<void> stop() async {
    final hub = _endSession();
    _token = null;
    await hub?.disconnect();
  }

  Future<void> refresh() => _refresh(_session);

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

  NotificationHubClient? _endSession() {
    _session++;
    _retryTimer?.cancel();
    _liveDuringRefresh.clear();
    final hub = _hub;
    _hub = null;
    if (_items.isNotEmpty) {
      _items = const [];
      notifyListeners();
    }
    return hub;
  }

  Future<void> _refresh(int session) async {
    final token = _token;
    if (token == null) return;
    final live = <AppNotification>[];
    _liveDuringRefresh.add(live);
    try {
      final items = await _api.list(token);
      if (session != _session) return;
      final listed = {for (final item in items) item.id};
      _items = [
        ...live.reversed.where((item) => !listed.contains(item.id)),
        ...items,
      ];
      notifyListeners();
    } on AuthApiException {
      // Keep the last loaded list; the next refresh or reconnect retries.
    } finally {
      _liveDuringRefresh.remove(live);
    }
  }

  void _replaceAll(bool Function(AppNotification item) test) {
    final now = DateTime.now();
    _items = [
      for (final item in _items) test(item) ? item.markedRead(now) : item,
    ];
    notifyListeners();
  }

  Future<void> _connect(
    int session,
    String token,
    NotificationHubClient hub,
  ) async {
    _retryTimer?.cancel();
    try {
      await hub.connect(
        token: token,
        onNotification: (notification) {
          if (session == _session) _receive(notification);
        },
        onReconnected: () {
          if (session == _session) _refresh(session);
        },
        onClosed: () => _scheduleReconnect(session, token, hub),
      );
      if (session != _session) await hub.disconnect();
    } catch (_) {
      _scheduleReconnect(session, token, hub);
    }
  }

  void _scheduleReconnect(
    int session,
    String token,
    NotificationHubClient hub,
  ) {
    if (session != _session) return;
    _retryTimer?.cancel();
    _retryTimer = Timer(_retryDelay, () async {
      if (session != _session) return;
      await _refresh(session);
      if (session == _session) await _connect(session, token, hub);
    });
  }

  void _receive(AppNotification notification) {
    for (final live in _liveDuringRefresh) {
      live.add(notification);
    }
    _items = [
      notification,
      ..._items.where((item) => item.id != notification.id),
    ];
    notifyListeners();
    _incoming.add(notification);
  }

  @override
  void dispose() {
    final hub = _endSession();
    _incoming.close();
    _openRequests.close();
    unawaited(hub?.disconnect());
    super.dispose();
  }
}
