import type { TripDetail, TripListItem, TripStatus } from "@/features/operations/types/trips";
import type { IncidentListItem } from "@/features/operations/types/incidents";

// Test data for Component C (Scheduling and Dispatch). Only this folder uses it.

export const CENTRE_ID = "centre-1";

export function buildTrip(overrides: Partial<TripListItem> = {}): TripListItem {
  return {
    id: "trip-1",
    centreId: CENTRE_ID,
    routeId: "route-1",
    routeDirectionId: "dir-1",
    routeNumber: "101",
    routeName: "Colombo - Kandy",
    directionName: "Colombo → Kandy",
    origin: "Colombo",
    destination: "Kandy",
    capacity: 52,
    occupied: 10,
    available: 42,
    isFull: false,
    vehicleId: "vehicle-1",
    vehicle: "WP NB-4821",
    driverId: "driver-1",
    driver: "Kamal Perera",
    bayId: "bay-1",
    bay: "B1",
    scheduledTime: "2030-06-10T04:30:00Z",
    status: "Scheduled",
    ...overrides,
  };
}

export function buildTripDetail(overrides: Partial<TripDetail> = {}): TripDetail {
  const base = buildTrip();
  return {
    ...base,
    centre: { id: CENTRE_ID, code: "CMB", name: "Colombo Fort" },
    route: {
      id: "route-1", routeNumber: "101", name: "Colombo - Kandy", origin: "Colombo", destination: "Kandy", estimatedDurationMin: 180,
    },
    vehicleDetails: { id: "vehicle-1", plateNumber: "WP NB-4821", model: "Ashok Leyland Viking", capacity: 52, isAccessible: true },
    driverDetails: { id: "driver-1", fullName: "Kamal Perera", licenseNumber: "DL-100" },
    bayDetails: { id: "bay-1", code: "B1" },
    notes: "Hold for the connecting train",
    createdAt: "2030-06-01T00:00:00Z",
    updatedAt: "2030-06-09T00:00:00Z",
    ...overrides,
  };
}

export const tripWith = (status: TripStatus, overrides: Partial<TripListItem> = {}) =>
  buildTrip({ id: `trip-${status.toLowerCase()}`, status, routeNumber: status.slice(0, 3).toUpperCase(), ...overrides });

export function buildIncident(overrides: Partial<IncidentListItem> = {}): IncidentListItem {
  return {
    id: "incident-1",
    centreId: CENTRE_ID,
    tripId: "trip-1",
    tripRouteNumber: "101",
    tripRouteName: "Colombo - Kandy",
    reportedByName: "Control room",
    type: "Breakdown",
    severity: "High",
    title: "Engine fault at bay 3",
    description: "The bus stopped while boarding.",
    status: "Open",
    slaDueAt: "2030-06-10T08:00:00Z",
    createdAt: "2030-06-10T04:00:00Z",
    ...overrides,
  };
}
