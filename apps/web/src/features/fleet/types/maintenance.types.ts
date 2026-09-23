export type MaintenanceStatus =
  "Scheduled" | "InProgress" | "Completed" | "Cancelled";

export interface MaintenanceListItem {
  id: string;
  vehicleId: string;
  vehicle: string;
  centreId: string;
  centre: string;
  type: string;
  description: string;
  status: MaintenanceStatus;
  scheduledFor: string;
  completedAt?: string | null;
}

export interface MaintenanceDetail {
  id: string;
  vehicleId: string;
  vehicle: {
    id: string;
    plateNumber: string;
    model: string;
    centreId: string;
  };
  type: string;
  description: string;
  status: MaintenanceStatus;
  scheduledFor: string;
  completedAt?: string | null;
}

export interface CreateMaintenanceRequest {
  vehicleId: string;
  type: string;
  description: string;
  scheduledFor: string;
}

export interface UpdateMaintenanceRequest {
  type: string;
  description: string;
  status: MaintenanceStatus;
  scheduledFor: string;
  completedAt?: string | null;
}

export interface MaintenanceQueryParams {
  vehicleId?: string;
  centreId?: string;
  status?: MaintenanceStatus;
}
