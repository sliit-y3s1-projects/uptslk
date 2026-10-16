# Passenger, booking, and payment data dictionary

**Audience:** Component D developers, testers, and reviewers.

**Source baseline:** `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16.

**Scope:** selected domain fields and relationships in the existing implementation. This is not a proposed schema or a complete API payload specification.

## Core records

| Entity / field | Type and meaning | Review implications |
| --- | --- | --- |
| Passenger `Id` / `UserId` | GUID identity / nullable linked account GUID | A staff-created passenger may have no portal account. Do not substitute a user ID for a passenger ID. |
| Passenger `FullName`, `PhoneNumber`, `Email` | Name, required phone, optional email | Personal information; use synthetic values in shared evidence. Phone normalization removes spaces and hyphens after trimming. |
| Passenger `Category`, `IsActive` | Classification and active flag | Booking requires active status. Category is not a standard-fare lookup key in the current model. |
| Wallet `PassengerId`, `Balance` | Passenger relationship and decimal balance | One wallet per passenger; top-ups and wallet-funded booking/refund operations update this balance. |
| Booking `TripId`, `PassengerId` | Trip and passenger GUIDs | Relationships use API identifiers, not route numbers or passenger display names. |
| Booking `PassengerCount` | Integer group size, default 1 | Creation accepts 1–10. Capacity sums this field rather than counting rows. |
| Booking `SeatNumber` | Generated boarding reference | Internal legacy field name; not a passenger-selected numbered seat. |
| Booking `Fare`, `PassengerCategory` | Total decimal fare and category snapshot | Fare is total for the group. Historical booking values are not a live fare quote. |
| Booking `QrCode` | Stored `BKG-...` reference | Ticket/detail responses expose it only when boarding is allowed. Treat it as ticket data when sharing evidence. |
| Booking cancellation fields | `CancelledAt`, reason, decimal `RefundAmount` | For card refunds, the amount is recorded when accepted, before provider completion. |
| FareRule `RouteId`, `Amount`, `IsActive` | Route, standard per-passenger amount, availability flag | One record per route, including inactive records; see the [fare reference](fare-rules-and-calculations.md). |

Sources: [Passenger](../../apps/backend/api/Entities/Passenger.cs), [Wallet](../../apps/backend/api/Entities/Wallet.cs), [Booking](../../apps/backend/api/Entities/Booking.cs), [FareRule](../../apps/backend/api/Entities/FareRule.cs), and [PassengerService](../../apps/backend/api/Services/PassengerService.cs).

## Money movement and provider records

| Record | Important fields | Interpretation |
| --- | --- | --- |
| Transaction | WalletId, optional BookingId, Type, Amount | Wallet history. The enum includes Topup, Fare, Refund, and Payout; enum membership alone does not prove an exposed payout workflow. |
| Payment | BookingId, Provider, ProviderOrderId, ProviderCheckoutId, ProviderPaymentId, Status, Amount, Currency | External checkout record. Currency defaults to LKR; provider identifiers serve different correlation purposes. |
| PaymentRefund | PaymentId, Amount, Reason, Status, ProviderRefundId, FailureReason | A refund attempt and its provider outcome, separate from the original payment. |
| PaymentWebhookEvent | Provider, ProviderEventId, EventType | Stored processing evidence used for event deduplication. |

Sources: [Transaction](../../apps/backend/api/Entities/Transaction.cs), [Payment](../../apps/backend/api/Entities/Payment.cs), [PaymentRefund](../../apps/backend/api/Entities/PaymentRefund.cs), and [PaymentWebhookEvent](../../apps/backend/api/Entities/PaymentWebhookEvent.cs).

## Constraints and representation

- Passenger phone numbers have a unique index. The passenger/account relationship is optional; deletion of the linked User sets the passenger foreign key to null. Name/phone maximum lengths in the mapping are 160/32.
- Booking trip/passenger relationships restrict deletion. Fare and refund amount use precision `(12,2)`. The unique trip/boarding-reference index applies to Pending and Confirmed bookings; service locking and capacity checks enforce the aggregate occupancy rule.
- FareRule has a unique RouteId index and amount precision `(12,2)`. Deactivating a fare does not remove the uniqueness constraint.
- Payment has a unique `(Provider, ProviderOrderId)` index, amount precision `(12,2)`, and a currency maximum length of three. These constraints do not establish provider settlement.
- Entity timestamps initialize with UTC values. API JSON uses string enum serialization; database enum numeric values are implementation details and should not be copied into client contracts.

Mappings: [PassengerConfiguration](../../apps/backend/api/Data/Configurations/PassengerConfiguration.cs), [BookingConfiguration](../../apps/backend/api/Data/Configurations/BookingConfiguration.cs), [FareRuleConfiguration](../../apps/backend/api/Data/Configurations/FareRuleConfiguration.cs), [PaymentConfiguration](../../apps/backend/api/Data/Configurations/PaymentConfiguration.cs), and [JSON setup](../../apps/backend/api/Common/Extensions/ApiServiceExtensions.cs).

## Reading responses correctly

Entity properties are not a promise that every endpoint returns those properties. Use [BookingResponses](../../apps/backend/api/DTO/Bookings/BookingResponses.cs) and [PaymentResponses](../../apps/backend/api/DTO/Payments/PaymentResponses.cs) for response shapes. Derived values such as ticket group, boarding eligibility, and remaining capacity must not be treated as independently stored status columns. This document adds no retention policy, schema migration, or production data-handling approval.
