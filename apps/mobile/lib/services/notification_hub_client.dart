import 'dart:async';

import 'package:signalr_netcore/signalr_client.dart';

import '../core/config/api_config.dart';
import '../models/app_notification.dart';

/// Live connection to the API's SignalR notification hub.
class NotificationHubClient {
  HubConnection? _connection;

  Future<void> connect({
    required String token,
    required void Function(AppNotification notification) onNotification,
    required void Function() onReconnected,
    required void Function() onClosed,
  }) async {
    await disconnect();
    final baseUrl = ApiConfig.baseUrl.replaceFirst(RegExp(r'/+$'), '');
    final connection = HubConnectionBuilder()
        .withUrl(
          '$baseUrl/hubs/notifications',
          options: HttpConnectionOptions(accessTokenFactory: () async => token),
        )
        .withAutomaticReconnect()
        .build();

    connection.on('notification', (arguments) {
      final payload = arguments?.firstOrNull;
      if (payload is Map) {
        onNotification(
          AppNotification.fromJson(Map<String, dynamic>.from(payload)),
        );
      }
    });
    connection.onreconnected(({connectionId}) => onReconnected());
    connection.onclose(({error}) {
      if (identical(_connection, connection)) onClosed();
    });

    _connection = connection;
    await connection.start();
  }

  Future<void> disconnect() async {
    final connection = _connection;
    _connection = null;
    if (connection != null) await connection.stop();
  }
}
