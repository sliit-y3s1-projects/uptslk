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

export type RecoveryPlanning = {
  planningMode: string;
  modelProvider?: string | null;
  modelName?: string | null;
  planningDurationMs: number;
  totalTokenCount: number;
  planningFallbackReason?: string | null;
  replanCount: number;
};

export type RecoveryWorkflowDetail = {
  summary: RecoveryWorkflow;
  planning: RecoveryPlanning;
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
  plan: {
    order: number;
    title: string;
    owner: string;
    purpose: string;
    status: string;
    completedAt?: string | null;
  }[];
  validationResults: {
    phase: string;
    check: string;
    passed: boolean;
    detail: string;
    checkedAt: string;
  }[];
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
    toolCalls: {
      tool: string;
      input: unknown;
      output: unknown;
    }[];
    retryCount: number;
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

// ---- Coordinator tools (fleet, network, passenger and dispatch agents) ----

export type ScoreFactor = {
  factor: string;
  points: number;
  maxPoints: number;
  detail: string;
};

export type RecoveryAlternative = {
  id: string;
  vehicleId: string;
  vehiclePlate: string;
  vehicleCapacity: number;
  driverId?: string | null;
  driverName?: string | null;
  bayId?: string | null;
  bayCode?: string | null;
  scheduledTime: string;
  delayMinutes: number;
  score: number;
  feasible: boolean;
  isCurrent: boolean;
  factors: ScoreFactor[];
  risks: string[];
  issues: string[];
};

export type RecoveryLesson = {
  id: string;
  kind: "Rejected" | "Approved";
  summary: string;
  reason?: string | null;
  createdAt: string;
};

export type RecoveryAlternatives = {
  alternatives: RecoveryAlternative[];
  evaluated: number;
  recommendation: string;
  lessons: RecoveryLesson[];
};

export type ScenarioRequest = {
  vehicleId: string;
  driverId?: string | null;
  bayId?: string | null;
  delayMinutes: number;
};

export type ImpactRisk = "High" | "Medium" | "Low";
export type ImpactReviewDecision = "Ignore" | "Escalate" | "Reset";

export type ImpactNode = {
  tripId: string;
  lane: "Bus" | "Driver" | "Connection" | "Headway";
  depth: number;
  routeNumber: string;
  routeName: string;
  scheduledTime: string;
  risk: ImpactRisk;
  slackMinutes?: number | null;
  expectedLateMinutes: number;
  passengers: number;
  vehicle?: string | null;
  driver?: string | null;
  reason: string;
  review?: "Ignored" | "Escalated" | null;
};

export type RecoveryImpact = {
  recoveredDeparture: string;
  recoveredArrival: string;
  delayMinutes: number;
  nodes: ImpactNode[];
  summary: {
    high: number;
    medium: number;
    low: number;
    ignored: number;
    escalated: number;
    passengersAtRisk: number;
  };
  recommendation: string;
};

export type ImpactExplanation = { answer: string; source: "ai" | "rules" };

export type MessageSegment = {
  segment: string;
  label: string;
  description: string;
  recipients: number;
  canReceiveInApp: boolean;
  subject: string;
  message: string;
  source: "default" | "edited" | "ai" | "rules" | "sent";
  warnings: string[];
  delivered: number;
  failed: number;
};

export type RecoveryMessages = {
  segments: MessageSegment[];
  totalRecipients: number;
  sent: boolean;
};

export type ReviseRequest = {
  reason: string;
  avoidVehicle: boolean;
  avoidDriver: boolean;
  avoidBay: boolean;
  maxDelayMinutes?: number | null;
};

export type ReviseResult = {
  id: string;
  status: WorkflowStatus;
  failureReason?: string | null;
  revision: number;
  rationale: string;
  appliedConstraints: string[];
  alternativesConsidered: number;
};
