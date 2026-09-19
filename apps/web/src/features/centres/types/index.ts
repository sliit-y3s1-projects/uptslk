export type CentreStatus = "Planned" | "Operating" | "Closed" | "Suspended";
export type BayStatus = "Available" | "Occupied" | "OutOfService";

export interface Centre {
  id: string;
  code: string;
  name: string;
  city: string;
  district: string;
  status: CentreStatus;
  bayCount: number;
  routeCount: number;
}

export interface Bay {
  id: string;
  centreId: string;
  code: string;
  name: string | null;
  status: BayStatus;
}

export interface RouteSummary {
  id: string;
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
  isActive: boolean;
}

export interface CentreDetail {
  id: string;
  code: string;
  name: string;
  city: string;
  district: string;
  description: string | null;
  status: CentreStatus;
  bays: Bay[];
  routes: RouteSummary[];
}

export interface CreateCentreRequest {
  code: string;
  name: string;
  city: string;
  district: string;
  description?: string;
  status: CentreStatus;
}

export interface UpdateCentreRequest {
  name: string;
  city: string;
  district: string;
  description?: string;
  status: CentreStatus;
}

export interface CreateBayRequest {
  code: string;
  name?: string;
  status: BayStatus;
}

export interface UpdateBayRequest {
  code: string;
  name?: string;
  status: BayStatus;
}
