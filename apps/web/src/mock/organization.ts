export type Employee = {
  id: string;
  name: string;
  email: string;
  role: string;
  centreId?: string;
  status: "Active" | "Invited" | "Suspended";
  lastActive: string;
};

export type PlatformRole = {
  id: string;
  name: string;
  scope: "Global" | "Centre";
  users: number;
  permissions: string[];
};

export const employees: Employee[] = [
  { id: "EMP-001", name: "Ayesha Fernando", email: "ayesha@upts.lk", role: "Super Admin", status: "Active", lastActive: "Now" },
  { id: "EMP-014", name: "Nimal Perera", email: "nimal@upts.lk", role: "Centre Manager", centreId: "makumbura", status: "Active", lastActive: "8 min ago" },
  { id: "EMP-021", name: "Shanthi Silva", email: "shanthi@upts.lk", role: "Centre Manager", centreId: "kadawatha", status: "Active", lastActive: "24 min ago" },
  { id: "EMP-027", name: "Kasun Jayawardena", email: "kasun@upts.lk", role: "Dispatch Officer", centreId: "makumbura", status: "Active", lastActive: "4 min ago" },
  { id: "EMP-031", name: "Imesha Peris", email: "imesha@upts.lk", role: "Fleet Officer", centreId: "kadawatha", status: "Invited", lastActive: "Invitation sent" },
  { id: "EMP-034", name: "Ravindu Senanayake", email: "ravindu@upts.lk", role: "Safety Officer", centreId: "makumbura", status: "Suspended", lastActive: "Sep 2" },
];

export const platformRoles: PlatformRole[] = [
  { id: "ROLE-01", name: "Super Admin", scope: "Global", users: 1, permissions: ["Organization", "Centres", "People", "Roles", "Audit", "Settings"] },
  { id: "ROLE-02", name: "Centre Manager", scope: "Centre", users: 2, permissions: ["Operations", "Fleet", "Routes", "Employees", "Reports"] },
  { id: "ROLE-03", name: "Dispatch Officer", scope: "Centre", users: 8, permissions: ["Dispatch", "Bays", "Trips", "Incidents"] },
  { id: "ROLE-04", name: "Fleet Officer", scope: "Centre", users: 5, permissions: ["Vehicles", "Drivers", "Maintenance"] },
  { id: "ROLE-05", name: "Safety Officer", scope: "Centre", users: 4, permissions: ["Incidents", "Approvals", "Audit read"] },
];

export const accessRequests = [
  { id: "REQ-104", employee: "Imesha Peris", request: "Fleet Officer access", centre: "Kadawatha MMC", requested: "18 min ago", risk: "Standard" },
  { id: "REQ-101", employee: "Nimal Perera", request: "Temporary Finance approval", centre: "Makumbura MMC", requested: "2 hours ago", risk: "Elevated" },
];

export const auditEvents = [
  { id: "AUD-8301", actor: "Ayesha Fernando", action: "Invited employee", target: "imesha@upts.lk", time: "10:18", source: "Super Admin" },
  { id: "AUD-8298", actor: "Nimal Perera", action: "Updated vehicle assignment", target: "WP CAB-4821", time: "09:44", source: "Makumbura MMC" },
  { id: "AUD-8291", actor: "Shanthi Silva", action: "Resolved incident", target: "INC-1036", time: "09:12", source: "Kadawatha MMC" },
  { id: "AUD-8284", actor: "Ayesha Fernando", action: "Changed role permissions", target: "Fleet Officer", time: "Yesterday", source: "Super Admin" },
];

export const serviceHealth = [
  { name: "Identity and access", status: "Operational", latency: "84 ms" },
  { name: "Transport API", status: "Operational", latency: "126 ms" },
  { name: "Notification service", status: "Degraded", latency: "680 ms" },
  { name: "Payment sandbox", status: "Operational", latency: "214 ms" },
  { name: "Maps and routing", status: "Operational", latency: "172 ms" },
];
