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
  bayId: string;
  bayCode: string;
  firstDeparture: string;
  lastDeparture: string;
  headwayMinutes: number;
  operatingDays: string;
  isActive: boolean;
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
}

export interface RouteSummary {
  id: string;
  centreId: string;
  routeNumber: string;
  name: string;
  origin: string;
  destination: string;
  isActive: boolean;
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
