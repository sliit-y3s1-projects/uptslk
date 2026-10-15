import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile/models/app_notification.dart';
import 'package:mobile/services/notification_api_service.dart';
import 'package:mobile/services/notification_hub_client.dart';
import 'package:mobile/state/notification_store.dart';

class _FakeHub extends NotificationHubClient {
  void Function(AppNotification notification)? push;

  @override
  Future<void> connect({
    required String token,
    required void Function(AppNotification notification) onNotification,
    required void Function() onReconnected,
    required void Function() onClosed,
  }) async {
    push = onNotification;
  }

  @override
  Future<void> disconnect() async => push = null;
}

AppNotification _notification(String id) => AppNotification(
  id: id,
  type: 'TripAssigned',
  title: 'New trip assigned',
  message: 'Route 138',
  createdAt: DateTime.utc(2026, 10, 15),
);

void main() {
  test('adds live notifications to the top and streams them', () async {
    final hub = _FakeHub();
    final store = NotificationStore(
      api: NotificationApiService(
        client: MockClient((_) async => http.Response(jsonEncode([]), 200)),
      ),
      hub: hub,
    );
    await store.start('token');
    final incoming = store.incoming.first;

    hub.push!(_notification('n-1'));

    expect((await incoming).id, 'n-1');
    expect(store.items.single.id, 'n-1');
    expect(store.unreadCount, 1);
  });

  test('mark all read clears the unread count', () async {
    final hub = _FakeHub();
    final store = NotificationStore(
      api: NotificationApiService(
        client: MockClient(
          (request) async => request.method == 'GET'
              ? http.Response(jsonEncode([]), 200)
              : http.Response('', 204),
        ),
      ),
      hub: hub,
    );
    await store.start('token');
    hub.push!(_notification('n-1'));
    hub.push!(_notification('n-2'));

    await store.markAllRead();

    expect(store.unreadCount, 0);
  });

  test('stop disconnects and clears notifications', () async {
    final hub = _FakeHub();
    final store = NotificationStore(
      api: NotificationApiService(
        client: MockClient((_) async => http.Response(jsonEncode([]), 200)),
      ),
      hub: hub,
    );
    await store.start('token');
    hub.push!(_notification('n-1'));

    await store.stop();

    expect(store.items, isEmpty);
    expect(hub.push, isNull);
  });
}
