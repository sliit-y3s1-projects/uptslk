import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createDriver,
  deactivateDriver,
  getDriver,
  getDrivers,
  updateDriver,
} from "../services/drivers.service";
import type {
  CreateDriverRequest,
  DriverDetail,
  DriverListItem,
  DriverQueryParams,
  UpdateDriverRequest,
} from "../types";

export function useDrivers(params?: DriverQueryParams) {
  return useQuery<DriverListItem[]>({
    queryKey: ["drivers", params],
    queryFn: () => getDrivers(params),
  });
}

export function useDriver(driverId?: string) {
  return useQuery<DriverDetail>({
    queryKey: ["drivers", driverId],
    queryFn: () => getDriver(driverId!),
    enabled: Boolean(driverId),
  });
}

export function useCreateDriver() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateDriverRequest) => createDriver(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["drivers"] });
    },
  });
}

export function useUpdateDriver() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDriverRequest }) =>
      updateDriver(id, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ["drivers"] });
      queryClient.invalidateQueries({ queryKey: ["drivers", variables.id] });
    },
  });
}

export function useDeactivateDriver() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (driverId: string) => deactivateDriver(driverId),
    onSuccess: (_data, driverId) => {
      queryClient.invalidateQueries({ queryKey: ["drivers"] });
      queryClient.invalidateQueries({ queryKey: ["drivers", driverId] });
    },
  });
}
