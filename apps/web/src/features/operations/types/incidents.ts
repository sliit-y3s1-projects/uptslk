export type IncidentStatus = "Open" | "InProgress" | "Resolved";
export type IncidentType = "Delay" | "Safety" | "Breakdown" | "Other";
export type IncidentSeverity = "Low" | "Medium" | "High";
export interface IncidentListItem { id: string; centreId: string; tripId?: string; tripTime?: string; reportedByName: string; type: IncidentType; severity: IncidentSeverity; title: string; assignedTo?: string; status: IncidentStatus; slaDueAt: string; resolvedAt?: string; createdAt: string; }
export interface IncidentDetail extends IncidentListItem { description: string; centre: { id: string; code: string; name: string }; trip?: { id: string; scheduledTime: string; status: string }; updatedAt: string; }
export interface CreateIncidentRequest { centreId: string; tripId?: string; reportedByName: string; type: IncidentType; severity: IncidentSeverity; title: string; description: string; assignedTo?: string; slaDueAt?: string; }
export interface UpdateIncidentRequest { severity: IncidentSeverity; title: string; description: string; assignedTo?: string; status: IncidentStatus; slaDueAt: string; }

