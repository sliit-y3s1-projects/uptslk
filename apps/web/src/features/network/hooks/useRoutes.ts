import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { routesApi } from "../services/routes.api";
import type {
  CreateRouteRequest,
  UpdateRouteRequest,
  CreateRouteScheduleRequest,
  UpdateRouteScheduleRequest,
  GenerateScheduleTripsRequest,
  CreateRouteDirectionRequest,
} from "../types";

export function useRoutes(centreId?: string, search?: string) {
  return useQuery({
    queryKey: ["routes", { centreId, search }],
    queryFn: () => routesApi.getRoutes(centreId, search),
  });
}

export function useRoute(routeId?: string) {
  return useQuery({
    queryKey: ["routes", routeId],
    queryFn: () => routesApi.getRoute(routeId!),
    enabled: !!routeId,
  });
}

export function useCreateRoute() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateRouteRequest) => routesApi.createRoute(data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["routes"] }),
  });
}

export function useUpdateRoute(routeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateRouteRequest) =>
      routesApi.updateRoute(routeId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["routes", routeId] });
      queryClient.invalidateQueries({ queryKey: ["routes"] });
    },
  });
}

export function useArchiveRoute() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (routeId: string) => routesApi.archiveRoute(routeId),
    onSuccess: (_, routeId) => {
      queryClient.invalidateQueries({ queryKey: ["routes", routeId] });
      queryClient.invalidateQueries({ queryKey: ["routes"] });
    },
  });
}

export function useSchedules(routeId?: string) {
  return useQuery({
    queryKey: ["routes", routeId, "schedules"],
    queryFn: () => routesApi.getSchedules(routeId!),
    enabled: !!routeId,
  });
}

export function useCreateSchedule(routeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateRouteScheduleRequest) =>
      routesApi.createSchedule(routeId, data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["routes", routeId] }),
  });
}

export function useUpdateSchedule(routeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      scheduleId,
      data,
    }: {
      scheduleId: string;
      data: UpdateRouteScheduleRequest;
    }) => routesApi.updateSchedule(scheduleId, data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["routes", routeId] }),
  });
}

export function useDeactivateSchedule(routeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (scheduleId: string) =>
      routesApi.deactivateSchedule(scheduleId),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["routes", routeId] }),
  });
}

export function useGenerateScheduleTrips(routeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      scheduleId,
      data,
    }: {
      scheduleId: string;
      data: GenerateScheduleTripsRequest;
    }) => routesApi.generateScheduleTrips(scheduleId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["trips"] });
      queryClient.invalidateQueries({
        queryKey: ["routes", routeId, "schedules"],
      });
    },
  });
}

export function useDirections(routeId?: string) {
  return useQuery({
    queryKey: ["routes", routeId, "directions"],
    queryFn: () => routesApi.getDirections(routeId!),
    enabled: !!routeId,
  });
}

export function useCreateDirection(routeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateRouteDirectionRequest) =>
      routesApi.createDirection(routeId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["routes", routeId] });
      queryClient.invalidateQueries({
        queryKey: ["routes", routeId, "directions"],
      });
      queryClient.invalidateQueries({ queryKey: ["routes"] });
    },
  });
}

export function useUpdateDirection(routeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      directionId,
      data,
    }: {
      directionId: string;
      data: CreateRouteDirectionRequest & { isActive: boolean };
    }) => routesApi.updateDirection(directionId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["routes", routeId] });
      queryClient.invalidateQueries({
        queryKey: ["routes", routeId, "directions"],
      });
      queryClient.invalidateQueries({ queryKey: ["routes"] });
    },
  });
}
