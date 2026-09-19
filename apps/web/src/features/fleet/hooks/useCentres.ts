import { useQuery } from "@tanstack/react-query";
import { getCentres } from "../services/centres.service";
import type { CentreOption } from "../types";

const UUID_REGEX =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function useCentres() {
  return useQuery<CentreOption[]>({
    queryKey: ["centres"],
    queryFn: getCentres,
    staleTime: 5 * 60 * 1000,
  });
}

export function resolveCentreGuid(
  centres: CentreOption[] | undefined,
  centreIdOrSlug?: string,
): string | undefined {
  if (!centreIdOrSlug || !centreIdOrSlug.trim()) return undefined;
  const trimmed = centreIdOrSlug.trim();

  // If already a valid UUID/GUID format, return it
  if (UUID_REGEX.test(trimmed)) {
    return trimmed;
  }

  if (!centres || centres.length === 0) return undefined;

  // Resolve by ID, Code, Name, or City against real centres from GET /api/v1/centres
  const lower = trimmed.toLowerCase();
  const matched = centres.find(
    (c) =>
      c.id.toLowerCase() === lower ||
      c.code.toLowerCase() === lower ||
      c.name.toLowerCase().includes(lower) ||
      c.city.toLowerCase().includes(lower),
  );

  return matched?.id;
}

export function useEffectiveCentreGuid(centreIdOrSlug?: string) {
  const { data: centres, isLoading, error } = useCentres();
  const effectiveCentreId = resolveCentreGuid(centres, centreIdOrSlug);

  return {
    effectiveCentreId,
    centres: centres ?? [],
    isLoading,
    error,
  };
}
