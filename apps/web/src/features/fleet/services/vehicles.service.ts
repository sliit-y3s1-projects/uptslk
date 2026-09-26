import { apiClient } from "@/lib/api/api-client";
import type {
  CreateVehicleRequest,
  UpdateVehicleRequest,
  VehicleDetail,
  VehicleListItem,
  VehicleQueryParams,
} from "../types";

export async function getVehicles(
  params?: VehicleQueryParams,
): Promise<VehicleListItem[]> {
  const query = new URLSearchParams();
  if (params?.centreId) query.set("centreId", params.centreId);
  if (params?.status) query.set("status", params.status);
  if (params?.search && params.search.trim())
    query.set("search", params.search.trim());

  const queryString = query.toString();
  const path = `/api/v1/vehicles${queryString ? `?${queryString}` : ""}`;
  return apiClient<VehicleListItem[]>(path);
}

export async function getVehicle(vehicleId: string): Promise<VehicleDetail> {
  return apiClient<VehicleDetail>(`/api/v1/vehicles/${vehicleId}`);
}

export async function createVehicle(data: CreateVehicleRequest): Promise<{
  id: string;
  centreId: string;
  plateNumber: string;
  model: string;
  status: string;
}> {
  return apiClient<{
    id: string;
    centreId: string;
    plateNumber: string;
    model: string;
    status: string;
  }>("/api/v1/vehicles", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}

export async function updateVehicle(
  vehicleId: string,
  data: UpdateVehicleRequest,
): Promise<void> {
  return apiClient<void>(`/api/v1/vehicles/${vehicleId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
}

export async function deactivateVehicle(vehicleId: string): Promise<void> {
  return apiClient<void>(`/api/v1/vehicles/${vehicleId}`, {
    method: "DELETE",
  });
}

export async function uploadVehicleImage(
  vehicleId: string,
  file: File,
): Promise<{ imageUrl: string }> {
  const formData = new FormData();
  formData.append("file", file);

  return apiClient<{ imageUrl: string }>(
    `/api/v1/vehicles/${vehicleId}/image`,
    {
      method: "POST",
      body: formData,
    },
  );
}
