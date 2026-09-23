export type WorkflowStatus =
  "Running" | "PausedForApproval" | "Completed" | "Failed";
export type ApprovalDecision = "Approved" | "Rejected";

export type RecoveryWorkflow = {
  id: string;
  centreId: string;
  incidentId: string;
  tripId: string;
  objective: string;
  status: WorkflowStatus;
  failureReason?: string | null;
  incident: { title: string; type: string; severity: string };
  trip: { route: string; scheduledTime: string };
  pendingApproval: boolean;
  createdAt: string;
  updatedAt: string;
  completedAt?: string | null;
};

export type RecoveryWorkflowDetail = {
  summary: RecoveryWorkflow;
  trip: {
    id: string;
    scheduledTime: string;
    status: string;
    route: string;
    name: string;
    vehicle: string;
    driver: string;
    bay: string;
  };
  steps: {
    id: string;
    agentName: string;
    status: string;
    input: unknown;
    output: {
      summary?: string;
      reasons?: string[];
      warnings?: string[];
      vehicleId?: string;
      driverId?: string;
      bayId?: string;
      scheduledTime?: string;
      capacity?: number;
    } | null;
    error?: string | null;
    durationMs: number;
    createdAt: string;
  }[];
  approvals: {
    id: string;
    reason: string;
    decision: "Pending" | ApprovalDecision;
    decisionNote?: string | null;
    reviewedBy?: string | null;
    decidedAt?: string | null;
    appliedAt?: string | null;
    createdAt: string;
  }[];
};

export type RecoveryIncident = {
  id: string;
  tripId?: string | null;
  title: string;
  type: string;
  severity: string;
  status: string;
  tripRouteNumber?: string | null;
  tripTime?: string | null;
};
