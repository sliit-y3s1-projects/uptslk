import { useMutation, useQuery } from "@tanstack/react-query";
import { fareRulesService as service } from "../services/fare-rules.service";
import { useRefreshPassengerFares } from "./useRefresh";
import type { FareFilters, UpdateFareRuleRequest } from "../types/fares";
export const useFareRules = (filters: FareFilters) =>
  useQuery({
    queryKey: ["fare-rules", "list", filters],
    queryFn: () => service.list(filters),
    retry: false,
  });
export const useFareRule = (id: string) =>
  useQuery({
    queryKey: ["fare-rules", id],
    queryFn: () => service.detail(id),
    enabled: !!id,
    retry: false,
  });
export const useFareQuote = (tripId: string, passengerId: string) =>
  useQuery({
    queryKey: ["fare-quotes", tripId, passengerId],
    queryFn: () => service.quote(tripId, passengerId),
    enabled: !!tripId && !!passengerId,
    retry: false,
  });
export function useFareRuleMutations() {
  const onSuccess = useRefreshPassengerFares();
  const create = useMutation({ mutationFn: service.create, onSuccess });
  const update = useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateFareRuleRequest }) =>
      service.update(id, body),
    onSuccess,
  });
  const deactivate = useMutation({ mutationFn: service.deactivate, onSuccess });
  return { create, update, deactivate };
}
