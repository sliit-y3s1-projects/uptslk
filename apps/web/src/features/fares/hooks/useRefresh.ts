import { useQueryClient } from "@tanstack/react-query";
export function useRefreshPassengerFares() {
  const client = useQueryClient();
  return () =>
    Promise.all(
      [
        "passengers",
        "fare-rules",
        "bookings",
        "fare-quotes",
        "booking-seats",
        "booking-manifests",
      ].map((key) => client.invalidateQueries({ queryKey: [key] })),
    );
}
