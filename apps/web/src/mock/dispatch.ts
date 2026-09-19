export type TripStatus =
  | "Scheduled"
  | "Ready"
  | "Boarding"
  | "Dispatched"
  | "Completed"
  | "Delayed"
  | "Cancelled";

export type Trip = {
  id: string;
  centreId: string;
  serviceDate: string;
  scheduledTime: string;
  route: string;
  origin: string;
  destination: string;
  vehicleId: string;
  driverId: string;
  bay: string;
  occupancy: number;
  status: TripStatus;
  notes: string;
  updatedAt: string;
};

export type DispatchIncident = {
  id: string;
  centreId: string;
  tripId?: string;
  category: string;
  title: string;
  severity: "Low" | "Medium" | "High";
  status: "Open" | "Investigating" | "Resolved";
  owner: string;
  reportedAt: string;
};

export type DispatchApproval = {
  id: string;
  centreId: string;
  tripId: string;
  request: string;
  reason: string;
  status: "Pending" | "Approved" | "Rejected";
  requestedBy: string;
};

export const dispatchResources = {
  makumbura: {
    vehicles: ["WP CAB-4821", "WP NB-6602", "WP CAQ-7750", "WP CAD-1024"],
    drivers: [
      "DRV-020 · A. Perera",
      "DRV-036 · S. Fernando",
      "DRV-051 · N. Silva",
      "DRV-064 · K. Jayasinghe",
    ],
    routes: [
      "138 · Pettah",
      "EX01 · Galle",
      "EX02 · Matara",
      "C001 · Colombo Fort",
    ],
    bays: ["B03", "B05", "B07", "B11"],
  },
  kadawatha: {
    vehicles: [
      "WP ND-7714",
      "WP NC-3381",
      "WP ND-7304",
      "WP NB-2428",
      "WP NC-9012",
    ],
    drivers: [
      "DRV-071 · M. Iqbal",
      "DRV-078 · D. Kumara",
      "DRV-082 · R. Perera",
      "DRV-089 · S. Silva",
    ],
    routes: [
      "177 · Kaduwela",
      "EX04 · Kandy",
      "EX06 · Kurunegala",
      "234 · Colombo Fort",
    ],
    bays: ["B02", "B04", "B06", "B09", "B13"],
  },
};

export const initialTrips: Trip[] = [
  {
    id: "TRP-8241",
    centreId: "makumbura",
    serviceDate: "2026-09-07",
    scheduledTime: "09:45",
    route: "EX01",
    origin: "Makumbura MMC",
    destination: "Galle",
    vehicleId: "WP CAB-4821",
    driverId: "DRV-020 · A. Perera",
    bay: "B03",
    occupancy: 76,
    status: "Boarding",
    notes: "Expressway service",
    updatedAt: "2 min ago",
  },
  {
    id: "TRP-8244",
    centreId: "makumbura",
    serviceDate: "2026-09-07",
    scheduledTime: "10:00",
    route: "EX02",
    origin: "Makumbura MMC",
    destination: "Matara",
    vehicleId: "WP NB-6602",
    driverId: "DRV-036 · S. Fernando",
    bay: "B07",
    occupancy: 48,
    status: "Ready",
    notes: "Vehicle inspection complete",
    updatedAt: "6 min ago",
  },
  {
    id: "TRP-8248",
    centreId: "makumbura",
    serviceDate: "2026-09-07",
    scheduledTime: "10:20",
    route: "C001",
    origin: "Makumbura MMC",
    destination: "Colombo Fort",
    vehicleId: "WP CAD-1024",
    driverId: "DRV-051 · N. Silva",
    bay: "B05",
    occupancy: 88,
    status: "Delayed",
    notes: "Traffic restriction",
    updatedAt: "4 min ago",
  },
  {
    id: "TRP-8310",
    centreId: "kadawatha",
    serviceDate: "2026-09-07",
    scheduledTime: "10:30",
    route: "177",
    origin: "Kadawatha MMC",
    destination: "Kaduwela",
    vehicleId: "WP NC-4582",
    driverId: "DRV-071 · M. Iqbal",
    bay: "B04",
    occupancy: 81,
    status: "Dispatched",
    notes: "Recurring 30-minute service",
    updatedAt: "18 min ago",
  },
  {
    id: "TRP-8311",
    centreId: "kadawatha",
    serviceDate: "2026-09-07",
    scheduledTime: "11:00",
    route: "177",
    origin: "Kadawatha MMC",
    destination: "Kaduwela",
    vehicleId: "WP ND-7714",
    driverId: "DRV-078 · D. Kumara",
    bay: "B04",
    occupancy: 62,
    status: "Boarding",
    notes: "Recurring 30-minute service",
    updatedAt: "Now",
  },
  {
    id: "TRP-8312",
    centreId: "kadawatha",
    serviceDate: "2026-09-07",
    scheduledTime: "11:30",
    route: "177",
    origin: "Kadawatha MMC",
    destination: "Kaduwela",
    vehicleId: "WP NB-2428",
    driverId: "DRV-082 · R. Perera",
    bay: "B04",
    occupancy: 14,
    status: "Scheduled",
    notes: "Recurring 30-minute service",
    updatedAt: "Planned",
  },
  {
    id: "TRP-8304",
    centreId: "kadawatha",
    serviceDate: "2026-09-07",
    scheduledTime: "10:50",
    route: "EX04",
    origin: "Kadawatha MMC",
    destination: "Kandy",
    vehicleId: "WP NC-3381",
    driverId: "DRV-089 · S. Silva",
    bay: "B02",
    occupancy: 64,
    status: "Ready",
    notes: "Intercity service",
    updatedAt: "5 min ago",
  },
  {
    id: "TRP-8198",
    centreId: "kadawatha",
    serviceDate: "2026-09-06",
    scheduledTime: "18:30",
    route: "177",
    origin: "Kadawatha MMC",
    destination: "Kaduwela",
    vehicleId: "WP ND-7304",
    driverId: "DRV-071 · M. Iqbal",
    bay: "B04",
    occupancy: 74,
    status: "Completed",
    notes: "Completed normally",
    updatedAt: "Yesterday",
  },
  {
    id: "TRP-8182",
    centreId: "makumbura",
    serviceDate: "2026-09-06",
    scheduledTime: "17:00",
    route: "EX01",
    origin: "Makumbura MMC",
    destination: "Galle",
    vehicleId: "WP CAB-4821",
    driverId: "DRV-020 · A. Perera",
    bay: "B03",
    occupancy: 91,
    status: "Completed",
    notes: "Completed 8 min late",
    updatedAt: "Yesterday",
  },
];

export const initialDispatchIncidents: DispatchIncident[] = [
  {
    id: "INC-1042",
    centreId: "makumbura",
    tripId: "TRP-8248",
    category: "Delay",
    title: "Departure delayed by traffic restriction",
    severity: "High",
    status: "Investigating",
    owner: "Dispatch desk",
    reportedAt: "10:08",
  },
  {
    id: "INC-1051",
    centreId: "kadawatha",
    tripId: "TRP-8310",
    category: "Passenger",
    title: "Passenger queue exceeded threshold",
    severity: "Medium",
    status: "Open",
    owner: "Bay coordinator",
    reportedAt: "10:21",
  },
];

export const initialDispatchApprovals: DispatchApproval[] = [
  {
    id: "APR-301",
    centreId: "kadawatha",
    tripId: "TRP-8312",
    request: "Temporary bay reassignment",
    reason: "B04 requires cleaning after 11:00 departure",
    status: "Pending",
    requestedBy: "D. Kumara",
  },
  {
    id: "APR-298",
    centreId: "makumbura",
    tripId: "TRP-8248",
    request: "Late departure authorization",
    reason: "Traffic restriction affected vehicle arrival",
    status: "Pending",
    requestedBy: "N. Silva",
  },
];
