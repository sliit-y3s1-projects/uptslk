import type { CreatedFareRule } from "../types/fares";
import { apiClient } from "@/lib/api/api-client";
import { jsonBody, queryString } from "@/features/riders/services/request";
import type {
  CreateFareRuleRequest,
  UpdateFareRuleRequest,
  FareFilters,
  FareRule,
  FareQuote,
} from "../types/fares";
const path = "/api/v1/fare-rules";
export const fareRulesService = {
  list: (filters: FareFilters) =>
    apiClient<FareRule[]>(path + queryString(filters)),
  detail: (id: string) => apiClient<FareRule>(`${path}/${id}`),
  create: (body: CreateFareRuleRequest) =>
    apiClient<CreatedFareRule>(path, jsonBody("POST", body)),
  update: (id: string, body: UpdateFareRuleRequest) =>
    apiClient<void>(`${path}/${id}`, jsonBody("PUT", body)),
  deactivate: (id: string) =>
    apiClient<void>(`${path}/${id}`, { method: "DELETE" }),
  quote: (tripId: string, passengerId: string) =>
    apiClient<FareQuote>(
      `${path}/quote${queryString({ tripId, passengerId })}`,
    ),
};
