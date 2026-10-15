import 'dart:async';
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
      hubFactory: () => hub,
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
      hubFactory: () => hub,
    );
    await store.start('token');
    hub.push!(_notification('n-1'));
    hub.push!(_notification('n-2'));

    await store.markAllRead();

    expect(store.unreadCount, 0);
  });

  test('tapping a panel notification asks the app to open alerts', () async {
    final store = NotificationStore(hubFactory: _FakeHub.new);
    final opened = store.openRequests.first;

    store.requestOpen();

    await expectLater(opened, completes);
  });

  test('stop disconnects and clears notifications', () async {
    final hub = _FakeHub();
    final store = NotificationStore(
      api: NotificationApiService(
        client: MockClient((_) async => http.Response(jsonEncode([]), 200)),
      ),
      hubFactory: () => hub,
    );
    await store.start('token');
    hub.push!(_notification('n-1'));

    await store.stop();

    expect(store.items, isEmpty);
    expect(hub.push, isNull);
  });

  test(
    'keeps a live notification that arrives while the list is loading',
    () async {
      final hub = _FakeHub();
      final slowList = Completer<http.Response>();
      var requests = 0;
      final store = NotificationStore(
        api: NotificationApiService(
          client: MockClient(
            (_) async => ++requests == 1
                ? http.Response(jsonEncode([]), 200)
                : slowList.future,
          ),
        ),
        hubFactory: () => hub,
      );
      await store.start('token');

      final refreshing = store.refresh();
      hub.push!(_notification('n-1'));
      slowList.complete(http.Response(jsonEncode([]), 200));
      await refreshing;

      expect(store.items.map((item) => item.id), ['n-1']);
      expect(store.unreadCount, 1);
    },
  );

  test(
    'ignores notifications from the previous account after switching',
    () async {
      final hubs = <_FakeHub>[];
      final store = NotificationStore(
        api: NotificationApiService(
          client: MockClient((_) async => http.Response(jsonEncode([]), 200)),
        ),
        hubFactory: () => (hubs..add(_FakeHub())).last,
      );
      await store.start('first-account');
      final oldPush = hubs.first.push!;

      unawaited(store.stop());
      await store.start('second-account');
      oldPush(_notification('old-account'));

      expect(store.items, isEmpty);
      hubs.last.push!(_notification('new-account'));
      expect(store.items.map((item) => item.id), ['new-account']);
    },
  );
}
