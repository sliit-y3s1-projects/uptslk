export type DriverStatus = "Active" | "Inactive";

export interface DriverListItem {
  id: string;
  centreId: string;
  centre: string;
  fullName: string;
  phoneNumber?: string | null;
  licenseNumber: string;
  status: DriverStatus;
  userId?: string | null;
}

export interface DriverDetail {
  id: string;
  centreId: string;
  centre: {
    id: string;
    code: string;
    name: string;
  };
  fullName: string;
  phoneNumber?: string | null;
  licenseNumber: string;
  status: DriverStatus;
  userId?: string | null;
}

export interface CreateDriverRequest {
  centreId: string;
  fullName: string;
  phoneNumber?: string | null;
  licenseNumber: string;
  status: DriverStatus;
}

export interface UpdateDriverRequest {
  centreId: string;
  fullName: string;
  phoneNumber?: string | null;
  status: DriverStatus;
}

export interface DriverQueryParams {
  centreId?: string;
  status?: DriverStatus;
  search?: string;
}
