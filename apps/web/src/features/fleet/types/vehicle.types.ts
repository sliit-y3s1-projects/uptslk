export type VehicleType = "Normal" | "SemiLuxury" | "AcExpress";
export type VehicleStatus = "Active" | "Maintenance" | "Inactive";

export interface VehicleMaintenanceItem {
  id: string;
  type: string;
  description: string;
  status: "Scheduled" | "InProgress" | "Completed" | "Cancelled";
  scheduledFor: string;
  completedAt?: string | null;
}

export interface VehicleListItem {
  id: string;
  centreId: string;
  centre: string;
  plateNumber: string;
  model: string;
  type: VehicleType;
  capacity: number;
  isAccessible: boolean;
  status: VehicleStatus;
  imageUrl?: string | null;
  maintenanceCount: number;
}

export interface VehicleDetail {
  id: string;
  centreId: string;
  centre: {
    id: string;
    code: string;
    name: string;
  };
  plateNumber: string;
  model: string;
  type: VehicleType;
  capacity: number;
  isAccessible: boolean;
  status: VehicleStatus;
  imageUrl?: string | null;
  maintenance: VehicleMaintenanceItem[];
}

export interface CreateVehicleRequest {
  centreId: string;
  plateNumber: string;
  model: string;
  type: VehicleType;
  capacity: number;
  isAccessible: boolean;
  status: VehicleStatus;
}

export interface UpdateVehicleRequest {
  centreId: string;
  plateNumber: string;
  model: string;
  type: VehicleType;
  capacity: number;
  isAccessible: boolean;
  status: VehicleStatus;
}

export interface VehicleQueryParams {
  centreId?: string;
  status?: VehicleStatus;
  search?: string;
}
