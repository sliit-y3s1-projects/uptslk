import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { centresApi } from '../services/centres.api';
import type { CreateCentreRequest, UpdateCentreRequest, CreateBayRequest, UpdateBayRequest } from '../types';

export function useCentres(status?: string, district?: string, search?: string) {
  return useQuery({
    queryKey: ['centres', { status, district, search }],
    queryFn: () => centresApi.getCentres(status, district, search)
  });
}

export function useCentre(centreId?: string) {
  return useQuery({
    queryKey: ['centres', centreId],
    queryFn: () => centresApi.getCentre(centreId!),
    enabled: !!centreId
  });
}

export function useCreateCentre() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCentreRequest) => centresApi.createCentre(data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['centres'] })
  });
}

export function useUpdateCentre(centreId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateCentreRequest) => centresApi.updateCentre(centreId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['centres', centreId] });
      queryClient.invalidateQueries({ queryKey: ['centres'] });
    }
  });
}

export function useCloseCentre() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (centreId: string) => centresApi.closeCentre(centreId),
    onSuccess: (_, centreId) => {
      queryClient.invalidateQueries({ queryKey: ['centres', centreId] });
      queryClient.invalidateQueries({ queryKey: ['centres'] });
    }
  });
}

// Bay Hooks
export function useBays(centreId?: string) {
  return useQuery({
    queryKey: ['centres', centreId, 'bays'],
    queryFn: () => centresApi.getBays(centreId!),
    enabled: !!centreId
  });
}

export function useCreateBay(centreId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateBayRequest) => centresApi.createBay(centreId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['centres', centreId] });
      queryClient.invalidateQueries({ queryKey: ['centres'] });
    }
  });
}

export function useUpdateBay(centreId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ bayId, data }: { bayId: string; data: UpdateBayRequest }) => centresApi.updateBay(bayId, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['centres', centreId] })
  });
}

export function useDeactivateBay(centreId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (bayId: string) => centresApi.deactivateBay(bayId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['centres', centreId] })
  });
}
