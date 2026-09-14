import { apiClient } from "@/lib/api/api-client";
import type {
  CreateDriverRequest,
  DriverDetail,
  DriverListItem,
  DriverQueryParams,
  UpdateDriverRequest,
} from "../types";

export async function getDrivers(params?: DriverQueryParams): Promise<DriverListItem[]> {
  const query = new URLSearchParams();
  if (params?.centreId) query.set("centreId", params.centreId);
  if (params?.status) query.set("status", params.status);
  if (params?.search && params.search.trim()) query.set("search", params.search.trim());

  const queryString = query.toString();
  const path = `/api/v1/drivers${queryString ? `?${queryString}` : ""}`;
  return apiClient<DriverListItem[]>(path);
}

export async function getDriver(driverId: string): Promise<DriverDetail> {
  return apiClient<DriverDetail>(`/api/v1/drivers/${driverId}`);
}

export async function createDriver(data: CreateDriverRequest): Promise<{ id: string; centreId: string; fullName: string; licenseNumber: string; status: string }> {
  return apiClient<{ id: string; centreId: string; fullName: string; licenseNumber: string; status: string }>("/api/v1/drivers", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}

export async function updateDriver(driverId: string, data: UpdateDriverRequest): Promise<void> {
  return apiClient<void>(`/api/v1/drivers/${driverId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}

export async function deactivateDriver(driverId: string): Promise<void> {
  return apiClient<void>(`/api/v1/drivers/${driverId}`, {
    method: "DELETE",
  });
}

