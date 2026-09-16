import { apiClient } from "@/lib/api/api-client";
import type { Centre, CentreDetail, CreateCentreRequest, UpdateCentreRequest, Bay, CreateBayRequest, UpdateBayRequest } from "../types";

export const centresApi = {
  getCentres: (status?: string, district?: string, search?: string) => {
    const params = new URLSearchParams();
    if (status) params.append("status", status);
    if (district) params.append("district", district);
    if (search) params.append("search", search);
    const qs = params.toString();
    return apiClient<Centre[]>("/api/v1/centres" + (qs ? "?" + qs : ""));
  },
  getCentre: (centreId: string) => apiClient<CentreDetail>("/api/v1/centres/" + centreId),
  createCentre: (data: CreateCentreRequest) => apiClient<CentreDetail>("/api/v1/centres", { method: "POST", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  updateCentre: (centreId: string, data: UpdateCentreRequest) => apiClient<void>("/api/v1/centres/" + centreId, { method: "PUT", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  closeCentre: (centreId: string) => apiClient<void>("/api/v1/centres/" + centreId, { method: "DELETE" }),

  getBays: (centreId: string) => apiClient<Bay[]>("/api/v1/centres/" + centreId + "/bays"),
  createBay: (centreId: string, data: CreateBayRequest) => apiClient<Bay>("/api/v1/centres/" + centreId + "/bays", { method: "POST", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  updateBay: (bayId: string, data: UpdateBayRequest) => apiClient<void>("/api/v1/centres/bays/" + bayId, { method: "PUT", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  deactivateBay: (bayId: string) => apiClient<void>("/api/v1/centres/bays/" + bayId, { method: "DELETE" }),
};
