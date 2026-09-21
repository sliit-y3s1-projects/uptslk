import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { supportService } from "../services/support.service";
import type { CreateSupportRequest, SupportRequestType, UpdateSupportRequest } from "../types/support";

export function useSupportRequests(type?: SupportRequestType) {
  return useQuery({ queryKey: ["support-requests", type], queryFn: () => supportService.list(type), retry: false });
}

export function useSupportRequestMutations() {
  const client = useQueryClient();
  const refresh = () => client.invalidateQueries({ queryKey: ["support-requests"] });
  return {
    create: useMutation({ mutationFn: (body: CreateSupportRequest) => supportService.create(body), onSuccess: refresh }),
    update: useMutation({ mutationFn: ({ id, body }: { id: string; body: UpdateSupportRequest }) => supportService.update(id, body), onSuccess: refresh }),
  };
}
