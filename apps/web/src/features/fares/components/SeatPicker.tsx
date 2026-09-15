import { Button } from "@/components/ui/button";
import { useSeats } from "../hooks/useBookings";
import { QueryState } from "@/features/riders/components/FeatureUi";
export function SeatPicker({
  tripId,
  value,
  onChange,
  disabled = false,
}: {
  tripId: string;
  value: string;
  onChange: (seat: string) => void;
  disabled?: boolean;
}) {
  const query = useSeats(tripId);
  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-medium">Choose a seat</h3>
        <Button
          type="button"
          size="sm"
          variant="ghost"
          disabled={query.isFetching || disabled}
          onClick={() => query.refetch()}
        >
          Refresh seats
        </Button>
      </div>
      <QueryState query={query} empty={!query.data?.length} />
      {query.data && !query.error && (
        <>
          <p className="text-sm text-muted-foreground">
            {query.data.filter((s) => s.isAvailable).length} available ·{" "}
            {query.data.length} seats total. Unavailable seats are disabled.
          </p>
          <div
            role="group"
            aria-label="Available seats"
            className="grid max-w-lg grid-cols-5 gap-2 sm:grid-cols-8"
          >
            {query.data.map((s) => (
              <Button
                type="button"
                key={s.seatNumber}
                variant={value === s.seatNumber ? "default" : "outline"}
                aria-pressed={value === s.seatNumber}
                aria-label={`Seat ${s.seatNumber}${s.isAvailable ? "" : ", unavailable"}`}
                disabled={disabled || !s.isAvailable}
                onClick={() => onChange(s.seatNumber)}
              >
                {s.seatNumber}
              </Button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
