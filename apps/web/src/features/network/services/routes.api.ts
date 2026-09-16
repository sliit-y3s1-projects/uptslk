import { apiClient } from "@/lib/api/api-client";
import type { Route, RouteSummary, CreateRouteRequest, UpdateRouteRequest, RouteSchedule, CreateRouteScheduleRequest, UpdateRouteScheduleRequest } from "../types";

export const routesApi = {
  getRoutes: (centreId?: string, search?: string) => {
    const params = new URLSearchParams();
    if (centreId) params.append("centreId", centreId);
    if (search) params.append("search", search);
    const qs = params.toString();
    return apiClient<RouteSummary[]>("/api/v1/routes" + (qs ? "?" + qs : ""));
  },
  getRoute: (routeId: string) => apiClient<Route>("/api/v1/routes/" + routeId),
  createRoute: (data: CreateRouteRequest) => apiClient<RouteSummary>("/api/v1/routes", { method: "POST", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  updateRoute: (routeId: string, data: UpdateRouteRequest) => apiClient<void>("/api/v1/routes/" + routeId, { method: "PUT", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  archiveRoute: (routeId: string) => apiClient<void>("/api/v1/routes/" + routeId, { method: "DELETE" }),

  getSchedules: (routeId: string) => apiClient<RouteSchedule[]>("/api/v1/routes/" + routeId + "/schedules"),
  createSchedule: (routeId: string, data: CreateRouteScheduleRequest) => apiClient<RouteSchedule>("/api/v1/routes/" + routeId + "/schedules", { method: "POST", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  updateSchedule: (scheduleId: string, data: UpdateRouteScheduleRequest) => apiClient<void>("/api/v1/routes/schedules/" + scheduleId, { method: "PUT", body: JSON.stringify(data), headers: { "Content-Type": "application/json" } }),
  deactivateSchedule: (scheduleId: string) => apiClient<void>("/api/v1/routes/schedules/" + scheduleId, { method: "DELETE" }),
};
