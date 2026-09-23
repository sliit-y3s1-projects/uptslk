import { apiClient } from "@/lib/api/api-client";
import type {
  ApprovalDecision,
  RecoveryWorkflow,
  RecoveryWorkflowDetail,
} from "./agent-recovery.types";

export const agentRecoveryApi = {
  list: (centreId?: string) =>
    apiClient<RecoveryWorkflow[]>(
      `/api/v1/agent-recovery/workflows${centreId ? `?centreId=${centreId}` : ""}`,
    ),
  detail: (id: string) =>
    apiClient<RecoveryWorkflowDetail>(`/api/v1/agent-recovery/workflows/${id}`),
  start: (incidentId: string, objective?: string) =>
    apiClient<{ id: string }>("/api/v1/agent-recovery/workflows", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ incidentId, objective }),
    }),
  decide: (id: string, decision: ApprovalDecision, note?: string) =>
    apiClient<{ id: string }>(
      `/api/v1/agent-recovery/workflows/${id}/approval`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ decision, note }),
      },
    ),
};
