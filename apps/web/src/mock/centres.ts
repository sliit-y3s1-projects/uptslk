export type Centre = {
  id: string;
  name: string;
  location: string;
  district: string;
  status: "Operating" | "Planned";
  bays: number;
  description: string;
};

export type Departure = {
  id: string;
  bay: string;
  route: string;
  destination: string;
  time: string;
  vehicle: string;
  occupancy: number;
  status: "Boarding" | "On time" | "Delayed";
};

export const centres: Centre[] = [
  { id: "makumbura", name: "Makumbura MMC", location: "Kottawa", district: "Colombo", status: "Operating", bays: 12, description: "Expressway, local bus, rail and Park & Ride interchange" },
  { id: "kadawatha", name: "Kadawatha MMC", location: "Kadawatha", district: "Gampaha", status: "Operating", bays: 13, description: "Colombo–Kandy corridor and expressway interchange" },
  { id: "anuradhapura", name: "Anuradhapura South MMC", location: "Anuradhapura", district: "Anuradhapura", status: "Planned", bays: 10, description: "Planned northern-region interchange" },
  { id: "kandy", name: "Kandy MMC", location: "Kandy", district: "Kandy", status: "Planned", bays: 12, description: "Planned hill-country interchange" },
  { id: "kurunegala", name: "Kurunegala MMC", location: "Kurunegala", district: "Kurunegala", status: "Planned", bays: 10, description: "Proposed north-western interchange" },
];

export const departuresByCentre: Record<string, Departure[]> = {
  makumbura: [
    { id: "MMC-811", bay: "B03", route: "EX01", destination: "Galle", time: "09:45", vehicle: "WP CAB-4821", occupancy: 76, status: "Boarding" },
    { id: "MMC-812", bay: "B07", route: "EX02", destination: "Matara", time: "10:00", vehicle: "WP NB-6602", occupancy: 48, status: "On time" },
    { id: "MMC-813", bay: "B11", route: "138", destination: "Pettah", time: "10:10", vehicle: "WP CAQ-7750", occupancy: 32, status: "On time" },
    { id: "MMC-814", bay: "B05", route: "C001", destination: "Fort", time: "10:20", vehicle: "WP CAD-1024", occupancy: 88, status: "Delayed" },
  ],
  kadawatha: [
    { id: "KDW-421", bay: "B02", route: "EX04", destination: "Kandy", time: "09:50", vehicle: "WP NC-3381", occupancy: 64, status: "Boarding" },
    { id: "KDW-422", bay: "B06", route: "EX06", destination: "Kurunegala", time: "10:05", vehicle: "WP ND-7304", occupancy: 53, status: "On time" },
    { id: "KDW-423", bay: "B09", route: "234", destination: "Colombo Fort", time: "10:15", vehicle: "WP NB-2428", occupancy: 41, status: "On time" },
    { id: "KDW-424", bay: "B13", route: "EX08", destination: "Dambulla", time: "10:30", vehicle: "WP NC-9012", occupancy: 72, status: "Delayed" },
    { id: "KDW-425", bay: "B04", route: "177", destination: "Kaduwela", time: "11:00", vehicle: "WP ND-7714", occupancy: 62, status: "Boarding" },
  ],
  anuradhapura: [],
  kandy: [],
  kurunegala: [],
};

export const demoAccounts = [
  { label: "Makumbura operations", role: "CentreManager", centreId: "makumbura" as const },
  { label: "Kadawatha operations", role: "CentreManager", centreId: "kadawatha" as const },
  { label: "Super admin overview", role: "SuperAdmin", centreId: undefined },
];
