import { apiClient } from "@/lib/api/api-client";
import type {
  CreateMaintenanceRequest,
  MaintenanceDetail,
  MaintenanceListItem,
  MaintenanceQueryParams,
  UpdateMaintenanceRequest,
} from "../types";

export async function getMaintenanceRecords(
  params?: MaintenanceQueryParams,
): Promise<MaintenanceListItem[]> {
  const query = new URLSearchParams();
  if (params?.vehicleId) query.set("vehicleId", params.vehicleId);
  if (params?.centreId) query.set("centreId", params.centreId);
  if (params?.status) query.set("status", params.status);

  const queryString = query.toString();
  const path = `/api/v1/maintenance-records${queryString ? `?${queryString}` : ""}`;
  return apiClient<MaintenanceListItem[]>(path);
}

export async function getMaintenanceRecord(
  recordId: string,
): Promise<MaintenanceDetail> {
  return apiClient<MaintenanceDetail>(
    `/api/v1/maintenance-records/${recordId}`,
  );
}

export async function createMaintenanceRecord(
  data: CreateMaintenanceRequest,
): Promise<{
  id: string;
  vehicleId: string;
  type: string;
  status: string;
  scheduledFor: string;
}> {
  return apiClient<{
    id: string;
    vehicleId: string;
    type: string;
    status: string;
    scheduledFor: string;
  }>("/api/v1/maintenance-records", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}

export async function updateMaintenanceRecord(
  recordId: string,
  data: UpdateMaintenanceRequest,
): Promise<void> {
  return apiClient<void>(`/api/v1/maintenance-records/${recordId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}

export async function cancelMaintenanceRecord(recordId: string): Promise<void> {
  return apiClient<void>(`/api/v1/maintenance-records/${recordId}`, {
    method: "DELETE",
  });
}
