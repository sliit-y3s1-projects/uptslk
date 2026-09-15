import { useAuth } from "@/hooks/useAuth";
import { useCentres } from "./useBookings";
export function useCentreSelection(selected: string) {
  const { user } = useAuth();
  const query = useCentres();
  const centreId =
    selected || query.data?.find((c) => c.id === user?.centreId)?.id || "";
  return { query, centreId };
}
