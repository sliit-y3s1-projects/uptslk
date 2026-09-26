import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createVehicle,
  deactivateVehicle,
  getVehicle,
  getVehicles,
  uploadVehicleImage,
  updateVehicle,
} from "../services/vehicles.service";
import type {
  CreateVehicleRequest,
  UpdateVehicleRequest,
  VehicleDetail,
  VehicleListItem,
  VehicleQueryParams,
} from "../types";

export function useVehicles(params?: VehicleQueryParams) {
  return useQuery<VehicleListItem[]>({
    queryKey: ["vehicles", params],
    queryFn: () => getVehicles(params),
  });
}

export function useVehicle(vehicleId?: string) {
  return useQuery<VehicleDetail>({
    queryKey: ["vehicles", vehicleId],
    queryFn: () => getVehicle(vehicleId!),
    enabled: Boolean(vehicleId),
  });
}

export function useCreateVehicle() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateVehicleRequest) => createVehicle(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
    },
  });
}

export function useUpdateVehicle() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateVehicleRequest }) =>
      updateVehicle(id, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      queryClient.invalidateQueries({ queryKey: ["vehicles", variables.id] });
    },
  });
}

export function useDeactivateVehicle() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vehicleId: string) => deactivateVehicle(vehicleId),
    onSuccess: (_data, vehicleId) => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      queryClient.invalidateQueries({ queryKey: ["vehicles", vehicleId] });
    },
  });
}

export function useUploadVehicleImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ vehicleId, file }: { vehicleId: string; file: File }) =>
      uploadVehicleImage(vehicleId, file),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      queryClient.invalidateQueries({
        queryKey: ["vehicles", variables.vehicleId],
      });
    },
  });
}
