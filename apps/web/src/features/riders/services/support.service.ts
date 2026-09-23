import { apiClient } from "@/lib/api/api-client";
import { jsonBody } from "./request";
import type {
  CreateSupportRequest,
  SupportRequest,
  SupportRequestType,
  UpdateSupportRequest,
} from "../types/support";

const path = "/api/v1/support-requests";
export const supportService = {
  list: (type?: SupportRequestType) =>
    apiClient<SupportRequest[]>(type ? `${path}?type=${type}` : path),
  create: (body: CreateSupportRequest) =>
    apiClient<{ id: string }>(path, jsonBody("POST", body)),
  update: (id: string, body: UpdateSupportRequest) =>
    apiClient<void>(`${path}/${id}`, jsonBody("PUT", body)),
};
