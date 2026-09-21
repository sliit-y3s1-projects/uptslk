export type SupportRequestType = "Assistance" | "Support";
export type SupportRequestStatus = "Open" | "InProgress" | "WaitingForPassenger" | "Resolved" | "Closed";
export type SupportRequestPriority = "Low" | "Medium" | "High" | "Urgent";

export interface SupportRequest {
  id: string;
  passengerId?: string | null;
  passenger?: string | null;
  tripId?: string | null;
  trip?: string | null;
  centreId?: string | null;
  type: SupportRequestType;
  priority: SupportRequestPriority;
  status: SupportRequestStatus;
  subject: string;
  description: string;
  assignedTo?: string | null;
  resolution?: string | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
}

export interface CreateSupportRequest {
  passengerId?: string;
  tripId?: string;
  centreId?: string;
  type: SupportRequestType;
  priority: SupportRequestPriority;
  subject: string;
  description: string;
}

export interface UpdateSupportRequest {
  priority: SupportRequestPriority;
  status: SupportRequestStatus;
  subject: string;
  description: string;
  assignedTo?: string;
  resolution?: string;
}
