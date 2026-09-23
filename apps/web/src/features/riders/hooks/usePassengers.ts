import { useMutation, useQuery } from "@tanstack/react-query";
import { passengersService as service } from "../services/passengers.service";
import { useRefreshPassengerFares } from "@/features/fares/hooks/useRefresh";
import type {
  PassengerFilters,
  UpdatePassengerRequest,
} from "../types/passengers";
export const usePassengers = (filters: PassengerFilters = {}) =>
  useQuery({
    queryKey: ["passengers", "list", filters],
    queryFn: () => service.list(filters),
    retry: false,
  });
export const usePassenger = (id: string) =>
  useQuery({
    queryKey: ["passengers", id],
    queryFn: () => service.detail(id),
    enabled: !!id,
    retry: false,
  });
export function usePassengerMutations() {
  const onSuccess = useRefreshPassengerFares();
  const create = useMutation({ mutationFn: service.create, onSuccess });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdatePassengerRequest }) =>
      service.update(id, body),
    onSuccess,
  });
  const deactivate = useMutation({ mutationFn: service.deactivate, onSuccess });
  const restore = useMutation({ mutationFn: service.restore, onSuccess });
  const resetPassword = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      service.resetPassword(id, password),
  });
  const topUp = useMutation({
    mutationFn: ({ id, amount }: { id: string; amount: number }) =>
      service.topUp(id, amount),
    onSuccess,
  });
  return { create, update, deactivate, restore, resetPassword, topUp };
}
