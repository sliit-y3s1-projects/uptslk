import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  cancelMaintenanceRecord,
  createMaintenanceRecord,
  getMaintenanceRecord,
  getMaintenanceRecords,
  updateMaintenanceRecord,
} from "../services/maintenance.service";
import type {
  CreateMaintenanceRequest,
  MaintenanceDetail,
  MaintenanceListItem,
  MaintenanceQueryParams,
  UpdateMaintenanceRequest,
} from "../types";

export function useMaintenanceRecords(params?: MaintenanceQueryParams) {
  return useQuery<MaintenanceListItem[]>({
    queryKey: ["maintenance-records", params],
    queryFn: () => getMaintenanceRecords(params),
  });
}

export function useMaintenanceRecord(recordId?: string) {
  return useQuery<MaintenanceDetail>({
    queryKey: ["maintenance-records", recordId],
    queryFn: () => getMaintenanceRecord(recordId!),
    enabled: Boolean(recordId),
  });
}

export function useCreateMaintenanceRecord() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateMaintenanceRequest) => createMaintenanceRecord(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-records"] });
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
    },
  });
}

export function useUpdateMaintenanceRecord() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateMaintenanceRequest }) =>
      updateMaintenanceRecord(id, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-records"] });
      queryClient.invalidateQueries({
        queryKey: ["maintenance-records", variables.id],
      });
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
    },
  });
}

export function useCancelMaintenanceRecord() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (recordId: string) => cancelMaintenanceRecord(recordId),
    onSuccess: (_data, recordId) => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-records"] });
      queryClient.invalidateQueries({
        queryKey: ["maintenance-records", recordId],
      });
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
    },
  });
}

