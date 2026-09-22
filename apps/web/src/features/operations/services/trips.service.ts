import { apiClient } from "@/lib/api/api-client";
import type {
  CreateTripRequest,
  ReassignTripRequest,
  TripDetail,
  TripListItem,
  TripStatus,
  UpdateTripRequest,
} from "../types/trips";

export function getTrips(
  params: { centreId?: string; terminalId?: string; routeId?: string; directionId?: string; status?: string; date?: string } = {},
) {
  const q = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value) q.set(key, value);
  });
  return apiClient<TripListItem[]>(`/api/v1/trips${q.size ? `?${q}` : ""}`);
}
export function getTrip(id: string) {
  return apiClient<TripDetail>(`/api/v1/trips/${id}`);
}
export function getTripHistory(centreId?: string) {
  return getTripsHistory(centreId);
}
function getTripsHistory(centreId?: string) {
  const q = centreId ? `?centreId=${encodeURIComponent(centreId)}` : "";
  return apiClient<TripListItem[]>(`/api/v1/trips/history${q}`);
}
export function createTrip(data: CreateTripRequest) {
  return apiClient<{ id: string }>("/api/v1/trips", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}
export function updateTrip(id: string, data: UpdateTripRequest) {
  return apiClient<void>(`/api/v1/trips/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}
export function reassignTrip(id: string, data: ReassignTripRequest) {
  return apiClient<void>(`/api/v1/trips/${id}/reassign`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}
export function updateTripStatus(
  id: string,
  status: TripStatus,
  note?: string,
) {
  return apiClient<void>(`/api/v1/trips/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status, note }),
  });
}
export function cancelTrip(id: string, reason: string) {
  return apiClient<void>(`/api/v1/trips/${id}`, {
    method: "DELETE",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ reason }),
  });
}
