export type TripStatus =
  | "Scheduled"
  | "Ready"
  | "Boarding"
  | "Dispatched"
  | "Completed"
  | "Cancelled"
  | "Delayed";

export interface TripListItem {
  id: string;
  centreId: string;
  routeId: string;
  routeDirectionId?: string | null;
  routeNumber: string;
  routeName: string;
  directionName?: string | null;
  origin?: string;
  destination?: string;
  vehicleId: string;
  vehicle: string;
  driverId: string;
  driver: string;
  bayId: string;
  bay: string;
  scheduledTime: string;
  status: TripStatus;
  notes?: string;
}

export interface TripDetail extends TripListItem {
  centre: { id: string; code: string; name: string };
  route: {
    id: string;
    routeNumber: string;
    name: string;
    origin: string;
    destination: string;
    estimatedDurationMin: number;
  };
  vehicleDetails: {
    id: string;
    plateNumber: string;
    model: string;
    capacity: number;
    isAccessible: boolean;
  };
  driverDetails: { id: string; fullName: string; licenseNumber: string };
  bayDetails: { id: string; code: string; name?: string };
  cancellationReason?: string;
  actualDepartureAt?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTripRequest {
  centreId: string;
  routeId: string;
  routeDirectionId?: string | null;
  vehicleId: string;
  driverId: string;
  bayId: string;
  scheduledTime: string;
  notes?: string;
}
export type UpdateTripRequest = CreateTripRequest;
export interface ReassignTripRequest {
  vehicleId: string;
  driverId: string;
  bayId: string;
  scheduledTime: string;
  notes?: string;
}
export interface UpdateTripStatusRequest {
  status: TripStatus;
  note?: string;
}
