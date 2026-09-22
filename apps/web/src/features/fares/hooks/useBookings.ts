import { useMutation, useQuery } from "@tanstack/react-query";
import {
  bookingsService as service,
  bookingLookups,
} from "../services/bookings.service";
import { useRefreshPassengerFares } from "./useRefresh";
import type { BookingFilters } from "../types/fares";
export const useBookings = (filters: BookingFilters = {}) =>
  useQuery({
    queryKey: ["bookings", "list", filters],
    queryFn: () => service.list(filters),
    retry: false,
  });
export const useBooking = (id: string) =>
  useQuery({
    queryKey: ["bookings", id],
    queryFn: () => service.detail(id),
    enabled: !!id,
    retry: false,
  });
export const useSeats = (id: string) =>
  useQuery({
    queryKey: ["booking-seats", id],
    queryFn: () => service.seats(id),
    enabled: !!id,
    refetchInterval: 15000,
    retry: false,
  });
export const useManifest = (id: string) =>
  useQuery({
    queryKey: ["booking-manifests", id],
    queryFn: () => service.manifest(id),
    enabled: !!id,
    refetchInterval: 15000,
    retry: false,
  });
export const useTrips = (centreId: string) =>
  useQuery({
    queryKey: ["fare-lookups", "trips", centreId],
    queryFn: () => bookingLookups.trips(centreId),
    enabled: !!centreId,
    refetchInterval: 15000,
    retry: false,
  });
export const useTrip = (id: string) =>
  useQuery({
    queryKey: ["fare-lookups", "trip", id],
    queryFn: () => bookingLookups.trip(id),
    enabled: !!id,
    refetchInterval: 15000,
    retry: false,
  });
export const useRoutes = (centreId: string) =>
  useQuery({
    queryKey: ["fare-lookups", "routes", centreId],
    queryFn: () => bookingLookups.routes(centreId),
    enabled: !!centreId,
    retry: false,
  });
export const useCentres = () =>
  useQuery({
    queryKey: ["fare-lookups", "centres"],
    queryFn: bookingLookups.centres,
    retry: false,
  });
export function useBookingMutations() {
  const refresh = useRefreshPassengerFares();
  const create = useMutation({
    mutationFn: service.create,
    onSuccess: refresh,
    onError: refresh,
  });
  const checkout = useMutation({
    mutationFn: service.startCheckout,
    onError: refresh,
  });
  const seat = useMutation({
    mutationFn: ({ id, seatNumber }: { id: string; seatNumber: string }) =>
      service.seat(id, seatNumber),
    onSuccess: refresh,
    onError: refresh,
  });
  const complete = useMutation({
    mutationFn: service.complete,
    onSuccess: refresh,
  });
  const cancel = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      service.cancel(id, reason),
    onSuccess: refresh,
  });
  return { create, checkout, seat, complete, cancel };
}
