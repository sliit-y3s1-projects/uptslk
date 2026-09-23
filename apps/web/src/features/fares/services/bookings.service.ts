import type {
  CreatedBooking,
  CompleteBookingRequest,
  CancelBookingRequest,
} from "../types/fares";
import { apiClient } from "@/lib/api/api-client";
import { jsonBody, queryString } from "@/features/riders/services/request";
import type {
  Booking,
  BookingDetail,
  BookingFilters,
  CreateBookingRequest,
  Manifest,
  TripOption,
  TripDetail,
  RouteOption,
  CentreOption,
  CheckoutSession,
  PaymentOrderStatus,
} from "../types/fares";
const path = "/api/v1/bookings";
export const bookingsService = {
  list: (filters: BookingFilters) =>
    apiClient<Booking[]>(path + queryString(filters)),
  detail: (id: string) => apiClient<BookingDetail>(`${path}/${id}`),
  create: (body: CreateBookingRequest) =>
    apiClient<CreatedBooking>(path, jsonBody("POST", body)),
  startCheckout: (body: CreateBookingRequest) =>
    apiClient<CheckoutSession>(
      "/api/v1/payments/checkout",
      jsonBody("POST", body),
    ),
  paymentOrder: (orderId: string) =>
    apiClient<PaymentOrderStatus>(
      `/api/v1/payments/orders/${encodeURIComponent(orderId)}`,
    ),
  complete: (id: string) =>
    apiClient<void>(
      `${path}/${id}/status`,
      jsonBody("PATCH", {
        status: "Completed",
      } satisfies CompleteBookingRequest),
    ),
  cancel: (id: string, reason: string) =>
    apiClient<void>(
      `${path}/${id}`,
      jsonBody("DELETE", { reason } satisfies CancelBookingRequest),
    ),
  manifest: (tripId: string) =>
    apiClient<Manifest>(`${path}/trips/${tripId}/manifest`),
};
// Read-only lookup endpoints supply real foreign keys and current trip metadata.
export const bookingLookups = {
  trips: (centreId: string) =>
    apiClient<TripOption[]>(`/api/v1/trips${queryString({ centreId })}`),
  trip: (id: string) => apiClient<TripDetail>(`/api/v1/trips/${id}`),
  routes: (centreId: string) =>
    apiClient<RouteOption[]>(`/api/v1/routes${queryString({ centreId })}`),
  centres: () => apiClient<CentreOption[]>("/api/v1/centres"),
};
