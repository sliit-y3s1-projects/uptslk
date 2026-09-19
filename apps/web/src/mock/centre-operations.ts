export type BayState = "Boarding" | "Occupied" | "Available" | "Closed";
export type BayOperation = {
  bay: string;
  state: BayState;
  route?: string;
  destination?: string;
  vehicle?: string;
  departure?: string;
  queue?: number;
};
export type PassengerZone = {
  id: string;
  centreId: string;
  name: string;
  level: "Normal" | "Busy" | "Critical";
  passengers: number;
  wait: string;
  trend: string;
};
export type AssistanceCase = {
  id: string;
  centreId: string;
  type: "Accessibility" | "Lost property";
  passenger: string;
  detail: string;
  status: "Open" | "Assigned" | "Resolved";
  owner: string;
};

export const bayOperations: Record<string, BayOperation[]> = {
  makumbura: [
    {
      bay: "B03",
      state: "Boarding",
      route: "EX01",
      destination: "Galle",
      vehicle: "WP CAB-4821",
      departure: "09:45",
      queue: 34,
    },
    {
      bay: "B05",
      state: "Occupied",
      route: "C001",
      destination: "Fort",
      vehicle: "WP CAD-1024",
      departure: "10:20",
      queue: 18,
    },
    {
      bay: "B07",
      state: "Occupied",
      route: "EX02",
      destination: "Matara",
      vehicle: "WP NB-6602",
      departure: "10:00",
      queue: 26,
    },
    {
      bay: "B11",
      state: "Boarding",
      route: "138",
      destination: "Pettah",
      vehicle: "WP CAQ-7750",
      departure: "10:10",
      queue: 41,
    },
  ],
  kadawatha: [
    {
      bay: "B02",
      state: "Occupied",
      route: "EX04",
      destination: "Kandy",
      vehicle: "WP NC-3381",
      departure: "09:50",
      queue: 28,
    },
    {
      bay: "B04",
      state: "Boarding",
      route: "177",
      destination: "Kaduwela",
      vehicle: "WP ND-7714",
      departure: "11:00",
      queue: 37,
    },
    {
      bay: "B06",
      state: "Occupied",
      route: "EX06",
      destination: "Kurunegala",
      vehicle: "WP ND-7304",
      departure: "10:05",
      queue: 22,
    },
    {
      bay: "B09",
      state: "Boarding",
      route: "234",
      destination: "Colombo Fort",
      vehicle: "WP NB-2428",
      departure: "10:15",
      queue: 31,
    },
    { bay: "B13", state: "Closed" },
  ],
};

export const passengerZones: PassengerZone[] = [
  {
    id: "PZ-M01",
    centreId: "makumbura",
    name: "Expressway concourse",
    level: "Busy",
    passengers: 126,
    wait: "12 min",
    trend: "+18%",
  },
  {
    id: "PZ-M02",
    centreId: "makumbura",
    name: "Local bus platform",
    level: "Normal",
    passengers: 74,
    wait: "7 min",
    trend: "+3%",
  },
  {
    id: "PZ-M03",
    centreId: "makumbura",
    name: "Ticketing hall",
    level: "Normal",
    passengers: 31,
    wait: "4 min",
    trend: "−6%",
  },
  {
    id: "PZ-K01",
    centreId: "kadawatha",
    name: "Kaduwela platform",
    level: "Busy",
    passengers: 98,
    wait: "14 min",
    trend: "+22%",
  },
  {
    id: "PZ-K02",
    centreId: "kadawatha",
    name: "Intercity concourse",
    level: "Critical",
    passengers: 164,
    wait: "19 min",
    trend: "+31%",
  },
  {
    id: "PZ-K03",
    centreId: "kadawatha",
    name: "Ticketing hall",
    level: "Normal",
    passengers: 43,
    wait: "5 min",
    trend: "+2%",
  },
];

export const assistanceCases: AssistanceCase[] = [
  {
    id: "AST-211",
    centreId: "makumbura",
    type: "Accessibility",
    passenger: "S. Wijesinghe",
    detail: "Wheelchair boarding assistance · EX01",
    status: "Assigned",
    owner: "Passenger care desk",
  },
  {
    id: "LST-094",
    centreId: "makumbura",
    type: "Lost property",
    passenger: "N. Perera",
    detail: "Black backpack reported near Bay B07",
    status: "Open",
    owner: "Unassigned",
  },
  {
    id: "AST-219",
    centreId: "kadawatha",
    type: "Accessibility",
    passenger: "M. Fernando",
    detail: "Assistance requested for Route 177",
    status: "Open",
    owner: "Unassigned",
  },
  {
    id: "LST-101",
    centreId: "kadawatha",
    type: "Lost property",
    passenger: "T. Silva",
    detail: "Mobile phone handed to information desk",
    status: "Assigned",
    owner: "Information desk",
  },
];

export const centreFinance = {
  makumbura: {
    collected: 186400,
    cash: 54200,
    digital: 132200,
    refunds: 4200,
    variance: 850,
    transactions: 1248,
  },
  kadawatha: {
    collected: 142800,
    cash: 48700,
    digital: 94100,
    refunds: 3100,
    variance: -420,
    transactions: 976,
  },
};
