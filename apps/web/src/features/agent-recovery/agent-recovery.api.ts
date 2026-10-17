import { apiClient } from "@/lib/api/api-client";
import type {
  ApprovalDecision,
  ImpactExplanation,
  ImpactReviewDecision,
  MessageSegment,
  RecoveryAlternative,
  RecoveryAlternatives,
  RecoveryImpact,
  RecoveryLesson,
  RecoveryMessages,
  RecoveryWorkflow,
  RecoveryWorkflowDetail,
  ReviseRequest,
  ReviseResult,
  ScenarioRequest,
} from "./agent-recovery.types";

const base = "/api/v1/agent-recovery/workflows";
const json = { "Content-Type": "application/json" };
const post = <T>(path: string, body: unknown) =>
  apiClient<T>(path, {
    method: "POST",
    headers: json,
    body: JSON.stringify(body),
  });

export const agentRecoveryApi = {
  list: (centreId?: string) =>
    apiClient<RecoveryWorkflow[]>(
      `/api/v1/agent-recovery/workflows${centreId ? `?centreId=${centreId}` : ""}`,
    ),
  detail: (id: string) =>
    apiClient<RecoveryWorkflowDetail>(`/api/v1/agent-recovery/workflows/${id}`),
  start: (incidentId: string, objective?: string, allowFallback = true) =>
    apiClient<{ id: string }>("/api/v1/agent-recovery/workflows", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ incidentId, objective, allowFallback }),
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
  // Fleet agent: what-if simulation
  alternatives: (id: string) =>
    apiClient<RecoveryAlternatives>(`${base}/${id}/alternatives`),
  simulate: (id: string, scenario: ScenarioRequest) =>
    post<RecoveryAlternative>(`${base}/${id}/simulate`, scenario),
  applyAlternative: (id: string, scenario: ScenarioRequest) =>
    post<{ id: string }>(`${base}/${id}/proposal`, scenario),

  // Network agent: cascading impact
  impact: (id: string) => apiClient<RecoveryImpact>(`${base}/${id}/impact`),
  explainImpact: (id: string, tripId: string, question?: string) =>
    post<ImpactExplanation>(`${base}/${id}/impact/explain`, {
      tripId,
      question: question || undefined,
    }),
  reviewImpact: (id: string, tripId: string, decision: ImpactReviewDecision) =>
    post<RecoveryImpact>(`${base}/${id}/impact/review`, { tripId, decision }),

  // Passenger agent: message desk
  messages: (id: string) => apiClient<RecoveryMessages>(`${base}/${id}/messages`),
  saveMessage: (id: string, segment: string, message: string) =>
    apiClient<MessageSegment>(`${base}/${id}/messages/${segment}`, {
      method: "PUT",
      headers: json,
      body: JSON.stringify({ message }),
    }),
  regenerateMessage: (id: string, segment: string, instruction: string) =>
    post<MessageSegment>(`${base}/${id}/messages/${segment}/regenerate`, {
      instruction,
    }),
  resetMessage: (id: string, segment: string) =>
    apiClient<MessageSegment>(`${base}/${id}/messages/${segment}`, {
      method: "DELETE",
    }),

  // Dispatch agent: reject, revise and remember
  revise: (id: string, request: ReviseRequest) =>
    post<ReviseResult>(`${base}/${id}/revise`, request),
  lessons: (id: string) => apiClient<RecoveryLesson[]>(`${base}/${id}/lessons`),
};
