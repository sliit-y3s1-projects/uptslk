import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as trips from "../services/trips.service";
import type { CreateTripRequest, ReassignTripRequest, TripStatus, UpdateTripRequest } from "../types/trips";
export function useTrips(params: { centreId?: string; status?: string; date?: string } = {}) { return useQuery({ queryKey: ["trips", params], queryFn: () => trips.getTrips(params) }); }
export function useTrip(id?: string) { return useQuery({ queryKey: ["trips", id], queryFn: () => trips.getTrip(id!), enabled: Boolean(id) }); }
export function useTripHistory(centreId?: string) { return useQuery({ queryKey: ["trips", "history", centreId], queryFn: () => trips.getTripHistory(centreId) }); }
function invalidate(client: ReturnType<typeof useQueryClient>) { client.invalidateQueries({ queryKey: ["trips"] }); client.invalidateQueries({ queryKey: ["incidents"] }); }
export function useCreateTrip() { const client = useQueryClient(); return useMutation({ mutationFn: (data: CreateTripRequest) => trips.createTrip(data), onSuccess: () => invalidate(client) }); }
export function useUpdateTrip(id: string) { const client = useQueryClient(); return useMutation({ mutationFn: (data: UpdateTripRequest) => trips.updateTrip(id, data), onSuccess: () => invalidate(client) }); }
export function useReassignTrip(id: string) { const client = useQueryClient(); return useMutation({ mutationFn: (data: ReassignTripRequest) => trips.reassignTrip(id, data), onSuccess: () => invalidate(client) }); }
export function useUpdateTripStatus() { const client = useQueryClient(); return useMutation({ mutationFn: ({ id, status, note }: { id: string; status: TripStatus; note?: string }) => trips.updateTripStatus(id, status, note), onSuccess: () => invalidate(client) }); }
export function useCancelTrip() { const client = useQueryClient(); return useMutation({ mutationFn: ({ id, reason }: { id: string; reason: string }) => trips.cancelTrip(id, reason), onSuccess: () => invalidate(client) }); }

