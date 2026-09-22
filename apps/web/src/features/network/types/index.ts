export interface RouteStop {
  id?: string;
  stopName: string;
  sequenceOrder?: number;
  latitude?: number;
  longitude?: number;
}

export interface RouteSchedule {
  id: string;
  routeId: string;
  routeDirectionId?: string | null;
  bayId: string;
  bayCode: string;
  firstDeparture: string;
  lastDeparture: string;
  headwayMinutes: number;
  operatingDays: string;
  isActive: boolean;
}

export interface RouteDirection {
  id: string;
  routeId: string;
  startCentreId: string;
  startCentre: { id: string; code: string; name: string };
  endCentreId: string;
  endCentre: { id: string; code: string; name: string };
  name: string;
  distanceKm: number;
  estimatedDurationMin: number;
  isActive: boolean;
  stops: RouteStop[];
  schedules: RouteSchedule[];
}

export interface Route {
  id: string;
  centreId: string;
  centre: { id: string; code: string; name: string };
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
  serviceType: number;
  distanceKm: number;
  estimatedDurationMin: number;
  isActive: boolean;
  stops: RouteStop[];
  schedules: RouteSchedule[];
  directions: RouteDirection[];
}

export interface RouteSummary {
  id: string;
  centreId: string;
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
  estimatedDurationMin: number;
  isActive: boolean;
  directions?: RouteDirection[];
}

export interface RouteStopRequest {
  stopName: string;
  latitude: number;
  longitude: number;
}

export interface CreateRouteRequest {
  centreId: string;
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
  serviceType: number;
  distanceKm: number;
  estimatedDurationMin: number;
  stops: RouteStopRequest[];
  startCentreId?: string;
  endCentreId?: string;
}

export interface CreateRouteDirectionRequest {
  startCentreId: string;
  endCentreId: string;
  name: string;
  distanceKm: number;
  estimatedDurationMin: number;
  stops: RouteStopRequest[];
}

export interface UpdateRouteRequest {
  name: string;
  origin: string;
  destination: string;
  serviceType: number;
  distanceKm: number;
  estimatedDurationMin: number;
  isActive: boolean;
  stops: RouteStopRequest[];
}

export interface CreateRouteScheduleRequest {
  routeDirectionId?: string;
  bayId: string;
  firstDeparture: string;
  lastDeparture: string;
  headwayMinutes: number;
  operatingDays: string;
}

export interface UpdateRouteScheduleRequest {
  bayId: string;
  firstDeparture: string;
  lastDeparture: string;
  headwayMinutes: number;
  operatingDays: string;
  isActive: boolean;
}

export interface GenerateScheduleTripsRequest {
  serviceDate: string;
}

export interface GenerateScheduleTripsResult {
  planned: number;
  created: number;
  existing: number;
  conflicts: number;
  serviceDate: string;
  createdDepartures: string[];
  skippedDepartures: { time: string; reason: string }[];
}
