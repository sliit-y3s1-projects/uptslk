import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as incidents from "../services/incidents.service";
import type { CreateIncidentRequest, UpdateIncidentRequest } from "../types/incidents";
export function useIncidents(centreId?: string) { return useQuery({ queryKey: ["incidents", centreId], queryFn: () => incidents.getIncidents(centreId) }); }
export function useCreateIncident() { const client = useQueryClient(); return useMutation({ mutationFn: (data: CreateIncidentRequest) => incidents.createIncident(data), onSuccess: () => client.invalidateQueries({ queryKey: ["incidents"] }) }); }
export function useUpdateIncident() { const client = useQueryClient(); return useMutation({ mutationFn: ({ id, data }: { id: string; data: UpdateIncidentRequest }) => incidents.updateIncident(id, data), onSuccess: () => client.invalidateQueries({ queryKey: ["incidents"] }) }); }
