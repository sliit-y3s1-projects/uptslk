import { apiClient } from "@/lib/api/api-client";
import type { CreateIncidentRequest, IncidentDetail, IncidentListItem, UpdateIncidentRequest } from "../types/incidents";
export function getIncidents(centreId?: string) { const q = centreId ? `?centreId=${encodeURIComponent(centreId)}` : ""; return apiClient<IncidentListItem[]>(`/api/v1/incidents${q}`); }
export function getIncident(id: string) { return apiClient<IncidentDetail>(`/api/v1/incidents/${id}`); }
export function createIncident(data: CreateIncidentRequest) { return apiClient<{ id: string }>("/api/v1/incidents", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(data) }); }
export function updateIncident(id: string, data: UpdateIncidentRequest) { return apiClient<void>(`/api/v1/incidents/${id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(data) }); }

