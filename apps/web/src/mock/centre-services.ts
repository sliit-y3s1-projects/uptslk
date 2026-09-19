export type ServiceRun = {
  time: string;
  vehicle: string;
  state: "In service" | "Boarding" | "Scheduled";
  detail: string;
};

export type CentreRouteService = {
  id: string;
  centreId: string;
  route: string;
  origin: string;
  destination: string;
  intervalMinutes: number;
  bay: string;
  currentLocation: string;
  runs: ServiceRun[];
};

export const centreRouteServices: CentreRouteService[] = [
  {
    id: "SVC-KDW-177",
    centreId: "kadawatha",
    route: "177",
    origin: "Kadawatha MMC",
    destination: "Kaduwela",
    intervalMinutes: 30,
    bay: "B04",
    currentLocation: "Approaching Biyagama",
    runs: [
      {
        time: "10:30",
        vehicle: "WP NC-4582",
        state: "In service",
        detail: "Departed · 18 min ago",
      },
      {
        time: "11:00",
        vehicle: "WP ND-7714",
        state: "Boarding",
        detail: "Bay B04 · 62% allocated",
      },
      {
        time: "11:30",
        vehicle: "WP NB-3901",
        state: "Scheduled",
        detail: "Driver assigned",
      },
      {
        time: "12:00",
        vehicle: "WP NC-5520",
        state: "Scheduled",
        detail: "Vehicle ready",
      },
    ],
  },
  {
    id: "SVC-MKB-138",
    centreId: "makumbura",
    route: "138",
    origin: "Makumbura MMC",
    destination: "Pettah",
    intervalMinutes: 20,
    bay: "B11",
    currentLocation: "Passing Maharagama",
    runs: [
      {
        time: "09:50",
        vehicle: "WP CAQ-7750",
        state: "In service",
        detail: "Departed · 20 min ago",
      },
      {
        time: "10:10",
        vehicle: "WP CAD-1024",
        state: "Boarding",
        detail: "Bay B11 · 32% allocated",
      },
      {
        time: "10:30",
        vehicle: "WP NB-6821",
        state: "Scheduled",
        detail: "Driver assigned",
      },
      {
        time: "10:50",
        vehicle: "WP NC-1190",
        state: "Scheduled",
        detail: "Vehicle ready",
      },
    ],
  },
];
