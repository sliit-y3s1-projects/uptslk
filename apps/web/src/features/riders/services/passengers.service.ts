import type {
  CreatedPassenger,
  TopUpWalletRequest,
  TopUpWalletResponse,
} from "../types/passengers";
import { apiClient } from "@/lib/api/api-client";
import { jsonBody, queryString } from "./request";
import type {
  Passenger,
  PassengerDetail,
  PassengerFilters,
  PassengerRequest,
  UpdatePassengerRequest,
} from "../types/passengers";
const path = "/api/v1/passengers";
export const passengersService = {
  list: (filters: PassengerFilters) =>
    apiClient<Passenger[]>(path + queryString(filters)),
  detail: (id: string) => apiClient<PassengerDetail>(`${path}/${id}`),
  create: (body: PassengerRequest) =>
    apiClient<CreatedPassenger>(path, jsonBody("POST", body)),
  update: (id: string, body: UpdatePassengerRequest) =>
    apiClient<void>(`${path}/${id}`, jsonBody("PUT", body)),
  deactivate: (id: string) =>
    apiClient<void>(`${path}/${id}`, { method: "DELETE" }),
  restore: (id: string) =>
    apiClient<void>(`${path}/${id}/restore`, { method: "POST" }),
  resetPassword: (id: string, password: string) =>
    apiClient<void>(
      `${path}/${id}/reset-password`,
      jsonBody("POST", { password }),
    ),
  topUp: (id: string, amount: number) =>
    apiClient<TopUpWalletResponse>(
      `${path}/${id}/wallet/top-ups`,
      jsonBody("POST", { amount } satisfies TopUpWalletRequest),
    ),
};
