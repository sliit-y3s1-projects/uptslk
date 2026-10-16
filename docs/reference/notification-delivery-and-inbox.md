# Notification delivery and inbox behavior

**Audience:** mobile integrators and operations-workflow reviewers.

**Baseline:** `main` commit `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16. This document separates saved notifications, live delivery, and read state.

## General inbox contract

The [NotificationsController](../../apps/backend/api/Controllers/NotificationsController.cs) permits Commuter and Driver roles under `/api/v1/notifications`:

| Request | Result |
| --- | --- |
| `GET` | Latest 50 notifications for the authenticated user, newest first. |
| `POST /{notificationId}/read` | Returns 204 for an owned record, preserving an existing ReadAt; returns Not Found for an unavailable/other user's ID. |
| `POST /read-all` | Marks all unread records for the caller, including records outside the latest-50 list. |

Sources: [UserNotificationService](../../apps/backend/api/Services/UserNotificationService.cs) and [UserNotificationRepository](../../apps/backend/api/Repositories/UserNotificationRepository.cs). The inbox is user-scoped; the service does not accept an arbitrary recipient ID from the client. An unread count derived from the returned list is not necessarily a count across all historical records.

## Persistence and live delivery

[MobileNotificationService](../../apps/backend/api/Services/MobileNotificationService.cs) adds UserNotification records to the shared request context and queues them for publication. The caller saves them with the change they describe. The [action filter](../../apps/backend/api/Common/Filters/SendNotificationsFilter.cs) calls SendPendingAsync after an action completes without an exception or an explicit error-status result.

The [SignalR publisher](../../apps/backend/api/Services/SignalRNotificationPublisher.cs) sends the `notification` event to the recipient's user channel on `/hubs/notifications`. The [hub](../../apps/backend/api/Hubs/NotificationHub.cs) permits Commuter and Driver roles. Publication exceptions are logged and caught by SendPendingAsync; the saved inbox record remains available for a later list refresh.

The pending list is request-local memory. This is not a durable transport outbox, a background retry queue, or a guarantee that a disconnected phone receives an operating-system push. Distinguish the saved record from successful live receipt and from user acknowledgement.

Trip notifications target linked user accounts. Active passengers with multiple bookings on one trip are deduplicated by user for the shared trip message. Unlinked passenger profiles have no account recipient. Driver assignment changes can notify both the old and new driver; passenger trip-change messages focus on time/bay changes rather than every vehicle/driver edit.

## Mobile refresh and read state

The [NotificationStore](../../apps/mobile/lib/state/notification_store.dart) refreshes the inbox, connects the hub, merges live messages by ID, and ignores callbacks belonging to older login sessions. It refreshes after reconnection and schedules a 30-second reconnect attempt when needed. This recovers listed records after a disconnect; it does not replay every historical event beyond the API list limit.

Mark-read actions are optimistic in the mobile store. A failed API call may leave the local item visually read until a later refresh restores the server's state. Consequently, a reduced badge count alone does not prove the server saved ReadAt.

## Recovery notices use a separate API

`/api/v1/passenger-notifications` is a separate Commuter-only feed for delivered recovery passenger notices. Its [query service](../../apps/backend/api/Services/PassengerNotificationQueryService.cs) lists up to 50 delivered records and supports an owner-scoped mark-read operation. Do not merge its IDs, delivery statuses, or list results conceptually with UserNotification records merely because both are called notifications.

## Verification

Check two-account isolation, owner-only read updates, failed-action publication, hub role denial, and reconnect/list behavior. Verify persisted read state through a fresh list request. Existing backend cases are in [MobileNotificationTests](../../apps/backend/api.Tests/Notifications/MobileNotificationTests.cs), [NotificationHubTests](../../apps/backend/api.Tests/Notifications/NotificationHubTests.cs), and [SupportAndNotificationTests](../../apps/backend/api.Tests/Bookings/SupportAndNotificationTests.cs). Device background delivery and mobile race scenarios require their own execution evidence; source review is not that evidence.
